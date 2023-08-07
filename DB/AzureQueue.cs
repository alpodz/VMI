using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using System.Runtime.CompilerServices;

namespace CosmosDB
{
    public class AzureQueue : Interfaces.IQueueService
    {
        static QueueClient? _client = null;
        
        public static void Init(string Connection, string QueueName)
        {
            if (_client != null) return;
            _client = new QueueClient(Connection, QueueName);
            _client.CreateIfNotExists();
        }

        public AzureQueue(string Connection, string QueueName) 
        {
            Init(Connection, QueueName);
        }

        public void SendToService(Object Item)
        {
            if (_client == null) return;
            _client.SendMessage(System.Text.Json.JsonSerializer.Serialize(Item));
        }

        public void SendToService(IBase Item)
        {
            if (_client == null) return;
            _client.SendMessage(System.Text.Json.JsonSerializer.Serialize(Item));
        }

        public void SendToService(String Item)
        {
            if (_client == null) return;
            _client.SendMessage(Item);
        }

        public static void SendToService(string Connection, string QueueName, IBase Item)
        {            
            Init(Connection,QueueName);
            if (_client == null) return;
            _client.SendMessage(System.Text.Json.JsonSerializer.Serialize(Item));
        }

        public static void SendToService(string Connection, string QueueName, String Item)
        {
            Init(Connection, QueueName);
            if (_client == null) return;           
            _client.SendMessage(Item);
        }

        public static void SendToService(string Connection, string QueueName, Object Item)
        {
            Init(Connection, QueueName);
            if (_client == null) return;
            _client.SendMessage(System.Text.Json.JsonSerializer.Serialize(Item));
        }



    }
}
