using DB.Vendor;
using System;
using System.Collections.Generic;

namespace Core
{
    public class InProgressOrder
    {

        public string? to { get; set; }
        public string? from { get; set; }

        public string? body { get; set; }

        public string? OrderedOrderID { get; set; }
        public string? OrderedPartName { get; set; }
        public int OrderedPartTotal { get; set; }
        public DateTime RequiredBy { get; set; }

        public IList<Order> orders { get; set; } = new List<Order>();
    }
}