using Rethink.Services.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Jobs
{
    public sealed class BackgroundJobQueue : IBackgroundJobQueue
    {
        private readonly Channel<object> _queue = Channel.CreateUnbounded<object>();

        public ValueTask EnqueueAsync(object job)
            => _queue.Writer.WriteAsync(job);

        public ValueTask<object> DequeueAsync(CancellationToken cancellationToken)
            => _queue.Reader.ReadAsync(cancellationToken);
    }
}
