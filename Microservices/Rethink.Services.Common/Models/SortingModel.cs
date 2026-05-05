namespace Rethink.Services.Common.Models
{
    public class SortingModel
    {
        public string Field { get; set; }
        public string Dir { get; set; } = "desc";
    }
    public class GridFilterModel
    {
        public string PropertyName { get; set; } 
        public string OperatorName { get; set; }
        public string Value { get; set; }
    }
}