namespace GymPlatform.Shared.Exceptions;

public abstract class GymException : Exception
{
    protected GymException(string message) : base(message) { }
    protected GymException(string message, Exception innerException) : base(message, innerException) { }
}
