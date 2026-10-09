namespace VetRIS.Domain.Exceptions;

/// <summary>Thrown when an operation would violate a domain invariant or business rule.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
