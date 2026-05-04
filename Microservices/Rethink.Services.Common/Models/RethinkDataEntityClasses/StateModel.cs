namespace Rethink.Services.Common.Models.RethinkDataEntityClasses
{
    public class StateModel
    {
        public string name { get; set; }
        public string abbreviation { get; set; }
        public int utcOffset { get; set; }
        public int utcDstOffset { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }

    public class CountryModel
    {
        public string name { get; set; }
        public string code { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}
