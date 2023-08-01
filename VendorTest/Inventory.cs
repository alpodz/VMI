using Core;
using DB;
using DB.Admin;
using DB.Vendor;
using Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;

namespace VendorTest
{
    public class Inventory : IInventory
    {
        private readonly IDBObject DBLocation;
        private Dictionary<Type, Dictionary<string, Base>> MainDBCollections;
        private static readonly object EmaiLLock = new object();
        private static IExchange email;
        private static Exchange _Exchange;

        public Inventory(IDBObject dBLocation, ref Dictionary<Type, Dictionary<string, Base>> mainDBCollections)
        {
            DBLocation = dBLocation;
            MainDBCollections = mainDBCollections;
        }

        public void ExecuteMaint(IExchange _email)
        {
            _Exchange = _email;
            CheckForNeedOfVendorOrder();

            if (email.Client == null) return;
            // query email server and do what you need to do:
            foreach (var msg in email.Client.Search())
                ExecuteWorkAgainstMailMessage(msg);

            EmailAdminToDoStuff();
        }

        public void ExecuteWorkAgainstMailMessage(uint mailnum)
        {
            lock (EmaiLLock)
            {
                // possiblely already deleted // we'll catch the exception returning and just 'eat it'
                using (MailMessage mail = email.Client.GetMessage(mailnum))
                {
                    // apparently deleted messages are loaded but have no sender
                    if (mail == null || mail.From == null) return;
                    CheckEmailForVariousResponses(mail);
                }
                email.Client.DeleteMessage(mailnum);
            }
        }

        private void CheckEmailForVariousResponses(MailMessage mail)
        {            
            // RequestVendorOrderResponse   In  -   Man     Re: VENDOR ORDER NEEDED -   Get Response From Admin              
            // SendVendorOrder              Out -   Auto    ORDER                   -   Send Vendor Order
            if (CheckForVendorOrderResponse(email.RetrieveMail(mail, EnuReceiveAdmin.RequestVendorOrderResponse.ToString()))) return;
            // ReceiveCustomerOrder         In  -   Auto    ORDER                   -   Receive Vendor Order / Customer Order
            // SendCustomerOrderResponse    Out -   Auto    Re: ORDER               -   Send Order Response
            if (GetOrders(email, email.RetrieveMail(mail, EnuRecieveAuto.RecieveCustomerOrder.ToString()))) return;
            // ReceiveOrderResponse         In  -   Auto    Re: ORDER               -   Receive Order Response
            if (GetResponsesToOrders(email.RetrieveMail(mail, EnuRecieveAuto.RecieveOrderResponse.ToString()))) return;
        }

        private void EmailAdminToDoStuff()
        {
            // only process if these items are set
            if (MainDBCollections == null) return;
            var multipleorders = new Dictionary<EnuSendAdmin, List<Order>>();

            // RequestVendorOrder           Out -   Man     VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
            // OldUnOrdered                 Out -   Man     PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
            // OldUnScheduled               Out -   Man     PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
            // -- SendAdminCustomerFail     Out -   Man     ORDER FAILURE           -   Non-Vendor Order - Order Date, Not Scheduled (Message)
            // -- SendAdminVendorFail       Out -   Man     ORDER FAILURE           -   Vendor Order - Order Date, Not Scheduled (Message)
            // OldUnCompleted               Out -   Man     PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date

            var Orders = MainDBCollections[typeof(Order)].Values.Cast<Order>();
            foreach (var objOrd in Orders)
            {
                if (!objOrd.DateAdminLastNotified.HasValue || objOrd.DateAdminLastNotified.HasValue && objOrd.DateAdminLastNotified < DateTime.Now.Date)
                {
                    if (!objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
                    {
                        // OldUnOrdered -- Queue Up
                        if (!multipleorders.ContainsKey(EnuSendAdmin.OldUnOrdered)) multipleorders.Add(EnuSendAdmin.OldUnOrdered, new List<Order>());
                        multipleorders[EnuSendAdmin.OldUnOrdered].Add(objOrd);
                    }
                    else if (objOrd.DateOrdered.HasValue && !objOrd.DateScheduled.HasValue && !objOrd.DateCompleted.HasValue)
                    {
                        // OldUnScheduled -- Queue Up
                        if (!multipleorders.ContainsKey(EnuSendAdmin.OldUnScheduled)) multipleorders.Add(EnuSendAdmin.OldUnScheduled, new List<Order>());
                        multipleorders[EnuSendAdmin.OldUnScheduled].Add(objOrd);
                    }
                    else if (objOrd.DateOrdered.HasValue && objOrd.DateScheduled.HasValue && objOrd.DateScheduled < DateTime.Now.Date && !objOrd.DateCompleted.HasValue)
                    {
                        // OldUnCompleted -- Queue Up
                        if (!multipleorders.ContainsKey(EnuSendAdmin.OldUnCompleted)) multipleorders.Add(EnuSendAdmin.OldUnCompleted, new List<Order>());
                        multipleorders[EnuSendAdmin.OldUnCompleted].Add(objOrd);
                    }
                }
            }

            foreach (var queue in multipleorders)
            {
                var body = "<HTML><BODY>";
                foreach (var order in queue.Value)
                {
                    var typeofOrder = "Customer";
                    if (order.VendorOrder) typeofOrder = "Vendor";
                    body += $"{typeofOrder} Order: {order.id} ";

                    switch (queue.Key)
                    {
                        case EnuSendAdmin.OldUnCompleted:
                            body += $" was scheduled to arrive or be completed for {order.DateScheduled} but has not been marked completed.";
                            break;
                        case EnuSendAdmin.OldUnScheduled:
                            body += $" has failed to be ordered and/or scheduled.";
                            break;
                        default:
                            body += $" has not been ordered yet.";
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(order.Message)) body += $" Message: {order.Message}";
                    body += "<BR>";

                    order.DateAdminLastNotified = DateTime.Now.Date;
                    Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
                }
                email.SendAdmin(queue.Key, "Summary", body + "</BODY></HTML>");
            }
        }

        private void CheckForNeedOfVendorOrder()
        {
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
                body += $"<BR><BR>Reply to this Email to Proceed. (Subject must be: {email.SetSubject(EnuReceiveAdmin.RequestVendorOrderResponse.ToString())}).";

                email.SendAdmin(EnuSendAdmin.RequestVendorOrder, objOrder.id, body);
            }

        }

        private bool CheckForVendorOrderResponse(string OrderedOrderID)
        {
            if (OrderedOrderID == null) return false;
            // let's make sure it's a real order
            Order ord = null;
            if (MainDBCollections[typeof(Order)].ContainsKey(OrderedOrderID))
                ord = (Order)MainDBCollections[typeof(Order)][OrderedOrderID];
            if (ord != null)
            {
                // place order // update to address to vendor
                ord.MainDBCollections = MainDBCollections;
                ord.DateOrdered = DateTime.Now.Date;
                Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
            }
            return true;
        }

        private bool GetResponsesToOrders(ExchangedOrders response)
        {
            if (response == null) return false;
            // update the order placed
            Order orig = (Order)MainDBCollections[typeof(Order)][response.OrderedOrderID];
            if (orig != null)
            {
                if (response.orders.Count == 1 && response.orders[0].TotalAmountOrdered == 0) // negative response?
                {
                    orig.Message = response.orders[0].Message;
                    orig.DateOrdered = null;
                }
                // success
                else
                {
                    // we have an existing order and possiblely other shipments, we will need to create additional shipments on customer side
                    // single order or first order
                    if (response.orders.Count >= 1)
                    {
                        var order = response.orders[0];
                        orig.DateScheduled = order.DateScheduled;
                        //orig.ShipmentAmount = order.ShipmentAmount;
                        //orig.Shipment = order.Shipment;
                        orig.TotalAmountOrdered = order.TotalAmountOrdered;
                        orig.Message = order.Message;
                        orig.CustomerOrderID = order.id;

                        response.orders.Remove(order);
                    }

                    // loop through remaining orders and create new orders
                    foreach (var order in response.orders)
                    {
                        order.CustomerID = orig.CustomerID;
                        var guid = Guid.NewGuid().ToString();
                        order.id = guid;
                        order.PartID = orig.PartID;
                        order.CustomerOrderID = order.id;
                        order.WorkcenterID = "0";
                        MainDBCollections[typeof(Order)].Add(guid, order);
                    }
                }

                Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
            }
            return true;
        }

        private bool GetOrders(Exchange mail, ExchangedOrders request)
        {
            if (request == null) return false;
            // make/schedule orders
            request.CheckFulfillment(ref MainDBCollections);

            // save the incoming order with a new order id, however save the customer order id
            foreach (var order in request.orders)
            {
                //order.Shipment = " of " + request.orders.Count;
                order.TotalAmountOrdered = request.OrderedPartTotal;
                order.CustomerOrderID = order.id;
                order.id = Guid.NewGuid().ToString();
                MainDBCollections[typeof(Order)].Add(order.id, order);
                Base.SaveCollection(DBLocation, typeof(Order), MainDBCollections[typeof(Order)]);
            }
            request.body = "Order Response";
            mail.SendAuto(EnuSendAuto.SendCustomerOrderResponse, request);
            return true;
        }

    }
}