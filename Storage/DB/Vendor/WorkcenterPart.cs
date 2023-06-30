// WorkCenter Related -- Part Allocation

using DB.Admin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DB.Vendor
{
    public class WorkcenterPart : Base
    {
        [PrimaryKey]

        public string WorkcenterPartID { get; set; }
        [ForeignKey(typeof(Workcenter))]

        public string WorkcenterID { get; set; }

        [ForeignKey(typeof(Part))]
        public string PartID { get; set; }

        public int PriorityLevel { get; set; }
        public decimal PartsPerMinute { get; set; }
        public decimal SetupTimeInMinutes { get; set; }

        public Order SchedulePartOnWorkCenter(ref Dictionary<Type, Dictionary<String, Base>> DBCollection, int BatchNumber, int intAmt, DateTime BeginSchedule, DateTime EndSchedule)
        {
            MainDBCollections = DBCollection;
            decimal TimeToMakeParts = intAmt / PartsPerMinute;
            decimal TotalTimeToMakeParts = SetupTimeInMinutes + TimeToMakeParts;

            // Cycle through each date, looking for a hole to enter the order in. This will require a) checking the workcenter exceptions for the
            // particular day, if no exception, use the day of weeks time as the maximum amount for that day.

            DateTime DateToSchedule = BeginSchedule;
            // Loop
            while (DateToSchedule <= EndSchedule)
            {
                decimal decHoursAvailable = GetScheduledHoursForWorkCenter(WorkcenterID, DateToSchedule);

                // Test to see if Current Schedule can handle parts
                if (decHoursAvailable - (TotalTimeToMakeParts / 60) >= 0)
                {
                    // Parts can be handled
                    var scheduledOrder = new Order()
                    {
                        //Shipment = BatchNumber.ToString() + " of ",
                        //ShipmentAmount = intAmt,
                        TotalAmountOrdered = intAmt,
                        DateScheduled = DateToSchedule,
                        PartID = this.PartID,
                        WorkcenterID = WorkcenterID,
                        DateOrdered = DateTime.Now.Date,
                        OrderID = Guid.NewGuid().ToString()
                    };
                    return scheduledOrder;
                }

                DateToSchedule = DateToSchedule.Date.AddDays(1);
            }
            return null;
        }
        private decimal GetScheduledHoursForWorkCenter(string WorkcenterID, DateTime DateToSchedule)
        {
            decimal decHoursAvailable = GetHoursAvailableForWorkCenter(WorkcenterID, DateToSchedule);
            // check currently scheduled parts for workcenter
            foreach (var ScheduledOrder in MainDBCollections[typeof(Order)].Values.Cast<Order>().Where(a => a.DateScheduled.HasValue && a.DateScheduled.Value.Date == DateToSchedule.Date && a.WorkcenterID == WorkcenterID))
            {
                WorkcenterPart ScheduledPart = MainDBCollections[typeof(WorkcenterPart)].Values.Cast<WorkcenterPart>().FirstOrDefault(a => a.WorkcenterID == ScheduledOrder.WorkcenterID && a.PartID == ScheduledOrder.PartID);
                decimal MinutesRequiredForPartsToBeCompleted = ScheduledOrder.TotalAmountOrdered / ScheduledPart.PartsPerMinute;
                decimal TotalMinutesRequiredForOrderToBeCompleted = ScheduledPart.SetupTimeInMinutes + MinutesRequiredForPartsToBeCompleted;
                decHoursAvailable -= (TotalMinutesRequiredForOrderToBeCompleted / 60);
            }
            return decHoursAvailable;
        }

        private decimal GetHoursAvailableForWorkCenter(string WorkcenterID, DateTime DateToSchedule)
        {
            var wk = (Workcenter) MainDBCollections[typeof(Workcenter)][WorkcenterID];
            if (wk == null)                 return 0;

            var exp = (WorkcenterAvailability)MainDBCollections[typeof(WorkcenterAvailability)].Values.Cast<WorkcenterAvailability>()
                .FirstOrDefault(a => a.WorkcenterID == WorkcenterID && a.ExceptionDate.Date == DateToSchedule.Date);
            if (exp != null)                return exp.HoursAvailable;
            
            // Hours Available
            switch (DateToSchedule.DayOfWeek)
            {
                case DayOfWeek.Sunday:      return wk.SunWorkHours;
                case DayOfWeek.Monday:      return wk.MonWorkHours;
                case DayOfWeek.Tuesday:     return wk.TueWorkHours;
                case DayOfWeek.Wednesday:   return wk.WedWorkHours;
                case DayOfWeek.Thursday:    return wk.ThuWorkHours;
                case DayOfWeek.Friday:      return wk.FriWorkHours;
                case DayOfWeek.Saturday:    return wk.SatWorkHours;
                default:                    return 0;
            }
        }
    }
}