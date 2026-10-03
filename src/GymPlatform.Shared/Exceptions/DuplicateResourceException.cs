namespace GymPlatform.Shared.Exceptions;

public class DuplicateResourceException : ConflictException
{
    public DuplicateResourceException(string message) : base(message) { }
}
