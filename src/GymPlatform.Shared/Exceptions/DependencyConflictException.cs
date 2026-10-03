namespace GymPlatform.Shared.Exceptions;

public class DependencyConflictException : ConflictException
{
    public DependencyConflictException(string message) : base(message) { }
}
