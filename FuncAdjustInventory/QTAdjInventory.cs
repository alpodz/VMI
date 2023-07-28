using System;
using System.Linq;
using DB.Admin;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QTAdjInventory
{
    public class QTAdjInventory
    {
        [FunctionName("QTAdjInventory")]
        public void Run([QueueTrigger("AdjInventory")] Order order, ILogger log, ExecutionContext context)
        {
            var configurationBuilder = new ConfigurationBuilder()
                 .SetBasePath(context.FunctionAppDirectory)
                 .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                 .AddEnvironmentVariables()
                 .Build();

            string myappsettingsValue = configurationBuilder["ConnectionStrings:CosmosDB"];

            var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
            var MainDBCollections = Base.PopulateMainCollection(DBLocation);

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (DBLocation == null || MainDBCollections == null) return;

            if (order.VendorOrder)
            {
                var objPart = (Part)MainDBCollections[typeof(Part)][order.PartID];
                if (objPart == null) return;
                objPart.InStock += order.TotalAmountOrdered;
                order.Message = "Vendor - Shipment Arrived.";
            }
            else
            {
                foreach (var objAssocParts in MainDBCollections[typeof(Recipe)].Values.Cast<Recipe>().Where(a => a.CreatedPartID == order.PartID))
                {
                    var objPart = (Part)MainDBCollections[typeof(Part)][objAssocParts.PartID];
                    if (objPart == null) return;
                    objPart.InStock -= objAssocParts.NumberOfParts;
                }
                order.Message = "Customer - Shipment Ready.";
            }

            Base.SaveCollection(DBLocation, typeof(Part), MainDBCollections[typeof(Part)]);

        }                
    }
}
