using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

namespace Auto_GetOrderResponse;
public static class auto_getorderresponse
{
    [FunctionName(nameof(auto_getorderresponse))]
    public static void Run([QueueTrigger(nameof(auto_getorderresponse))] ExchangedOrders response, ILogger log, ExecutionContext context)
    {
        if (response == null) return;

        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var Orders = Base.PopulateDictionary(DBLocation, typeof(Order));

        // update the order placed
        Order orig = (Order) Orders[response.OrderedOrderID];
        if (orig != null)
        {
            if (response.orders.Count == 1 && response.orders[0].TotalAmountOrdered == 0) // negative response?
            {
                orig.Message = response.orders[0].Message;
                orig.DateOrdered = null;
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
                    Orders.Add(guid, order);
                }
            }

            Base.SaveCollection(DBLocation, typeof(Order), Orders);
        }

    }
}
