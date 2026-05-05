namespace BillingService.Domain.Models
{
    public class ActionResponse
    {
        protected ActionResponse(object model)
        {
            Success = true;
            Data = model;
        }

        protected ActionResponse(string error)
        {
            Success = false;
            Error = error;
        }

        public bool Success { get; protected set; }

        public string Error { get; protected set; }

        public object Data { get; }

        public static ActionResponse SuccessResult()
        {
            return new ActionResponse(new object());
        }

        public static ActionResponse SuccessResult(object model)
        {
            return new ActionResponse(model);
        }

        public static ActionResponse FailResult(string errorMessage)
        {
            return new ActionResponse(errorMessage);
        }
    }
}
