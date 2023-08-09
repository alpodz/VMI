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
        var Orders = Base.PopulateDictionary(DBLocation, typeof(Order), response.OrderedOrderID);

        if (!Orders.TryGetValue(response.OrderedOrderID, out var founditem)) return;

        founditem.SendOrderService = new CosmosDB.AzureQueue(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), nameof(ExchangedOrders.OutgoingMessageType.generateorder));
        Order ord = (Order)founditem;
        ord.DateOrdered = DateTime.Now.Date;
                
        Base.SaveObject(DBLocation, typeof(Order), ord);
    }
}
