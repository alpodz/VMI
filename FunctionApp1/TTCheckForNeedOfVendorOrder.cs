using System;
using System.Linq;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TTCheckForNeedOfVendorOrder;
public class TTCheckForNeedOfVendorOrder
{
    [FunctionName("TTCheckForNeedOfVendorOrder")]
    public void Run([TimerTrigger("*/15 * * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        var configurationBuilder = new ConfigurationBuilder()
             .SetBasePath(context.FunctionAppDirectory)
             .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
             .AddEnvironmentVariables()
             .Build();

        string myappsettingsValue = configurationBuilder["ConnectionStrings:CosmosDB"];
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var MainDBCollections = Base.PopulateMainCollection(DBLocation);

        // only process if these items are set
        if (MainDBCollections == null) return;
        // calculate first
        foreach (var objPart in MainDBCollections[typeof(Part)].Values.Cast<Part>())
            objPart.CalculateFields(DBLocation, ref MainDBCollections);

        // schedule next
        foreach (var objPart in MainDBCollections[typeof(Part)].Values.Cast<Part>())
        {
            if (objPart.AssignedVendorPart == null) return;
            if (objPart.PullQuantity == 0) return;
            if (objPart.PullQuantity < objPart.InStock + objPart.OrderedAmt) return;
            // check existing orders
            if (MainDBCollections[typeof(Order)].Values.Cast<Order>().FirstOrDefault(a => a.PartID == objPart.id && a.VendorOrder == true && !a.DateOrdered.HasValue) != null) return;

            // we're going to push the required by date because perhaps it's 'too late', we'll make it the current date
            var requiredby = objPart.DateRequiredBy.Value;
            if (requiredby < DateTime.Now.Date) requiredby = DateTime.Now.Date.AddDays(objPart.AssignedVendorPart.LeadDays);
            var guid = Guid.NewGuid().ToString();

            var objOrder = new Order()
            {
                id = guid,
                CustomerID = objPart.AssignedVendorPart.CustomerID,
                PartID = objPart.id,
                VendorOrder = true,
                //ShipmentAmount = objPart.PullQuantity,
                TotalAmountOrdered = objPart.PullQuantity,
                //Shipment = "1 of 1",
                Message = $"Order Required!",
                RequiredBy = requiredby,
                VendorPartName = objPart.AssignedVendorPart.VendorPartName,
                WorkcenterID = "0"
            };
            // Puts it in the Grid
            MainDBCollections[typeof(Order)].Add(guid, objOrder);
            objOrder.DateAdminLastNotified = DateTime.Now.Date;
            Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);

            var body = $"{objOrder.Message} {objOrder.VendorPartName} with a Quantity of: {objOrder.TotalAmountOrdered} and is needed by: {objOrder.RequiredBy.GetValueOrDefault(DateTime.MinValue)}";
            if (objPart.DateRequiredBy.GetValueOrDefault(DateTime.MinValue) < DateTime.Now.Date) body += " WARNING: The Date Required is in the Past, we will request the current date plus lead time";
            //body += $"<BR><BR>Reply to this Email to Proceed. (Subject must be: {Exchange.SetSubject(Exchange.EnuReceiveAdmin.RequestVendorOrderResponse.ToString())}).";

            //email.SendAdmin(Exchange.EnuSendAdmin.RequestVendorOrder, objOrder.id, body);
        }


    }
}
