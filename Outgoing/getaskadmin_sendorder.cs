using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
namespace QTAdminResponse_SendOrder;
public class getaskadmin_sendorder
{
    [FunctionName(nameof(ExchangedOrders.IncomingMessageType.getaskadmin_sendorder))]
    public static void Run(
        [QueueTrigger(nameof(ExchangedOrders.IncomingMessageType.getaskadmin_sendorder))] ExchangedOrders response, 
        ILogger log)
    {
        if (response == null) return;
        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var Orders = Base.PopulateDictionary(DBLocation, typeof(Order));

        if (!Orders.TryGetValue(response.OrderedOrderID, out var founditem)) return;

        founditem.SendOrderService = new CosmosDB.AzureQueue(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "sendorder");
        Order ord = (Order)founditem;
        ord.DateOrdered = DateTime.Now.Date;
                
        Base.SaveCollection(DBLocation, typeof(Order), Orders);
    }
}
