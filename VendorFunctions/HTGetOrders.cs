using DB.Vendor;
using Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace HTGetOrders;
public class HTGetOrders
{
    private IDBObject DBLocation;
    private Dictionary<Type, Dictionary<string, IBase>> MainDBCollections;

    [FunctionName("HTGetOrders")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string name = req.Query["name"];

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name = name ?? data?.name;

        GetOrders(null);

        string responseMessage = string.IsNullOrEmpty(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            : $"Hello, {name}. This HTTP triggered function executed successfully.";

        return new OkObjectResult(responseMessage);
    }

    private bool GetOrders(ExchangedOrders request)
    {
        if (request == null) return false;
        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        MainDBCollections = Base.PopulateMainCollection(DBLocation);

        // make/schedule orders
        CheckFulfillment(request);

        // save the incoming order with a new order id, however save the customer order id
        foreach (var order in request.orders)
        {
            //order.Shipment = " of " + request.orders.Count;
            order.TotalAmountOrdered = request.OrderedPartTotal;
            order.CustomerOrderID = order.id;
            order.id = Guid.NewGuid().ToString();
            MainDBCollections[typeof(Order)].Add(order.id, order);
            Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
        }
        //request.body = "Order Response";
        //mail.SendAuto(Exchange.EnuSendAuto.SendCustomerOrderResponse, request);
        return true;
    }

    public bool CheckFulfillment(ExchangedOrders request)
    {
        request.orders.Clear();
        // create the incoming order 
        var incomingOrder = new Order()
        { id = request.OrderedOrderID, VendorOrder = false, TotalAmountOrdered = request.OrderedPartTotal, DateOrdered = DateTime.Now.Date, Message = string.Empty, RequiredBy = request.RequiredBy };
        try
        {
            if (!CheckUserName(request.from, out var Customer))
                incomingOrder.Message += $"Unregistered User for email: {request.from} |";
            else
                incomingOrder.CustomerID = Customer.id;

            Part PartID = MainDBCollections[typeof(Part)].Values.Cast<Part>().FirstOrDefault(a => a.Name == request.OrderedPartName);
            if (PartID == null)
                incomingOrder.Message += $"Part {request.OrderedPartName} does not exist. |";
            else
                incomingOrder.PartID = PartID.id;

            var workcenters = MainDBCollections[typeof(WorkcenterPart)].Values.Cast<WorkcenterPart>().Where(a => a.PartID == PartID.id).OrderBy(a => a.PriorityLevel).ToList();
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
                    var outgoingOrder = WorkCenterScheduling.WorkCenterScheduling.SchedulePartOnWorkCenter(ref MainDBCollections, ShipmentNumber, PartsInShipment, BeginDateOfProduction, EndDateOfProduction, rwWorkCenter);
                    if (outgoingOrder != null)
                    {
                        PartsLeftToShip -= PartsInShipment;
                        outgoingOrder.CustomerID = Customer.id;
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


    private bool CheckUserName(string strUserName, out Customer customer)
    {
        customer = MainDBCollections[typeof(Customer)].Values.Cast<Customer>()
           .FirstOrDefault(a => a.EmailAddress == strUserName);
        return customer != null;
    }

}