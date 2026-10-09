namespace VetRIS.Domain.Exceptions;

/// <summary>
/// Thrown by repositories (Infrastructure layer) when a required entity
/// cannot be found. Kept in Domain so Application-layer handlers can catch
/// it without referencing Infrastructure.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"\"{entityName}\" with identifier ({key}) was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
