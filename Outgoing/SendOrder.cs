using System;
using System.Collections.Generic;
using Core;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VendorFunctions
{
    public class MainDB
    {
        public Dictionary<Type, Dictionary<string, Base>> LoadMainDB(ExecutionContext context)
        {
            var configurationBuilder = new ConfigurationBuilder()
         .SetBasePath(context.FunctionAppDirectory)
         .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables()
         .Build();

            string myappsettingsValue = configurationBuilder["ConnectionStrings:MyConnection"];

            //            if (email.Client == null) return;
            // query email server and do what you need to do:
            //              foreach (var msg in email.Client.Search(S22.Imap.SearchCondition.All()))
            //                ExecuteWorkAgainstMailMessage(msg);

            //          EmailAdminToDoStuff();
            //    }

            var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
            var MainDBCollections = Base.PopulateMainCollection(DBLocation);
            return MainDBCollections;
        }
    }

    public static class SendOrder
    {
        public static Dictionary<Type, Dictionary<string, Base>> MainDBCollections;

        [FunctionName("SendOrder")]
        public void Run([QueueTrigger("Outgoing", Connection = "OutgoingConnection")] Order _order, ILogger log, ExecutionContext context)
        {
            MainDBCollections = new MainDB().LoadMainDB(context);
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {_order.CustomerOrderID}");
                // only process if these items are set
                if (MainDBCollections == null || !_order.DateOrdered.HasValue || String.IsNullOrEmpty(_order.PartID) || String.IsNullOrEmpty(_order.CustomerID) || String.IsNullOrEmpty(_order.id)) return;

                if (_order.VendorOrder)
                {
                    var part = (Part)MainDBCollections[typeof(Part)][_order.PartID];
                    var PullVendor = (Customer)MainDBCollections[typeof(Customer)][_order.CustomerID];
                    if (PullVendor == null || part == null) return;

                    if (!part.Populated) part.PopulateDerivedFields(_order.DBLocation, ref MainDBCollections);
                    if (part.AssignedVendorPart == null) return;

                _order.VendorPartName = part.AssignedVendorPart.VendorPartName;

                    ExchangedOrders exchangedOrders = new ExchangedOrders()
                    {
                        OrderedOrderID = _order.id,
                        OrderedPartName = _order.VendorPartName,
                        OrderedPartTotal = _order.TotalAmountOrdered,
                        to = PullVendor.EmailAddress,
                        body = "Order Request"
                    };
                    // we're going to push the required by date because perhaps it's 'too late', we'll make it the current date
                    var requiredby = part.DateRequiredBy.Value;
                    if (requiredby < DateTime.Now.Date) requiredby = DateTime.Now.Date.AddDays(part.AssignedVendorPart.LeadDays);

                    exchangedOrders.RequiredBy = requiredby;
                    if (_order.DateScheduled.HasValue) exchangedOrders.RequiredBy = _order.DateScheduled.Value;

                    var mail = new Exchange(ref MainDBCollections);
                    mail.SendAuto(Exchange.EnuSendAuto.SendVendorOrder, exchangedOrders);
                }
            }
        }
    }
}
