namespace Rethink.Services.Common.Extensions
{
    public static class StringExt
    {
        public static string ToEdiDiagnosisString(this string value)
        {
            return value.Replace(".", "");
        }
    }
}