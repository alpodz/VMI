using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace QTAdminResponse_SendOrder;
public class getaskadmin_sendorder
{
    [FunctionName(nameof(ExchangedOrders.IncomingMessageType.getaskadmin_sendorder))]
    public static async Task Run(
        [QueueTrigger(nameof(ExchangedOrders.IncomingMessageType.getaskadmin_sendorder))] ExchangedOrders response, 
        ILogger log)
    {
        if (response == null) return;
        string myappsettingsValue = await new CosmosDB.Config().GetValue("AzureCosmos");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var Orders = Base.PopulateDictionary(DBLocation, typeof(Order));

        if (!Orders.TryGetValue(response.OrderedOrderID, out var founditem)) return;

        founditem.SendOrderService = new CosmosDB.AzureQueue(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "sendorder");
        Order ord = (Order)founditem;
        ord.DateOrdered = DateTime.Now.Date;
                
        Base.SaveCollection(DBLocation, typeof(Order), Orders);
    }
}
