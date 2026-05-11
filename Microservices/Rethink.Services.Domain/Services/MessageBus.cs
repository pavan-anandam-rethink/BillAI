using Azure.Messaging.ServiceBus;
using Rethink.Services.Common.Infrastructure.Connection;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Thon.Hotels.FishBus;

namespace Rethink.Services.Domain.Services
{
    public class MessageBus : IMessageBus
    {
        private readonly string _namespaceConnectionString;
        public MessageBus(IServiceBusConnectionFactory serviceBusConnectionFactory)
        {
            _namespaceConnectionString = serviceBusConnectionFactory.ConnectionStringBuilder.GetNamespaceConnectionString();
        }

        public async Task SendAsync<T>(T data, string entityPath)
        {
            var connectionString = $"{_namespaceConnectionString};EntityPath={entityPath}";
            var messagePublisher = new MessagePublisher(connectionString);

            await messagePublisher.SendAsync(data);
        }

        public async Task SendBatchAsync<T>(string entityName, List<T> batch)
        {
            await using var client = new ServiceBusClient(_namespaceConnectionString);
            await using var sender = client.CreateSender(entityName);
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();
            foreach (var message in batch)
            {
                string json = JsonSerializer.Serialize(message);
                messageBatch.TryAddMessage(new ServiceBusMessage(json));
            }
            await sender.SendMessagesAsync(messageBatch);
        }

        /// <summary>
        /// Sends messages in chunks to avoid exceeding Service Bus message size limits.
        /// </summary>
        /// <typeparam name="T">Batch DataType</typeparam>
        /// <param name="entityName">Topic/Queue</param>
        /// <param name="batch">Message to send</param>
        /// <param name="chunkSize">Chunk Size</param>
        /// <returns></returns>
        public async Task SendBatchAsync<T>(string entityName, List<T> batch, int chunkSize)
        {
            await using var client = new ServiceBusClient(_namespaceConnectionString);
            await using var sender = client.CreateSender(entityName);

            foreach (var chunk in batch.Chunk(chunkSize))
            {
                var tasks = new List<Task>();
                foreach (var message in chunk)
                {

                    // Cast to ClaimProcessRequestModel to access RequestModel.Ids
                    var messageId = Guid.NewGuid().ToString(); // Default fallback

                    var requestModelProp = message?.GetType().GetProperty("RequestModel")?.GetValue(message);
                    var idsProp = requestModelProp?.GetType().GetProperty("Ids")?.GetValue(requestModelProp) as int[];

                    if (idsProp != null && idsProp.Length > 0)
                    {
                        messageId = idsProp.First().ToString();
                    }

                    string json = JsonSerializer.Serialize(message);
                    var serviceBusMessage = new ServiceBusMessage(json)
                    {
                        // Set MessageId for deduplication and preventing retries
                        MessageId = messageId
                    };
                    tasks.Add(sender.SendMessageAsync(serviceBusMessage));
                }

                // Send chunk in parallel
                await Task.WhenAll(tasks);
            }
        }
    }
}
