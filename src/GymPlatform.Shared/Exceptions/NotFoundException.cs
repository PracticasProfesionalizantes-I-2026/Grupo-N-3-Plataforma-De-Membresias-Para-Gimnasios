namespace GymPlatform.Shared.Exceptions;

public class NotFoundException : GymException
{
    public NotFoundException(string message) : base(message) { }
}
