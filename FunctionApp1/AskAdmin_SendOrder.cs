using Core;
using CosmosDB;
using DB.Admin;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TTAskAdmin_SendOrder;
public class askadmin_sendorder
{
    [FunctionName(nameof(ExchangedOrders.OutgoingMessageType.askadmin_sendorder))]
    public static async Task Run([TimerTrigger("*/15 * * * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        string myappsettingsValue = await new CosmosDB.Config().GetValue("AzureCosmos");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var MainDBCollections = Base.PopulateMainCollection(DBLocation);
        var Configs = MainDBCollections[typeof(Configuration)];
        Configuration AdminEmail = (Configuration)Configs["AdminEmail"];

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
            SendAdmin(DBLocation, objPart, AdminEmail.Value);
        }
    }

    private static void SendAdmin(CosmoObject DBLocation, Part objPart, String AdminEmail)
    {
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
            WorkcenterID = "0",
            IsDirty = true,
        };

        // Puts it in the Grid
        objOrder.DateAdminLastNotified = DateTime.Now.Date;
        DBLocation.SaveCollection(typeof(Order), new[] { objOrder });

        var body = $"{objOrder.Message} {objOrder.VendorPartName} with a Quantity of: {objOrder.TotalAmountOrdered} and is needed by: {objOrder.RequiredBy.GetValueOrDefault(DateTime.MinValue)}";
        if (objPart.DateRequiredBy.GetValueOrDefault(DateTime.MinValue) < DateTime.Now.Date) body += " WARNING: The Date Required is in the Past, we will request the current date plus lead time";
        body += $"<BR><BR>Reply to this Email to Proceed. (Subject must be: 'Re: VENDOR ORDER NEEDED:')).";

        // Purposes of Emailing (for easy splitting)
        string ToAddress = AdminEmail;
        string Subject = ExchangedOrders.SetSubject(ExchangedOrders.OutgoingMessageType.askadmin_sendorder) + objOrder.id;
        string Body = body;

        string OutgoingQueueMessage = $"{ToAddress}||{Subject}||{Body}";
        CosmosDB.AzureQueue.SendToService("AzureWebJobsStorage","sendadmin", OutgoingQueueMessage);
    }
}
