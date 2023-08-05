using System;
using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QTSendOrder;
public class SendOrder
{
    [FunctionName(nameof(ExchangedOrders.OutgoingMessageType.sendorder))]
    public static void Run(
        [QueueTrigger(nameof(ExchangedOrders.OutgoingMessageType.sendorder))] Order _order, 
        ILogger log, 
        ExecutionContext context, 
        [Queue(nameof(ExchangedOrders.IncomingMessageType.getreplyorder))] ExchangedOrders outorder,
        [Queue("sendauto")] string emailout
        )
    {
        var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

        string myappsettingsValue = configurationBuilder["ConnectionStrings:CosmosDB"];

        var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
        var MainDBCollections = Base.PopulateMainCollection(DBLocation);

        log.LogInformation($"C# queue trigger function processed message: {_order.CustomerOrderID}");
        // only process if these items are set
        if (MainDBCollections == null || !_order.DateOrdered.HasValue || string.IsNullOrEmpty(_order.PartID) || string.IsNullOrEmpty(_order.CustomerID) || string.IsNullOrEmpty(_order.id)) return;

        if (_order.VendorOrder)
        {
            var part = (Part)MainDBCollections[typeof(Part)][_order.PartID];
            var PullVendor = (Customer)MainDBCollections[typeof(Customer)][_order.CustomerID];
            if (PullVendor == null || part == null) return;

            if (!part.Populated) part.PopulateDerivedFields(DBLocation, ref MainDBCollections);
            if (part.AssignedVendorPart == null) return;

            _order.VendorPartName = part.AssignedVendorPart.VendorPartName;

            var OutgoingOrder = new ExchangedOrders()
            {
                OrderedOrderID = _order.id,
                OrderedPartName = _order.VendorPartName,
                OrderedPartTotal = _order.TotalAmountOrdered,
                to = PullVendor.EmailAddress,
            };
            // we're going to push the required by date because perhaps it's 'too late', we'll make it the current date
            var requiredby = part.DateRequiredBy.Value;
            if (requiredby < DateTime.Now.Date) requiredby = DateTime.Now.Date.AddDays(part.AssignedVendorPart.LeadDays);

            OutgoingOrder.RequiredBy = requiredby;
            if (_order.DateScheduled.HasValue) OutgoingOrder.RequiredBy = _order.DateScheduled.Value;

            if (String.IsNullOrEmpty(OutgoingOrder.to))
            {
                outorder = OutgoingOrder;
                return;
            }

            // format and send an email to the vendor email address
            emailout = "My Email Here";
        }
    }
}