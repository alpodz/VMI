using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using DB.Admin;
using System.Dynamic;
using Interfaces;

namespace DB.Vendor
{
    public class Part : Base, IBase
    {
        // Used for both Customer and Vendor
        [PartitionKey]
        [PrimaryKey]
        public string? id { get; set; }
        public string? Name { get; set; } // Used in placing Orders (VendorPartName) AND Receiving Orders (CustomerPartName)
        public string? Description { get; set; }
        [DisplayWidth(5)]
        public decimal Price { get; set; }
        [DisplayWidth(5)]
        public decimal Cost { get; set; }
        [DisplayWidth(5)]
        public int InStock { get; set; }

        #region "Derived Properties"
        [JsonIgnore]
        [DisplayWidth(5)]
        public int OrderedAmt { get; set; }
        [JsonIgnore]
        [DisplayWidth(5)]
        [Label("Avg Daily")]
        public int Daily { get; set; }
        [JsonIgnore]
        [DisplayWidth(5)]
        public int Last15Days { get; set; }

        [JsonIgnore]
        [DisplayWidth(8)]
        [Label("Days Inventory")]
        public int DaysLeft { get; set; }
       
        [JsonIgnore]
        [DisplayWidth(10)]
        [Label("Pull Quantity")]
        public int PullQuantity { get; private set; }
        [JsonIgnore]
        [DisplayWidth(8)]
        [Label("Pull Required By")]
        #endregion

        public System.DateTime? DateRequiredBy { get; private set; }
        public VendorPart? AssignedVendorPart;
        public bool Populated = false;

        void IBase.PopulateDerivedFields(IDBObject DBLocation, ref Dictionary<Type, Dictionary<String, IBase>> MainDB)
        {
            CalculateFields(DBLocation, ref MainDB);
        }

        public void CalculateFields(IDBObject  dBObject, ref Dictionary<Type, Dictionary<String, IBase>> MainDB)
        {
            if (MainDB == null) return;
            double NumberOfDaysForEstimation = 0;
            double MaxNumberOfDaysForEstimation = 15;
            
            OrderedAmt = 0;
            foreach (Order OrderedPart in MainDB[typeof(Order)].Values
                .Cast<Order>().Where(a => a.PartID == id && a.VendorOrder == true && a.DateOrdered.HasValue && !a.DateScheduled.HasValue && !a.DateCompleted.HasValue))
                OrderedAmt += OrderedPart.TotalAmountOrdered;

            AssignedVendorPart = MainDB[typeof(VendorPart)].Values.Cast<VendorPart>().FirstOrDefault(a => a.PartID == id && a.DesignatedInd == true);
            if (AssignedVendorPart == null) return;

            if (!Populated) 
            {
                foreach (Order objOrder in MainDB[typeof(Order)].Values
                    .Cast<Order>().Where(a => a.PartID == id && a.VendorOrder == false && a.DateScheduled.HasValue).OrderByDescending(a=> !a.DateScheduled.HasValue?DateTime.MinValue: a.DateScheduled))
                {
                    NumberOfDaysForEstimation++;
                    Last15Days += objOrder.TotalAmountOrdered;
                    foreach (Recipe objAssociatedPart in MainDB[typeof(Recipe)].Values.Cast<Recipe>().Where(a => a.CreatedPartID == id))
                    {
                        if (objAssociatedPart.PartID == null) continue;
                        Part objPart = (Part)MainDB[typeof(Part)][objAssociatedPart.PartID];
                        objPart.Last15Days += objOrder.TotalAmountOrdered * objAssociatedPart.NumberOfParts;
                    }
                    if (NumberOfDaysForEstimation >= MaxNumberOfDaysForEstimation) break;
                }
            }

            if (NumberOfDaysForEstimation == 0) return;
            Daily = (int)(Last15Days / NumberOfDaysForEstimation);
            if (Daily > 0 && InStock > 0)
                DaysLeft = InStock / Daily;

            PullQuantity = (int)Math.Ceiling((double)(Daily * AssignedVendorPart.LeadDays / AssignedVendorPart.PartsPerCase)) * AssignedVendorPart.PartsPerCase;
            DateRequiredBy = DateTime.Now.AddDays(DaysLeft - AssignedVendorPart.BufferDays).Date;
            Populated = true;
        }   
        
    }
}