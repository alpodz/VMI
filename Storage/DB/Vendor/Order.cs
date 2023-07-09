using Core;
using DB.Vendor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DB.Admin;
using System.Collections.Concurrent;

namespace DB.Vendor
{
    // used for both sides (vendor order id is filled in for 'customer')
    public class Order : Base
    {
        #region "Properties"
        [PartitionKey]
        [PrimaryKey]
        public string id { get; set; }
        [Label("Vendor Order")]
        [DisplayWidth(6)]
        public bool VendorOrder { get; set; }
        [ForeignKey(typeof(Customer))]
        [Label("Customer")]
        public string CustomerID { get; set; }
        [ForeignKey(typeof(Part))]
        [Label("Part")]
        public string PartID { get; set; }

        [ForeignKey(typeof(Workcenter))]
        [Label("WorkCenter")]
        public string WorkcenterID { get; set; }
        [Label("Total Ordered")]
        [DisplayWidth(5)]
        public int TotalAmountOrdered { get; set; }
        [Label("Date Ordered")]
        [DisplayWidth(6)]
        public DateTime? DateOrdered { 
            get => dateOrdered; 
            set {
                if (dateOrdered.GetValueOrDefault(DateTime.MinValue) == value.GetValueOrDefault(DateTime.MinValue)) return;
                dateOrdered = value;                
                if (value != null) SendOrder(); 
            } 
        }
        [Label("Date Scheduled")]
        [DisplayWidth(6)]
        public DateTime? DateScheduled { get; set; }
        [Label("Date Completed")]
        [DisplayWidth(6)]
        public DateTime? DateCompleted { 
            get => dateCompleted; 
            set {
                if (dateCompleted.GetValueOrDefault(DateTime.MinValue) == value.GetValueOrDefault(DateTime.MinValue)) return;               
                dateCompleted = value;                
                if (value != null) AdjustInventory();
            } 
        }
        //[ReadOnly]
        public DateTime? RequiredBy;// { get; set; }

        [ReadOnly]
        [DisplayWidth(0)]
        public DateTime? DateAdminLastNotified { get; set; }

        [ReadOnly]
        public string Message { get; set; }
        //public string Message { 
        //    get
        //    {
        //        return MainDBCollections[typeof(Message)].Values.Cast<Message>().OrderBy(a => a.MessageDate).FirstOrDefault().MessageText;
        //    }
        //    set
        //    {
        //        AddMessage(value);
        //    }
        //}

        //[ReadOnly]
        public string VendorPartName; // { get; internal set; }

        //[ReadOnly]
        //[Label("Customer Order ID")]
        public string CustomerOrderID; // { get; set; }

        private DateTime? dateOrdered;
        private DateTime? dateCompleted;

        #endregion

        #region "Methods"

        private void SendOrder()
        {
            // only process if these items are set
            if (MainDBCollections == null || !DateOrdered.HasValue || String.IsNullOrEmpty(PartID) || String.IsNullOrEmpty(CustomerID) || String.IsNullOrEmpty(id)) return;

            if (VendorOrder)
            {
                var part = (Part) MainDBCollections[typeof(Part)][PartID];
                var PullVendor = (Customer) MainDBCollections[typeof(Customer)][CustomerID];
                if (PullVendor == null || part == null) return;

                if (!part.Populated) part.PopulateDerivedFields(DBLocation, ref MainDBCollections);
                if (part.AssignedVendorPart == null) return;

                this.VendorPartName = part.AssignedVendorPart.VendorPartName;

                ExchangedOrders exchangedOrders = new ExchangedOrders()
                {
                    OrderedOrderID = id,
                    OrderedPartName = VendorPartName,
                    OrderedPartTotal = TotalAmountOrdered,
                    to = PullVendor.EmailAddress,
                    body = "Order Request"
                };
                // we're going to push the required by date because perhaps it's 'too late', we'll make it the current date
                var requiredby = part.DateRequiredBy.Value;
                if (requiredby < DateTime.Now.Date) requiredby = DateTime.Now.Date.AddDays(part.AssignedVendorPart.LeadDays);

                exchangedOrders.RequiredBy = requiredby;
                if (DateScheduled.HasValue) exchangedOrders.RequiredBy = DateScheduled.Value;

                var mail = new Exchange(ref MainDBCollections);
                mail.SendAuto(Exchange.EnuSendAuto.SendVendorOrder, exchangedOrders);
            }
        }

        private void AdjustInventory()
        {
            if (DBLocation == null || MainDBCollections == null) return;

            if (VendorOrder)
            {
                var objPart = (Part) MainDBCollections[typeof(Part)][PartID];
                if (objPart == null) return;
                objPart.InStock += TotalAmountOrdered;
                Message = "Vendor - Shipment Arrived.";
            }
            else
            {
                foreach (var objAssocParts in MainDBCollections[typeof(Recipe)].Values.Cast<Recipe>().Where(a => a.CreatedPartID == PartID))
                {
                    var objPart = (Part) MainDBCollections[typeof(Part)][objAssocParts.PartID];
                    if (objPart == null) return;
                    objPart.InStock -= objAssocParts.NumberOfParts;
                }
                Message = "Customer - Shipment Ready.";
            }

            Base.SaveCollection(DBLocation, typeof(Part), MainDBCollections[typeof(Part)]);
        }

        private void AddMessage(string Message)
        {
            if (DBLocation == null || MainDBCollections == null) return;

            var objMessage = new Message
            {
                id = Guid.NewGuid().ToString(),
                MessageText = Message,
                MessageDate = DateTime.Now,
                OrderID = id
            };
            MainDBCollections[typeof(Message)].Add(objMessage.id, objMessage);
            Base.SaveCollection(DBLocation, typeof(Message), MainDBCollections[typeof(Message)]);
        }
        #endregion
    }
}
