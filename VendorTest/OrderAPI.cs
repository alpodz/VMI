using Core;
using DB.Admin;
using DB.Vendor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VendorTest
{

    public class OrderAPI
    {
        public static Dictionary<Type, Dictionary<string, Base>> MainDBCollections;

        public static void SendOrder(Order Order)
        {
            // only process if these items are set
            if (MainDBCollections == null || !Order.DateOrdered.HasValue || string.IsNullOrEmpty(Order.PartID) || string.IsNullOrEmpty(Order.CustomerID) || string.IsNullOrEmpty(Order.id)) return;

            if (Order.VendorOrder)
            {
                var part = (Part)MainDBCollections[typeof(Part)][Order.PartID];
                var PullVendor = (Customer)MainDBCollections[typeof(Customer)][Order.CustomerID];
                if (PullVendor == null || part == null) return;

                if (!part.Populated) part.PopulateDerivedFields(Order.DBLocation, ref MainDBCollections);
                if (part.AssignedVendorPart == null) return;

                Order.VendorPartName = part.AssignedVendorPart.VendorPartName;

                ExchangedOrders exchangedOrders = new ExchangedOrders()
                {
                    OrderedOrderID = Order.id,
                    OrderedPartName = Order.VendorPartName,
                    OrderedPartTotal = Order.TotalAmountOrdered,
                    to = PullVendor.EmailAddress,
                    body = "Order Request"
                };
                // we're going to push the required by date because perhaps it's 'too late', we'll make it the current date
                var requiredby = part.DateRequiredBy.Value;
                if (requiredby < DateTime.Now.Date) requiredby = DateTime.Now.Date.AddDays(part.AssignedVendorPart.LeadDays);

                exchangedOrders.RequiredBy = requiredby;
                if (Order.DateScheduled.HasValue) exchangedOrders.RequiredBy = Order.DateScheduled.Value;

                var mail = new Exchange(ref MainDBCollections, out var success);
                if (!success) return;
                mail.SendAuto(EnuSendAuto.SendVendorOrder, exchangedOrders);
            }
        }

        public static void AdjustInventory(Order Order)
        {
            if (Order.DBLocation == null || MainDBCollections == null) return;

            if (Order.VendorOrder)
            {
                var objPart = (Part)MainDBCollections[typeof(Part)][Order.PartID];
                if (objPart == null) return;
                objPart.InStock += Order.TotalAmountOrdered;
                Order.Message = "Vendor - Shipment Arrived.";
            }
            else
            {
                foreach (var objAssocParts in MainDBCollections[typeof(Recipe)].Values.Cast<Recipe>().Where(a => a.CreatedPartID == Order.PartID))
                {
                    var objPart = (Part)MainDBCollections[typeof(Part)][objAssocParts.PartID];
                    if (objPart == null) return;
                    objPart.InStock -= objAssocParts.NumberOfParts;
                }
                Order.Message = "Customer - Shipment Ready.";
            }

            Base.SaveCollection(Order.DBLocation, typeof(Part), MainDBCollections[typeof(Part)]);
        }

        //private void AddMessage(string Message)
        //{
        //    if (Order.DBLocation == null || MainDBCollections == null) return;

        //    var objMessage = new Message
        //    {
        //        id = Guid.NewGuid().ToString(),
        //        MessageText = Message,
        //        MessageDate = DateTime.Now,
        //        OrderID = Order.id
        //    };
        //    MainDBCollections[typeof(Message)].Add(objMessage.id, objMessage);
        //    Base.SaveCollection(Order.DBLocation, typeof(Message), MainDBCollections[typeof(Message)]);
        //}

    }
}
