namespace GymPlatform.Shared.Exceptions;

public class ConflictException : GymException
{
    public ConflictException(string message) : base(message) { }
}
