using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Functions.Emails.Incoming;
public static class completeorder
{
    [FunctionName(nameof(ExchangedOrders.IncomingMessageType.completeorder))]
    public static async Task Run([QueueTrigger(nameof(ExchangedOrders.IncomingMessageType.completeorder))] InProgressOrder response, ILogger log)
    {
        if (response == null) return;
        string myappsettingsValue = await new CosmosDB.Config().GetValue("AzureCosmos");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var Orders = Base.PopulateDictionary(DBLocation, typeof(Order), response.OrderedOrderID);

        // update the order placed
        if (Orders.TryGetValue(response.OrderedOrderID, out var founditem))
        {
            Order orig = (Order)founditem;
            if (response.orders.Count == 1 && response.orders[0].TotalAmountOrdered == 0) // negative response?
            {
                orig.Message = response.orders[0].Message;
                orig.DateOrdered = null;
                orig.IsDirty = true;
            }
            // success
            else
            {
                // we have an existing order and possiblely other shipments, we will need to create additional shipments on customer side
                // single order or first order
                if (response.orders.Count >= 1)
                {
                    var order = response.orders[0];
                    orig.DateScheduled = order.DateScheduled;
                    //orig.ShipmentAmount = order.ShipmentAmount;
                    //orig.Shipment = order.Shipment;
                    orig.TotalAmountOrdered = order.TotalAmountOrdered;
                    orig.Message = order.Message;
                    orig.CustomerOrderID = order.id;
                    orig.IsDirty = true;
                    response.orders.Remove(order);
                }

                // loop through remaining orders and create new orders
                foreach (var order in response.orders)
                {
                    order.CustomerID = orig.CustomerID;
                    var guid = Guid.NewGuid().ToString();
                    order.id = guid;
                    order.PartID = orig.PartID;
                    order.CustomerOrderID = order.id;
                    order.WorkcenterID = "0";
                    order.IsDirty = true;
                    Orders.Add(guid, order);
                }
            }

            Base.SaveCollection(DBLocation, typeof(Order), Orders);
        }

    }
}
