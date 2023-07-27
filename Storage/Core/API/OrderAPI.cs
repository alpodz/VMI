using DB.Admin;
using DB.Vendor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Core.API
{

    public class OrderAPI
    {
        public static Dictionary<Type, Dictionary<string, IBase>> MainDBCollections;

        public static void SendOrder(Order order)
        {
            
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
