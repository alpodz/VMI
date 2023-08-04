using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Core;

namespace Auto_GetCustomerOrder;
public class auto_getcustomerorder
{
    [FunctionName(nameof(auto_getcustomerorder))]
    public static void Run([QueueTrigger(nameof(auto_getcustomerorder))] ExchangedOrders request, ILogger log, ExecutionContext context,[Queue("auto_sendcustomerorderresponse")] ICollector<ExchangedOrders> outqueue)
    {
        if (request == null) return;
        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var DB = Base.PopulateMainCollection(DBLocation);

        // make/schedule orders
        CheckFulfillment(request, DB);

        // save the incoming order with a new order id, however save the customer order id
        foreach (var order in request.orders)
        {
            //order.Shipment = " of " + request.orders.Count;
            order.TotalAmountOrdered = request.OrderedPartTotal;
            order.CustomerOrderID = order.id;
            order.id = Guid.NewGuid().ToString();
            var Orders = new Dictionary<string, IBase>
            {
                { order.id, order }
            };
            Base.SaveCollection(DBLocation, typeof(Order), Orders);
        }
        outqueue.Add(request);
    }


    public static bool CheckFulfillment(ExchangedOrders request, Dictionary<Type, Dictionary<string, IBase>> DB)
    {
        request.orders.Clear();
        // create the incoming order 
        var incomingOrder = new Order()
        { id = request.OrderedOrderID, VendorOrder = false, TotalAmountOrdered = request.OrderedPartTotal, DateOrdered = DateTime.Now.Date, Message = string.Empty, RequiredBy = request.RequiredBy };
        try
        {
            var customer = DB[typeof(Customer)].Cast<Customer>().FirstOrDefault(a => a.EmailAddress == request.from);
            if (customer == null)
                incomingOrder.Message += $"Unregistered User for email: {request.from} |";
            else
                incomingOrder.CustomerID = customer.id;

            Part PartID = DB[typeof(Part)].Cast<Part>().FirstOrDefault(a => a.Name == request.OrderedPartName);
            if (PartID == null)
                incomingOrder.Message += $"Part {request.OrderedPartName} does not exist. |";
            else
                incomingOrder.PartID = PartID.id;

            var workcenters = DB[typeof(WorkcenterPart)].Cast<WorkcenterPart>().Where(a => a.PartID == PartID.id).OrderBy(a => a.PriorityLevel).ToList();
            if (workcenters.Count == 0)
                incomingOrder.Message += $"No Workcenters Setup for Part {request.OrderedPartName} |";

            if (!incomingOrder.RequiredBy.HasValue || incomingOrder.RequiredBy.Value.Date < DateTime.Now.Date)
                incomingOrder.Message += $"Required By Date is invalid {request.RequiredBy} |";

            if (incomingOrder.Message != string.Empty)
            {
                request.orders.Add(incomingOrder);
                return false;
            }

            var PartsLeftToShip = request.OrderedPartTotal;
            while (request.orders.Count <= 3 && PartsLeftToShip > 0)
            {
                var ShipmentNumber = request.orders.Count + 1;
                var PartsInShipment = (int)Math.Ceiling(request.OrderedPartTotal / (double)ShipmentNumber);
                int DaysUntilFullShipmentIsRequired = (int)Math.Ceiling(request.RequiredBy.Date.Subtract(DateTime.Now.Date).TotalDays);

                var DaysAllowedForProduction = DaysUntilFullShipmentIsRequired / ShipmentNumber;

                var BeginDateOfProduction = DateTime.Now.Date.AddDays(DaysAllowedForProduction * (ShipmentNumber - 1));
                var EndDateOfProduction = BeginDateOfProduction.Date.AddDays(DaysAllowedForProduction);

                foreach (var rwWorkCenter in workcenters)
                {
                    var outgoingOrder = WorkCenterScheduling.SchedulePartOnWorkCenter(ref DB, ShipmentNumber, PartsInShipment, BeginDateOfProduction, EndDateOfProduction, rwWorkCenter);
                    if (outgoingOrder != null)
                    {
                        PartsLeftToShip -= PartsInShipment;
                        outgoingOrder.CustomerID = customer.id;
                        outgoingOrder.RequiredBy = request.RequiredBy;
                        request.orders.Add(outgoingOrder);
                    }
                    if (PartsLeftToShip <= 0) return true;
                }

                if (PartsLeftToShip > 0)
                {
                    request.orders.Clear();
                    incomingOrder.Message += $"Unable to schedule the Part for delivery for {EndDateOfProduction}";
                    request.orders.Add(incomingOrder);
                    return false;
                }
            }

        }
        catch
        {

        }
        request.orders.Clear();
        incomingOrder.Message += "Unable to Schedule Part";
        request.orders.Add(incomingOrder);
        return false;
    }
}