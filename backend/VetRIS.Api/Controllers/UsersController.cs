using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Domain.Enums;
using VetRIS.Infrastructure.Identity;

namespace VetRIS.Api.Controllers;

/// <summary>
/// Staff account provisioning. Deliberately admin-only — nobody signs
/// themselves up in a hospital system. Uses UserManager/RoleManager
/// directly rather than going through MediatR: Identity operations aren't
/// really business logic the same way Study/Report workflows are, and
/// UserManager is already a well-designed service layer of its own.
/// </summary>
[Authorize(Roles = "SuperAdmin,HospitalAdmin")]
public class UsersController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public record CreateUserRequest(string Email, string FirstName, string LastName, string TemporaryPassword, UserRoleType Role, Guid? ClinicId, Guid? HospitalId);

    public record UserDto(string Id, string Email, string FirstName, string LastName, bool IsActive, string[] Roles);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ClinicId = request.ClinicId,
            HospitalId = request.HospitalId,
            EmailConfirmed = true // provisioned directly by an admin, not self-registered
        };

        var createResult = await _userManager.CreateAsync(user, request.TemporaryPassword);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, request.Role.ToString());

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await ToDto(user));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user is null ? NotFound() : Ok(await ToDto(user));
    }

    [HttpGet]
    public async Task<ActionResult> GetList()
    {
        var users = _userManager.Users.ToList();
        var dtos = new List<UserDto>();
        foreach (var user in users)
        {
            dtos.Add(await ToDto(user));
        }
        return Ok(dtos);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult> Deactivate(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult> Activate(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    private async Task<UserDto> ToDto(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id.ToString(), user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsActive, roles.ToArray());
    }
}
