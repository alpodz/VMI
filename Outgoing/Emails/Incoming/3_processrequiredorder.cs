using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class processrequiredorder
{
    [FunctionName(nameof(ExchangedOrders.IncomingMessageType.processrequiredorder))]
    public static async Task Run(
        [QueueTrigger(nameof(ExchangedOrders.IncomingMessageType.processrequiredorder))] InProgressOrder response, 
        ILogger log)
    {
        if (response == null) return;
        string myappsettingsValue = await new CosmosDB.Config().GetValue("AzureCosmos");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var _Order =(Order)await DBLocation.GetObjectAsync<Order>(response.OrderedOrderID);

        if (_Order == null) return;
        ((IBase)_Order).SendOrderService = new CosmosDB.AzureQueue(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), nameof(ExchangedOrders.OutgoingMessageType.generateorder));
        _Order.DateOrdered = DateTime.Now.Date;
        await DBLocation.SaveObjectAsync<Order>(_Order);
    }
}
