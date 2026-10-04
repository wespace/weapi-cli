namespace WeSpace.Api.Application.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}
