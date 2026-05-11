using MediatR;
using Microsoft.Extensions.Logging;

namespace EdiProcessing.Application.Queries.GetProcessingResult;

public class GetProcessingResultQueryHandler : IRequestHandler<GetProcessingResultQuery, ProcessingResultDto?>
{
    private readonly ILogger<GetProcessingResultQueryHandler> _logger;

    public GetProcessingResultQueryHandler(ILogger<GetProcessingResultQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessingResultDto?> Handle(GetProcessingResultQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting processing result for file {FileId}", request.FileId);
        await Task.CompletedTask;
        return null;
    }
}
