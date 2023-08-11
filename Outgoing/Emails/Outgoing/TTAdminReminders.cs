using Core;
using DB.Admin;
using DB.Vendor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

    public class TTAdminReminders
    {
    /// <summary>
    /// Emails via the Queue:  "sendadmin"
    /// 
    /// RequestVendorOrder      VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
    /// OldUnOrdered            PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
    /// OldUnScheduled          PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
    /// OldUnCompleted Out      PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date
    
    /// Prospective:
    ///-- SendAdminCustomerFail ORDER FAILURE           -   Non-Vendor Order - Order Date, Not Scheduled(Message)
    ///-- SendAdminVendorFail   ORDER FAILURE           -   Vendor Order - Order Date, Not Scheduled(Message)
    /// </summary>
    [FunctionName(nameof(TTAdminReminders))]
        public async Task Run([TimerTrigger("0 */3 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var appsettings = await new CosmosDB.Config().GetValue("AzureCosmos");
            var DBLocation = new CosmosDB.CosmoObject(appsettings);
            var Orders = await DBLocation.PopulateTypeCollection(typeof(Order));
            var config = (Configuration) await DBLocation.GetObjectAsync<Configuration>(nameof(ExchangedOrders.RequiredConfiguration.AdminEmail));
        
            // only process if these items are set
            if (Orders == null || config == null) return;
            var multipleorders = new Dictionary<ExchangedOrders.OutgoingMessageType, List<Order>>();
            var adminEmail = config.Value;

        foreach (Order objOrd in Orders.Cast<Order>())
            {
                if (objOrd.DateAdminLastNotified.HasValue && objOrd.DateAdminLastNotified.Value.Date == DateTime.Now.Date) continue;
                ExchangedOrders.OutgoingMessageType MessageType = ExchangedOrders.OutgoingMessageType.Unknown;
                if (!objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
                    MessageType = ExchangedOrders.OutgoingMessageType.remindadminunordered;
                else if (objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
                    MessageType = ExchangedOrders.OutgoingMessageType.remindadminunscheduled;
                else if (objOrd.DateOrdered.HasValue && objOrd.DateScheduled.HasValue && objOrd.DateScheduled < DateTime.Now.Date && !objOrd.DateCompleted.HasValue)
                    MessageType = ExchangedOrders.OutgoingMessageType.remindadminuncompleted;
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
                        case ExchangedOrders.OutgoingMessageType.remindadminuncompleted:
                            body += $" was scheduled to arrive or be completed for {order.DateScheduled} but has not been marked completed.";
                            break;
                        case ExchangedOrders.OutgoingMessageType.remindadminunscheduled:
                            body += $" has failed to be ordered and/or scheduled.";
                            break;
                        case ExchangedOrders.OutgoingMessageType.remindadminunordered:
                        default:
                            body += $" has not been ordered yet.";
                            break;
                    }

                    if (!String.IsNullOrWhiteSpace(order.Message)) body += $" Message: {order.Message}";
                    body += "<BR>";

                    order.DateAdminLastNotified = DateTime.Now.Date;

                // Purposes of Emailing (for easy splitting)
                string ToAddress = adminEmail;
                    string Subject = ExchangedOrders.SetSubject(Message.Key) + "Summary";
                    string Body = body + "</BODY></HTML>";

                    string OutgoingQueueMessage = $"{ToAddress}||{Subject}||{Body}";

                    CosmosDB.AzureQueue.SendToService("AzureWebJobsStorage", nameof(ExchangedOrders.OutgoingEmailType.sendadmin), OutgoingQueueMessage);

                    await DBLocation.SaveObjectAsync<Order>(order);
                }               
            }
        }

    }

