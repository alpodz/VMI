using Core;
using DB.Vendor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DB.Admin;
using System.Collections.Concurrent;
using Core.Core.API;

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
                if (value != null) OrderAPI.SendOrder(this); 
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
                if (value != null) OrderAPI.AdjustInventory(this);
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
    }
}
