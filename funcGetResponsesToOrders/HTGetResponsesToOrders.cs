using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Core;
using DB.Vendor;
using System.Collections.Generic;
using System.Linq;

namespace HTGetResponsesToOrders;
public static class HTGetResponsesToOrders
{
    [FunctionName("HTGetResponsesToOrders")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string name = req.Query["name"];

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name = name ?? data?.name;

        GetResponsesToOrder(null);

        string responseMessage = string.IsNullOrEmpty(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            : $"Hello, {name}. This HTTP triggered function executed successfully.";

        return new OkObjectResult(responseMessage);
    }

    private static bool GetResponsesToOrder(ExchangedOrders response)
    {
        if (response == null) return false;

        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var MainDBCollections = new Dictionary<Type, Dictionary<String, IBase>>();
        MainDBCollections.Add(typeof(Order), Base.PopulateTypeCollection(DBLocation, typeof(Order)).Cast<IBase>().ToDictionary(a => a.GetPrimaryKeyValue()));

        // update the order placed
        Order orig = (Order)MainDBCollections[typeof(Order)][response.OrderedOrderID];
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
                    MainDBCollections[typeof(Order)].Add(guid, order);
                }
            }

            Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
        }
        return true;
    }
}
