using ClearingHouseService.Domain.Entities;

namespace ClearingHouseService.Domain.Interfaces
{
    /// <summary>
    /// Abstraction for clearing house transport operations.
    /// Implementations handle the specifics of sending/receiving EDI data
    /// via different protocols (SFTP, API, AS2, etc.).
    /// </summary>
    public interface IClearingHouseTransport
    {
        /// <summary>
        /// Sends an EDI file to the clearing house.
        /// </summary>
        /// <param name="config">Clearing house configuration with connection details.</param>
        /// <param name="fileName">Name of the file to send.</param>
        /// <param name="data">Stream containing the EDI data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Transmission result indicating success or failure.</returns>
        Task<TransmissionResult> SendAsync(ClearingHouseConfig config, string fileName, Stream data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Receives EDI response files from the clearing house.
        /// </summary>
        /// <param name="config">Clearing house configuration with connection details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of received files as streams with their file names.</returns>
        Task<List<(MemoryStream Data, string FileName)>> ReceiveAsync(ClearingHouseConfig config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the connection to the clearing house.
        /// </summary>
        /// <param name="config">Clearing house configuration with connection details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Transmission result indicating whether the connection is valid.</returns>
        Task<TransmissionResult> ValidateConnectionAsync(ClearingHouseConfig config, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file from the clearing house after processing.
        /// </summary>
        /// <param name="config">Clearing house configuration with connection details.</param>
        /// <param name="fileName">Name of the file to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the file was successfully deleted.</returns>
        Task<bool> DeleteAsync(ClearingHouseConfig config, string fileName, CancellationToken cancellationToken = default);
    }
}
