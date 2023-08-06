using Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading.Tasks;

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

//// retrieves requests for parts, calculate if this is possible, and send a response -- 'queues and waits for confirmation'
//// also recieves confirmations, this will set up a shipment
//public ExchangedOrders RetrieveMail(MailMessage msg, string req)
//{
//    if (msg.Subject.StartsWith(SetSubject(req)))
//    {
//        var incomingOrder = new ExchangedOrders();
//        if (msg.Attachments.Count == 1)
//        {
//            using (var att = msg.Attachments[0])
//            using (StreamReader sr = new StreamReader(att.ContentStream))
//            {
//                incomingOrder = System.Text.Json.JsonSerializer.Deserialize<ExchangedOrders>(sr.ReadToEnd());
//            }
//        }
//        else
//            incomingOrder.OrderedOrderID = msg.Subject.Substring(SetSubject(req).Length, 36);

//        incomingOrder.subject = msg.Subject;
//        incomingOrder.from = msg.From.Address;
//        incomingOrder.body = msg.Body;

//        if (!string.IsNullOrEmpty(incomingOrder.OrderedOrderID))
//            return incomingOrder;
//    }
//    return null;
//}

//public void SendAuto(EnuSendAuto req, ExchangedOrders order)
//{
//    string seriallized = System.Text.Json.JsonSerializer.Serialize(order);
//    byte[] buffer = Encoding.ASCII.GetBytes(seriallized);
//    using (var str = new MemoryStream(buffer))
//    using (var attach = new Attachment(str, order.OrderedOrderID + ".json"))
//        Client.SendEmail(attach, order.to, SetSubject(req.ToString()) + order.OrderedOrderID, order.body, false);
//}

//public void SendAdmin(EnuSendAdmin req, string orderID, string body)
//{
//    try
//    {
//        Client.SendEmail(null, ConfigHelper.Values["admin"], SetSubject(req.ToString()) + orderID, body, true);
//    }
//    catch
//    {

//    }
//}