//using System;
//using System.Collections.Generic;
//using System.Net.Mail;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Extensions.Logging;

//namespace FuncAdminEmail
//{
//    public class Function1
//    {
//        [FunctionName("Function1")]
//        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
//        {
//            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");


//        }

//        //private void ExecuteWorkAgainstMailMessage(uint mailnum)
//        //{
//        //    lock (Inventory.EmaiLLock)
//        //    {
//        //        // possiblely already deleted // we'll catch the exception returning and just 'eat it'
//        //        using (MailMessage mail = email.Client.GetMessage(mailnum))
//        //        {
//        //            // apparently deleted messages are loaded but have no sender
//        //            if (mail == null || mail.From == null) return;
//        //            CheckEmailForVariousResponses(mail);
//        //        }
//        //        email.Client.DeleteMessage(mailnum);
//        //    }
//        //}

//        private void CheckEmailForVariousResponses(MailMessage mail)
//        {
//            // RequestVendorOrderResponse   In  -   Man     Re: VENDOR ORDER NEEDED -   Get Response From Admin              
//            // SendVendorOrder              Out -   Auto    ORDER                   -   Send Vendor Order
//            if (CheckForVendorOrderResponse(Exchange.RetrieveMail(mail, Exchange.EnuReceiveAdmin.RequestVendorOrderResponse.ToString()))) return;
//            // ReceiveCustomerOrder         In  -   Auto    ORDER                   -   Receive Vendor Order / Customer Order
//            // SendCustomerOrderResponse    Out -   Auto    Re: ORDER               -   Send Order Response
//            //if (GetOrders(email, Exchange.RetrieveMail(mail, Exchange.EnuRecieveAuto.RecieveCustomerOrder.ToString()))) return;
//            // ReceiveOrderResponse         In  -   Auto    Re: ORDER               -   Receive Order Response
//            //if (GetResponsesToOrders(Exchange.RetrieveMail(mail, Exchange.EnuRecieveAuto.RecieveOrderResponse.ToString()))) return;
//        }

//        private void EmailAdminToDoStuff()
//        {
//            // only process if these items are set
//            if (MainDBCollections == null) return;
//            var multipleorders = new Dictionary<Exchange.EnuSendAdmin, List<Order>>();

//            // RequestVendorOrder           Out -   Man     VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
//            // OldUnOrdered                 Out -   Man     PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
//            // OldUnScheduled               Out -   Man     PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
//            // -- SendAdminCustomerFail     Out -   Man     ORDER FAILURE           -   Non-Vendor Order - Order Date, Not Scheduled (Message)
//            // -- SendAdminVendorFail       Out -   Man     ORDER FAILURE           -   Vendor Order - Order Date, Not Scheduled (Message)
//            // OldUnCompleted               Out -   Man     PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date

//            var Orders = MainDBCollections[typeof(Order)].Values.Cast<Order>();
//            foreach (var objOrd in Orders)
//            {
//                if (!objOrd.DateAdminLastNotified.HasValue || (objOrd.DateAdminLastNotified.HasValue && objOrd.DateAdminLastNotified < DateTime.Now.Date))
//                {
//                    if (!objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
//                    {
//                        // OldUnOrdered -- Queue Up
//                        // if (!multipleorders.ContainsKey(Exchange.EnuSendAdmin.OldUnOrdered)) multipleorders.Add(Exchange.EnuSendAdmin.OldUnOrdered, new List<Order>());
//                        // multipleorders[Exchange.EnuSendAdmin.OldUnOrdered].Add(objOrd);
//                    }
//                    else if (objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
//                    {
//                        // OldUnScheduled -- Queue Up
//                        // if (!multipleorders.ContainsKey(Exchange.EnuSendAdmin.OldUnScheduled)) multipleorders.Add(Exchange.EnuSendAdmin.OldUnScheduled, new List<Order>());
//                        //    multipleorders[Exchange.EnuSendAdmin.OldUnScheduled].Add(objOrd);
//                    }
//                    else if (objOrd.DateOrdered.HasValue && objOrd.DateScheduled.HasValue && objOrd.DateScheduled < DateTime.Now.Date && !objOrd.DateCompleted.HasValue)
//                    {
//                        // OldUnCompleted -- Queue Up
//                        // if (!multipleorders.ContainsKey(Exchange.EnuSendAdmin.OldUnCompleted)) multipleorders.Add(Exchange.EnuSendAdmin.OldUnCompleted, new List<Order>());
//                        // multipleorders[Exchange.EnuSendAdmin.OldUnCompleted].Add(objOrd);
//                    }
//                }
//            }

//            foreach (var queue in multipleorders)
//            {
//                var body = "<HTML><BODY>";
//                foreach (var order in queue.Value)
//                {
//                    var typeofOrder = "Customer";
//                    if (order.VendorOrder) typeofOrder = "Vendor";
//                    body += $"{typeofOrder} Order: {order.id} ";

//                    switch (queue.Key)
//                    {
//                        case Exchange.EnuSendAdmin.OldUnCompleted:
//                            body += $" was scheduled to arrive or be completed for {order.DateScheduled} but has not been marked completed.";
//                            break;
//                        case Exchange.EnuSendAdmin.OldUnScheduled:
//                            body += $" has failed to be ordered and/or scheduled.";
//                            break;
//                        default:
//                            body += $" has not been ordered yet.";
//                            break;
//                    }

//                    if (!String.IsNullOrWhiteSpace(order.Message)) body += $" Message: {order.Message}";
//                    body += "<BR>";

//                    order.DateAdminLastNotified = DateTime.Now.Date;
//                    Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
//                }
//                email.SendAdmin(queue.Key, "Summary", body + "</BODY></HTML>");
//            }
//        }
//    }
//}
