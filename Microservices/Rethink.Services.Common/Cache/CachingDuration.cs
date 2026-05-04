namespace Rethink.Services.Common.Cache
{
    public enum CachingDuration
    {
        None = 0,

        OneMinute = 60,

        FiveMinutes = 300,

        TenMinutes = 600,

        OneHour = 3660,

        OneDay = 86400,

        OneWeek = 604800,

        Forever = 8640000
    }
}
