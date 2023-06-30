using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DB.Vendor
{
    // This makes a Customer a Vendor of a Part
    public class VendorPart : Base, IBase
    {
        [PrimaryKey]
        
        public string VendorPartID { get; set; }
        [ForeignKey(typeof(Part))]
        [Label("Part")]
        public string PartID { get; set; }
        [ForeignKey(typeof(Customer))]
        [Label("Vendor")]
        public string CustomerID { get; set; }
        [Label("Vendor Part Name")]
        public string VendorPartName { get; set; }
        [DisplayWidth(1)]
        [Label("Designated Vendor")]
        public bool DesignatedInd { get; set; }
        [DisplayWidth(10)]
        [Label("Lead in Days")]
        public int LeadDays { get; set; }
        [DisplayWidth(10)]
        [Label("Buffer in Days")]
        public int BufferDays { get; set; }
        [DisplayWidth(10)]
        [Label("Parts Per Case")]
        public int PartsPerCase { get; set; }
        [JsonIgnore]
        [DisplayWidth(10)]
        [Label("Pull Quantity")]
        public int PullQuantity { get; private set; }
        [JsonIgnore]
        [DisplayWidth(10)]
        [Label("Parts In Stock")]
        public int InStock { get; private set; }
        [JsonIgnore]
        [DisplayWidth(8)]
        [Label("Pull Required By")]
        public System.DateTime? DateRequiredBy { get; private set; }

        void IBase.PopulateDerivedFields(String DBLoc, ref Dictionary<Type, Dictionary<String,Base> >MainDB)
        {
            if (MainDB == null || PartID == null) return;
            if (!MainDB[typeof(Part)].ContainsKey(PartID)) return;
            Part objpart = (Part)MainDB[typeof(Part)][PartID];
            objpart.PopulateDerivedFields(DBLoc, ref MainDB);
            InStock = objpart.InStock;
            PullQuantity = objpart.PullQuantity;
            DateRequiredBy = objpart.DateRequiredBy;
        }
    }
}
