using DB.Vendor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class ExchangedOrders
    {
        public string to { get; set; }
        public string from { get; set; }
        public string subject { get; set; }
        public string body { get; set; }


        public string OrderedOrderID { get; set; }
        public string OrderedPartName { get; set; }
        public int OrderedPartTotal { get; set; }
        public DateTime RequiredBy { get; set; }

        public IList<Order> orders { get; set; } = new List<Order>();

        private Dictionary<Type, Dictionary<String, Base>> MainDBCollections;

        public bool CheckFulfillment(ref Dictionary<Type, Dictionary<String, Base>> DBCollections)
        {
            MainDBCollections = DBCollections;
            orders.Clear();
            // create the incoming order 
            var incomingOrder = new Order()
                { id = OrderedOrderID, VendorOrder = false, TotalAmountOrdered = OrderedPartTotal, DateOrdered = DateTime.Now.Date, Message = String.Empty, RequiredBy = this.RequiredBy };
                try
                    {
                if (!CheckUserName(from, out var Customer))
                    incomingOrder.Message += $"Unregistered User for email: {from} |";
                else
                    incomingOrder.CustomerID = Customer.id;

                Part PartID = MainDBCollections[typeof(Part)].Values.Cast<Part>().FirstOrDefault(a => a.Name == OrderedPartName);
                if (PartID == null)
                    incomingOrder.Message += $"Part {OrderedPartName} does not exist. |";
                else
                    incomingOrder.PartID = PartID.id;

                var workcenters = MainDBCollections[typeof(WorkcenterPart)].Values.Cast<WorkcenterPart>().Where(a => a.PartID == PartID.id).OrderBy(a => a.PriorityLevel).ToList();
                if (workcenters.Count == 0)
                    incomingOrder.Message += $"No Workcenters Setup for Part {OrderedPartName} |";

                if (!incomingOrder.RequiredBy.HasValue || incomingOrder.RequiredBy.Value.Date < DateTime.Now.Date)
                    incomingOrder.Message += $"Required By Date is invalid {RequiredBy} |";

                if (incomingOrder.Message != String.Empty)
                {
                    orders.Add(incomingOrder);
                    return false;
                }

                var PartsLeftToShip = OrderedPartTotal;
                while (orders.Count <= 3 && PartsLeftToShip > 0)
                {
                    var ShipmentNumber = orders.Count + 1;
                    var PartsInShipment = (int)Math.Ceiling(OrderedPartTotal / (double)ShipmentNumber);
                    int DaysUntilFullShipmentIsRequired = (int)Math.Ceiling(RequiredBy.Date.Subtract(DateTime.Now.Date).TotalDays);

                    var DaysAllowedForProduction = DaysUntilFullShipmentIsRequired / ShipmentNumber;

                    var BeginDateOfProduction = DateTime.Now.Date.AddDays(DaysAllowedForProduction * (ShipmentNumber - 1));
                    var EndDateOfProduction = BeginDateOfProduction.Date.AddDays(DaysAllowedForProduction);

                    foreach (var rwWorkCenter in workcenters)
                    {
                        var outgoingOrder = rwWorkCenter.SchedulePartOnWorkCenter(ref DBCollections, ShipmentNumber, PartsInShipment, BeginDateOfProduction, EndDateOfProduction);
                        if (outgoingOrder != null)
                        {
                            PartsLeftToShip -= PartsInShipment;
                            outgoingOrder.CustomerID = Customer.id;
                            outgoingOrder.RequiredBy = RequiredBy;
                            orders.Add(outgoingOrder);
                        }
                        if (PartsLeftToShip <= 0) return true;
                    }

                    if (PartsLeftToShip > 0)
                    {
                        orders.Clear();
                        incomingOrder.Message += $"Unable to schedule the Part for delivery for {EndDateOfProduction}";
                        orders.Add(incomingOrder);
                        return false;
                    }
                }

            }            
            catch
            {

            }          
            orders.Clear();
            incomingOrder.Message += "Unable to Schedule Part";
            orders.Add(incomingOrder);
            return false;
        }

        public ExchangedOrders()
        {
        }

        private bool CheckUserName(string strUserName, out Customer customer)
        {
            customer = MainDBCollections[typeof(Customer)].Values.Cast<Customer>()
               .FirstOrDefault(a => a.EmailAddress == strUserName);
            return customer != null;
        }
    }
}