using DB.Vendor;
using System;
using System.Collections.Generic;
using System.Text;

namespace DB.Vendor
{
    class Message: Base
    {
        #region "Properties"

        [PrimaryKey]
        public string id { get; set; }      
        public string MessageText { get; set; }
        public DateTime MessageDate { get; set; }

        [PartitionKey]
        [ForeignKey(typeof(Order))]
        public string OrderID { get; set; }

        #endregion
    }
}
