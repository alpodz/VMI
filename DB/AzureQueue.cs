using Azure.Storage.Queues;
using System.Reflection.Metadata.Ecma335;

namespace CosmosDB
{
    public class AzureQueue : Interfaces.IQueueService
    {
        static QueueClient? _client = null;

        public AzureQueue(string Connection, string QueueName) 
        {
            _client = new QueueClient(Connection, QueueName);
            _client.CreateIfNotExists();
        }

        public void SendToService(IBase Item)
        {
            if (_client == null) return;
            _client.SendMessage(System.Text.Json.JsonSerializer.Serialize(Item));
        }
    }
}
