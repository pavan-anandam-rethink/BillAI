namespace Rethink.Services.Common.Extensions
{
    public static class DecimalExt
    {
        public static string ToEdiString(this decimal value)
        {
            return $"{value:F2}";
        }
    }
}