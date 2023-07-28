using Azure.Storage.Queues;

namespace CosmosDB
{
    public class AzureQueue : Interfaces.IQueueService
    {
        static QueueClient _client = null;

        public AzureQueue(string Connection, string QueueName) 
        {
            _client = new QueueClient(Connection, QueueName);
        }

        public void SendToService(IBase Item)
        {
            _client.SendMessage(System.Text.Json.JsonSerializer.Serialize(Item));
        }
    }
}
