using Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading.Tasks;
using static Core.ExchangedOrders;

namespace QTGetEmail;
public class getmail
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="myQueueItem">Email Queued by Logic App, each item seperated by a || (From, Subject, Body)</param>
    /// <param name="log"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    [FunctionName(nameof(getmail))]
    public async Task Run([QueueTrigger(nameof(getmail))]string myQueueItem, ILogger log, CloudQueueClient client)
    {        
        var SplitString = myQueueItem.Split("||");
        var From = SplitString[0];
        var Subject = SplitString[1];
        var Body = SplitString[2];
        
        var IncomingMessage = ExchangedOrders.ParseSubject(Subject).ToString();
        if (string.IsNullOrEmpty(IncomingMessage)) return;
        var queue = client.GetQueueReference(IncomingMessage);
        await queue.CreateIfNotExistsAsync();
        await queue.AddMessageAsync(new CloudQueueMessage(myQueueItem));            
    }
}
