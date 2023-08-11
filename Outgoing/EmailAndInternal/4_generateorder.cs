using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public class generateorder
{

    // NOTE: WE NEED TO WRITE A MESSAGE TO THE VENDOR
    // The vendor should get a nice order message requiring their response, this will be processed through incomingemail
    // and be shuffled to 'completeorder'
    // 'sendauto' is the outgoing email.
    [FunctionName(nameof(ExchangedOrders.OutgoingMessageType.generateorder))]
    public static async Task Run(
        [QueueTrigger(nameof(ExchangedOrders.OutgoingMessageType.generateorder))] Order inorder, 
        ILogger log)
    {
        string myappsettingsValue = await new CosmosDB.Config().GetValue("AzureCosmos");
        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var MainDBCollections = await Base.PopulateMainCollection(DBLocation);

        log.LogInformation($"C# queue trigger function processed message: {inorder.CustomerOrderID}");
        // only process if these items are set
        if (MainDBCollections == null || !inorder.DateOrdered.HasValue || string.IsNullOrEmpty(inorder.PartID) || string.IsNullOrEmpty(inorder.CustomerID) || string.IsNullOrEmpty(inorder.id)) return;

        if (inorder.VendorOrder)
        {
            var part = (Part)MainDBCollections[typeof(Part)][inorder.PartID];
            var PullVendor = (Customer)MainDBCollections[typeof(Customer)][inorder.CustomerID];
            if (PullVendor == null || part == null) return;

            if (!part.Populated) part.PopulateDerivedFields(DBLocation, ref MainDBCollections);
            if (part.AssignedVendorPart == null) return;

            inorder.VendorPartName = part.AssignedVendorPart.VendorPartName;

            var OutgoingOrder = new InProgressOrder()
            {
                OrderedOrderID = inorder.id,
                OrderedPartName = inorder.VendorPartName,
                OrderedPartTotal = inorder.TotalAmountOrdered,
                to = PullVendor.EmailAddress,
            };
            // we're going to push the required by date because perhaps it's 'too late', we'll make it the current date
            var requiredby = part.DateRequiredBy.Value;
            if (requiredby < DateTime.Now.Date) requiredby = DateTime.Now.Date.AddDays(part.AssignedVendorPart.LeadDays);

            OutgoingOrder.RequiredBy = requiredby;
            if (inorder.DateScheduled.HasValue) OutgoingOrder.RequiredBy = inorder.DateScheduled.Value;

            if (String.IsNullOrEmpty(OutgoingOrder.to))
                CosmosDB.AzureQueue.SendToService("AzureWebJobsStorage",  nameof(ExchangedOrders.IncomingMessageType.processorder), OutgoingOrder);
            else
                CosmosDB.AzureQueue.SendToService("AzureWebJobsStorage", nameof(ExchangedOrders.OutgoingEmailType.sendauto), "My Email Here");
        }
    }
}