using System;

namespace Core.Core.API
{
    public class ExchangedOrders
    {
        public string to { get; set; }
        public string from { get; set; }

        public string OrderedOrderID { get; set; }
        public string OrderedPartName { get; set; }
        public int OrderedPartTotal { get; set; }
        public DateTime RequiredBy { get; set; }
    }
}