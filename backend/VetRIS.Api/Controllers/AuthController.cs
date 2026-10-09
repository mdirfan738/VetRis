using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VetRIS.Infrastructure.Identity;
using VetRIS.Infrastructure.Persistence;

namespace VetRIS.Api.Controllers;

/// <summary>
/// Login (with an MFA challenge step when 2FA is enabled), TOTP-based MFA
/// setup/enable/disable, and refresh-token issuance/rotation/revocation.
/// Self-service registration is deliberately NOT here — in a hospital
/// system, staff accounts are provisioned by an admin (see
/// UsersController), not signed up by the staff member themselves.
/// </summary>
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private const string MfaChallengeAudience = "VetRIS.MfaChallenge";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly VetRISDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        VetRISDbContext dbContext,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public record LoginRequest(string Email, string Password);

    public record LoginResponse(
        string? AccessToken, DateTime? ExpiresAtUtc, string? RefreshToken, string? UserId, string[]? Roles,
        bool RequiresMfa, string? MfaChallengeToken);

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var passwordResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!passwordResult.Succeeded)
        {
            return Unauthorized(new { message = passwordResult.IsLockedOut ? "Account is locked. Try again later." : "Invalid credentials." });
        }

        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return Ok(new LoginResponse(null, null, null, null, null, RequiresMfa: true, MfaChallengeToken: GenerateMfaChallengeToken(user)));
        }

        return Ok(await IssueTokensAsync(user));
    }

    public record MfaVerifyRequest(string ChallengeToken, string Code);

    [HttpPost("mfa/verify")]
    public async Task<ActionResult<LoginResponse>> VerifyMfa(MfaVerifyRequest request)
    {
        var userId = ValidateMfaChallengeToken(request.ChallengeToken);
        if (userId is null)
        {
            return Unauthorized(new { message = "Challenge token is invalid or expired — log in again." });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var codeValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
        if (!codeValid)
        {
            return Unauthorized(new { message = "Invalid authenticator code." });
        }

        return Ok(await IssueTokensAsync(user));
    }

    public record MfaSetupResponse(string SharedKey, string AuthenticatorUri);

    /// <summary>Step 1 of enabling MFA: get a shared key to show as a QR code. Doesn't turn MFA on yet — see /mfa/enable.</summary>
    [HttpPost("mfa/setup")]
    [Authorize]
    public async Task<ActionResult<MfaSetupResponse>> SetupMfa()
    {
        var user = await _userManager.FindByIdAsync(CurrentUser.UserId ?? string.Empty);
        if (user is null)
        {
            return Unauthorized();
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var issuer = Uri.EscapeDataString("VetRIS");
        var label = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        var authenticatorUri = $"otpauth://totp/{issuer}:{label}?secret={key}&issuer={issuer}&digits=6";

        return Ok(new MfaSetupResponse(key!, authenticatorUri));
    }

    public record MfaCodeRequest(string Code);

    /// <summary>Step 2: submit a code generated from the key returned by /mfa/setup to actually turn MFA on.</summary>
    [HttpPost("mfa/enable")]
    [Authorize]
    public async Task<ActionResult> EnableMfa(MfaCodeRequest request)
    {
        var user = await _userManager.FindByIdAsync(CurrentUser.UserId ?? string.Empty);
        if (user is null)
        {
            return Unauthorized();
        }

        var valid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code);
        if (!valid)
        {
            return BadRequest(new { message = "Invalid code — MFA was not enabled." });
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return NoContent();
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<ActionResult> DisableMfa()
    {
        var user = await _userManager.FindByIdAsync(CurrentUser.UserId ?? string.Empty);
        if (user is null)
        {
            return Unauthorized();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        return NoContent();
    }

    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshRequest request)
    {
        var existing = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);
        if (existing is null || !existing.IsActive)
        {
            return Unauthorized(new { message = "Refresh token is invalid, expired, or already used." });
        }

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Rotation: the old token is revoked and replaced, never reused —
        // if it shows up again, that's a signal it was stolen.
        var response = await IssueTokensAsync(user);
        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.ReplacedByToken = response.RefreshToken;
        await _dbContext.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> Revoke(RefreshRequest request)
    {
        var existing = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);
        if (existing is null || !existing.IsActive)
        {
            return NotFound();
        }

        existing.RevokedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult Me() => Ok(new
    {
        userId = CurrentUser.UserId,
        userName = CurrentUser.UserName,
        roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value)
    });

    private async Task<LoginResponse> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, expiresAtUtc) = GenerateAccessToken(user, roles);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user);

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new LoginResponse(accessToken, expiresAtUtc, refreshToken, user.Id.ToString(), roles.ToArray(), RequiresMfa: false, MfaChallengeToken: null);
    }

    private (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expiryMinutes = double.TryParse(jwtSection["ExpiryMinutes"], out var minutes) ? minutes : 60;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"], audience: jwtSection["Audience"],
            claims: claims, expires: expiresAtUtc, signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    /// <summary>
    /// Short-lived, narrow-purpose token proving "password was correct" —
    /// NOT a valid access token. Its audience (MfaChallengeAudience) never
    /// matches Jwt:Audience, so the normal JWT bearer pipeline rejects it
    /// automatically if someone tries to use it against a real endpoint;
    /// only /mfa/verify's manual validation below accepts it.
    /// </summary>
    private string GenerateMfaChallengeToken(ApplicationUser user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: MfaChallengeAudience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string? ValidateMfaChallengeToken(string challengeToken)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(challengeToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = MfaChallengeAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    private async Task<string> GenerateAndStoreRefreshTokenAsync(ApplicationUser user)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokenValue,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _dbContext.SaveChangesAsync();

        return tokenValue;
    }
}
