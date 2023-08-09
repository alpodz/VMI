using Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace Functions.Emails.Incoming;
public class incomingmail
{
    /// <summary>
    /// Incoming Email is processed from the Queue: 'incomingmail'
    /// 
    /// Redirects Email down three paths:
    /// getreplyemail
    /// getsendorder
    /// getsendemailsenderorder
    /// 
    /// </summary>
    /// <param name="myQueueItem">Email Queued by Logic App, each item seperated by a || (From, Subject, Body)</param>
    /// <param name="log"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    [FunctionName(nameof(incomingmail))]
    public void Run([QueueTrigger(nameof(incomingmail))] string myQueueItem, ILogger log)
    {
        var SplitString = myQueueItem.Split("||");
        var From = SplitString[0];
        var Subject = SplitString[1];
        var Body = SplitString[2];
        string IncomingMessage = ExchangedOrders.ParseSubject(Subject).ToString();

        InProgressOrder IncomingOrder = new InProgressOrder
        {
            from = From,
            body = Body,
            OrderedOrderID = Subject.Substring(Subject.IndexOf(IncomingMessage), 36)
        };

        if (string.IsNullOrEmpty(IncomingMessage)) return;
        CosmosDB.AzureQueue.SendToService("AzureWebJobsStorage", IncomingMessage, myQueueItem);
    }
}
