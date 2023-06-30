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
        public string MessageID { get; set; }      
        public string MessageText { get; set; }
        public DateTime MessageDate { get; set; }

        [ForeignKey(typeof(Order))]
        public string OrderID { get; set; }

        #endregion
    }
}
