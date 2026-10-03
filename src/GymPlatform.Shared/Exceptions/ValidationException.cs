namespace GymPlatform.Shared.Exceptions;

public class ValidationException : GymException
{
    public ValidationException(string message) : base(message) { }
}
