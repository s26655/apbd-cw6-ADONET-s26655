namespace ApbdCw6AdonetS26655.Exceptions;

public class AppointmentConflictException : Exception
{
    public AppointmentConflictException(string message) : base(message)
    {
    }
}