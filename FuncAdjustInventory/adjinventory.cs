using DB.Admin;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace QTAdjInventory
{
    public class adjinventory
    {
        [FunctionName(nameof(adjinventory))]
        public void Run([QueueTrigger(nameof(adjinventory))] Order order, ILogger log, ExecutionContext context)
        {
            var configurationBuilder = new ConfigurationBuilder()
                 .SetBasePath(context.FunctionAppDirectory)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            string myappsettingsValue = configurationBuilder["ConnectionStrings:CosmosDB"];

            var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
            var Parts = Base.PopulateDictionary(DBLocation, typeof(Part));
            var Recipe = Base.PopulateDictionary(DBLocation, typeof(Recipe));

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (DBLocation == null || Parts == null || Recipe == null) return;

            if (order.VendorOrder)
            {
                var objPart = (Part)Parts[order.PartID];
                if (objPart == null) return;
                objPart.InStock += order.TotalAmountOrdered;
                order.Message = "Vendor - Shipment Arrived.";
            }
            else
            {
                foreach (var objAssocParts in Recipe.Cast<Recipe>().Where(a => a.CreatedPartID == order.PartID))
                {
                    var objPart = (Part)Parts[objAssocParts.PartID];
                    if (objPart == null) return;
                    objPart.InStock -= objAssocParts.NumberOfParts;
                }
                order.Message = "Customer - Shipment Ready.";
            }

            Base.SaveCollection(DBLocation, typeof(Part), Parts);
        }                
    }
}
