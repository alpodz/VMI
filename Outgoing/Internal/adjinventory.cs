using DB.Admin;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Internal
{
    public class adjinventory
    {
        [FunctionName(nameof(adjinventory))]
        public async Task Run([QueueTrigger(nameof(adjinventory))] Order order, ILogger log)
        {
            string myappsettingsValue = await new CosmosDB.Config().GetValue("AzureCosmos");
            var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (order.VendorOrder)
            {
                var Parts = Base.PopulateDictionary(DBLocation, typeof(Part), order.PartID);
                if (Parts.Count == 0) return;
                var objPart = (Part)Parts[order.PartID];
                objPart.InStock += order.TotalAmountOrdered;
                order.Message = "Vendor - Shipment Arrived.";
                Base.SaveCollection(DBLocation, typeof(Part), Parts);
            }
            else
            {
                var Recipe = Base.PopulateDictionary(DBLocation, typeof(Recipe));
                foreach (var objAssocParts in Recipe.Cast<Recipe>().Where(a => a.CreatedPartID == order.PartID))
                {
                    var Parts = Base.PopulateDictionary(DBLocation, typeof(Part), objAssocParts.PartID);
                    if (Parts.Count == 0) return;
                    var objPart = (Part)Parts[objAssocParts.PartID];
                    objPart.InStock -= objAssocParts.NumberOfParts;
                    Base.SaveObject(DBLocation, typeof(Part), objPart);
                }
                order.Message = "Customer - Shipment Ready.";
            }
            Base.SaveObject(DBLocation, typeof(Order), order);

        }
    }
}
