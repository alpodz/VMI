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
                var objPart =(Part) await DBLocation.GetObjectAsync<Part>(order.PartID);
                if (objPart == null) return;
                objPart.InStock += order.TotalAmountOrdered;
                order.Message = "Vendor - Shipment Arrived.";
                await DBLocation.SaveObjectAsync<Part>(objPart);
            }
            else
            {
                var Recipes = await DBLocation.PopulateTypeCollection(typeof(Recipe));
                foreach (var objAssocParts in Recipes.Cast<Recipe>().Where(a => a.CreatedPartID == order.PartID))
                {
                    var objPart =(Part) await DBLocation.GetObjectAsync<Part>(objAssocParts.PartID);
                    if (objPart == null) return;
                    objPart.InStock -= objAssocParts.NumberOfParts;
                    await DBLocation.SaveObjectAsync<Part>(objPart);
                }
                order.Message = "Customer - Shipment Ready.";
            }
            await DBLocation.SaveObjectAsync<Order>(order);

        }
    }
}
