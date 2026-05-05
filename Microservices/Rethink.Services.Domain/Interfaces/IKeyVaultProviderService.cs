using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces
{
    public interface IKeyVaultProviderService
    {
        Task<string> GetSecretAsync(string secretName);
    }
}
