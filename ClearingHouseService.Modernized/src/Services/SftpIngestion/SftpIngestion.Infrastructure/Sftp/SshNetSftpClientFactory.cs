using Microsoft.Extensions.Logging;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Infrastructure.Sftp;

public class SshNetSftpClientFactory : ISftpClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SshNetSftpClientFactory(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

    public ISftpClientWrapper CreateClient(string host, int port, string userName, string password)
    {
        return new SshNetSftpClientWrapper(host, port, userName, password, _loggerFactory.CreateLogger<SshNetSftpClientWrapper>());
    }
}
