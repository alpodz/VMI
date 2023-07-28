using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DB.Vendor;
using Interfaces;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Core;

namespace HTCheckForVendorOrderResponse;
public class HTCheckForVendorOrderResponse
{
    [FunctionName("HTCheckForVendorOrderResponse")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string name = req.Query["name"];

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);
        name = name ?? data?.name;

        CheckForVendorOrderResponse(null);

        string responseMessage = string.IsNullOrEmpty(name)
            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            : $"Hello, {name}. This HTTP triggered function executed successfully.";

        return new OkObjectResult(responseMessage);
    }

    private void CheckForVendorOrderResponse(ExchangedOrders response)
    {
        if (response == null) return;
        var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var MainDBCollections = new Dictionary<Type, Dictionary<String, IBase>>();
        MainDBCollections.Add(typeof(Order), Base.PopulateTypeCollection(DBLocation, typeof(Order)).Cast<IBase>().ToDictionary(a => a.GetPrimaryKeyValue()));

        // let's make sure it's a real order
        Order ord = null;
        if (MainDBCollections[typeof(Order)].ContainsKey(response.OrderedOrderID))
            ord = (Order)MainDBCollections[typeof(Order)][response.OrderedOrderID];
        if (ord != null)
        {
            // place order // update to address to vendor                
            ord.DateOrdered = DateTime.Now.Date;
            Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
        }
    }
}
