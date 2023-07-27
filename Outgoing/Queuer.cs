using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Outgoing
{
    public class OutgoingQueuer
    {
        [FunctionName("Function1")]
        public void Run([QueueTrigger("Outgoing", Connection = "OutgoingConnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
