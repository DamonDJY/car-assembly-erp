namespace CarAssemblyErp.Features.Common;

public record ErrorResponse(string Error, string Message);

public class BusinessException : Exception
{
    public string ErrorCode { get; }

    public BusinessException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
