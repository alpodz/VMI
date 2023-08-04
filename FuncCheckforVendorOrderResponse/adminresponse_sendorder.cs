using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
namespace QTAdminResponse_SendOrder;
public class adminresponse_sendorder
{
    [FunctionName(nameof(ExchangedOrders.IncomingMessageType.adminresponse_sendorder))]
    public static void Run(
        [QueueTrigger(nameof(ExchangedOrders.IncomingMessageType.adminresponse_sendorder))] ExchangedOrders response, 
        ILogger log, 
        ExecutionContext context)
    {
        if (response == null) return;
        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var Orders = Base.PopulateDictionary(DBLocation, typeof(Order));

        if (!Orders.TryGetValue(response.OrderedOrderID, out var founditem)) return;

        Order ord = (Order)founditem;
        ord.DateOrdered = DateTime.Now.Date;
                
        Base.SaveCollection(DBLocation, typeof(Order), Orders);
    }
}
