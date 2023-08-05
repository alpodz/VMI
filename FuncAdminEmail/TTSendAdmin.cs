using Core;
using DB.Admin;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TTSendAdmin
{
    public class TTSendAdmin
    {
        /// <summary>
        /// RequestVendorOrder Out -   Man VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
        /// OldUnOrdered                 Out -   Man PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
        /// OldUnScheduled               Out -   Man PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
        ///-- SendAdminCustomerFail Out -   Man ORDER FAILURE           -   Non-Vendor Order - Order Date, Not Scheduled(Message)
        ///-- SendAdminVendorFail Out -   Man ORDER FAILURE           -   Vendor Order - Order Date, Not Scheduled(Message)
        /// OldUnCompleted Out -   Man PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        /// <param name="context"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        [FunctionName(nameof(TTSendAdmin))]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context, CloudStorageAccount account)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            //var configurationBuilder = new ConfigurationBuilder()
            //     .SetBasePath(context.FunctionAppDirectory)
            //     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            //     .AddEnvironmentVariables()
            //     .Build();

            //string myappsettingsValue = configurationBuilder["ConnectionStrings:CosmosDB"];
            var myappsettingsValue = Environment.GetEnvironmentVariable("ConnectionStrings:CosmosDB");
            var DBLocation = new CosmosDB.CosmoObject(myappsettingsValue);
            var Orders = Base.PopulateDictionary(DBLocation, typeof(Order));
            var Configs = Base.PopulateDictionary(DBLocation, typeof(Configuration));

            var client = account.CreateCloudQueueClient();
            var queue = client.GetQueueReference("sendadmin");
            await queue.CreateIfNotExistsAsync();

            // only process if these items are set
            if (Orders == null || Configs == null) return;
            var multipleorders = new Dictionary<ExchangedOrders.OutgoingMessageType, List<Order>>();

            foreach (Order objOrd in Orders.Values.Cast<Order>())
            {
                if (objOrd.DateAdminLastNotified.HasValue && objOrd.DateAdminLastNotified.Value.Date == DateTime.Now.Date) continue;
                ExchangedOrders.OutgoingMessageType MessageType = ExchangedOrders.OutgoingMessageType.Unknown;
                if (!objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
                    MessageType = ExchangedOrders.OutgoingMessageType.RemindAdmin_UnOrdered;
                else if (objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
                    MessageType = ExchangedOrders.OutgoingMessageType.RemindAdmin_UnScheduled;
                else if (objOrd.DateOrdered.HasValue && objOrd.DateScheduled.HasValue && objOrd.DateScheduled < DateTime.Now.Date && !objOrd.DateCompleted.HasValue)
                    MessageType = ExchangedOrders.OutgoingMessageType.RemindAdmin_UnCompleted;
                if (MessageType == ExchangedOrders.OutgoingMessageType.Unknown) continue;
                if (!multipleorders.ContainsKey(MessageType)) multipleorders.Add(MessageType, new List<Order>());
                multipleorders[MessageType].Add(objOrd);
            }

            foreach (var Message in multipleorders)
            {
                var body = "<HTML><BODY>";
                foreach (var order in Message.Value)
                {
                    var typeofOrder = "Customer";
                    if (order.VendorOrder) typeofOrder = "Vendor";
                    body += $"{typeofOrder} Order: {order.id} ";

                    switch (Message.Key)
                    {
                        case ExchangedOrders.OutgoingMessageType.RemindAdmin_UnCompleted:
                            body += $" was scheduled to arrive or be completed for {order.DateScheduled} but has not been marked completed.";
                            break;
                        case ExchangedOrders.OutgoingMessageType.RemindAdmin_UnScheduled:
                            body += $" has failed to be ordered and/or scheduled.";
                            break;
                        case ExchangedOrders.OutgoingMessageType.RemindAdmin_UnOrdered:
                        default:
                            body += $" has not been ordered yet.";
                            break;
                    }

                    if (!String.IsNullOrWhiteSpace(order.Message)) body += $" Message: {order.Message}";
                    body += "<BR>";

                    order.DateAdminLastNotified = DateTime.Now.Date;

                    // Purposes of Emailing (for easy splitting)
                    string ToAddress = ((Configuration) Configs["Admin"]).Value;
                    string Subject = ExchangedOrders.SetSubject(Message.Key) + "Summary";
                    string Body = body + "</BODY></HTML>";

                    string OutgoingQueueMessage = $"{ToAddress}||{Subject}||{Body}";

                    var message = new CloudQueueMessage(OutgoingQueueMessage);
                    await queue.AddMessageAsync(message);                   

                    DBLocation.SaveCollection(typeof(Order), new[] { order });
                }               
            }
        }

    }
}


