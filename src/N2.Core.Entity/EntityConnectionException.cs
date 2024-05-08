namespace N2.Core.Entity;

public class EntityConnectionException : Exception
{
    public int ErrorCode { get; protected set; } = 500;

    public EntityConnectionException(string message) : base(message)
    {
    }

    public EntityConnectionException()
    {
    }

    public EntityConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}