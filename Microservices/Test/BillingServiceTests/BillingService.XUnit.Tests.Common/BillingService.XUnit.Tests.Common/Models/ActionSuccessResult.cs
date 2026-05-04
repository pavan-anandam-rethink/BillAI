namespace BillingService.XUnit.Tests.Common.Models
{
    public class ActionSuccessResult<T> : ActionBaseResult
    {
        public T Data { get; set; }
    }

    public class ActionSuccessResult : ActionBaseResult
    {
        public object Data { get; set; }
    }
}
