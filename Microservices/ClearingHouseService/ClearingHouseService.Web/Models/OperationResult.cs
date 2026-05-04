using Rethink.Services.Common.Enums.Billing;

namespace ClearingHouseService.Web.Service
{
    public class OperationResult
    {
        protected OperationResult()
        {
            IsSuccess = true;
        }

        protected OperationResult(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
        }

        public static OperationResult Success(string fileName)
        {
            return new OperationResult { IsSuccess = true, FileName = fileName ,Error=ErrorType.None};
        }

        public static OperationResult Fail(ErrorType error, string message)
        {
            return new OperationResult
            {
                IsSuccess = false,
                Error = error,
                ErrorMessage = message
            };
        }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string FileName { get; set; }
        public ErrorType Error { get; set; }
    }
}
