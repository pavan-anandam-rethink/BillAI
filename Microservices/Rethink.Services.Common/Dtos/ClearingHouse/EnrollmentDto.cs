namespace Rethink.Services.Common.Dtos.ClearingHouse
{
    public record EnrollmentDto(
        string EnrollmentId,
        string PayerId,
        string Status
    );
}
