namespace GymPlatform.Shared.Exceptions;

public class ScheduleConflictException : ConflictException
{
    public ScheduleConflictException(string message) : base(message) { }
}
