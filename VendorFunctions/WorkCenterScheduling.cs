using System;
using System.Collections.Generic;
using DB.Vendor;
using System.Linq;
using DB.Admin;

namespace WorkCenterScheduling

{
    public class WorkCenterScheduling
    {
        public static Dictionary<Type, Dictionary<string, IBase>> DBCollection;

        public static Order SchedulePartOnWorkCenter(ref Dictionary<Type, Dictionary<string, IBase>> db, int BatchNumber, int intAmt, DateTime BeginSchedule, DateTime EndSchedule, WorkcenterPart WorkcenterPart)
        {
            DBCollection = db;

            decimal TimeToMakeParts = intAmt / WorkcenterPart.PartsPerMinute;
            decimal TotalTimeToMakeParts = WorkcenterPart.SetupTimeInMinutes + TimeToMakeParts;

            // Cycle through each date, looking for a hole to enter the order in. This will require a) checking the workcenter exceptions for the
            // particular day, if no exception, use the day of weeks time as the maximum amount for that day.

            DateTime DateToSchedule = BeginSchedule;
            // Loop
            while (DateToSchedule <= EndSchedule)
            {
                decimal decHoursAvailable = GetScheduledHoursForWorkCenter(WorkcenterPart.WorkcenterID, DateToSchedule);

                // Test to see if Current Schedule can handle parts
                if (decHoursAvailable - TotalTimeToMakeParts / 60 >= 0)
                {
                    // Parts can be handled
                    var scheduledOrder = new Order()
                    {
                        //Shipment = BatchNumber.ToString() + " of ",
                        //ShipmentAmount = intAmt,
                        TotalAmountOrdered = intAmt,
                        DateScheduled = DateToSchedule,
                        PartID = WorkcenterPart.PartID,
                        WorkcenterID = WorkcenterPart.WorkcenterID,
                        DateOrdered = DateTime.Now.Date,
                        id = Guid.NewGuid().ToString()
                    };
                    return scheduledOrder;
                }

                DateToSchedule = DateToSchedule.Date.AddDays(1);
            }
            return null;
        }
        private static decimal GetScheduledHoursForWorkCenter(string WorkcenterID, DateTime DateToSchedule)
        {
            decimal decHoursAvailable = GetHoursAvailableForWorkCenter(WorkcenterID, DateToSchedule);
            // check currently scheduled parts for workcenter
            foreach (var ScheduledOrder in DBCollection[typeof(Order)].Values.Cast<Order>().Where(a => a.DateScheduled.HasValue && a.DateScheduled.Value.Date == DateToSchedule.Date && a.WorkcenterID == WorkcenterID))
            {
                WorkcenterPart ScheduledPart = DBCollection[typeof(WorkcenterPart)].Values.Cast<WorkcenterPart>().FirstOrDefault(a => a.WorkcenterID == ScheduledOrder.WorkcenterID && a.PartID == ScheduledOrder.PartID);
                decimal MinutesRequiredForPartsToBeCompleted = ScheduledOrder.TotalAmountOrdered / ScheduledPart.PartsPerMinute;
                decimal TotalMinutesRequiredForOrderToBeCompleted = ScheduledPart.SetupTimeInMinutes + MinutesRequiredForPartsToBeCompleted;
                decHoursAvailable -= TotalMinutesRequiredForOrderToBeCompleted / 60;
            }
            return decHoursAvailable;
        }

        private static decimal GetHoursAvailableForWorkCenter(string WorkcenterID, DateTime DateToSchedule)
        {
            var wk = (Workcenter)DBCollection[typeof(Workcenter)][WorkcenterID];
            if (wk == null) return 0;

            var exp = DBCollection[typeof(WorkcenterAvailability)].Values.Cast<WorkcenterAvailability>()
                .FirstOrDefault(a => a.WorkcenterID == WorkcenterID && a.ExceptionDate.Date == DateToSchedule.Date);
            if (exp != null) return exp.HoursAvailable;

            // Hours Available
            switch (DateToSchedule.DayOfWeek)
            {
                case DayOfWeek.Sunday: return wk.SunWorkHours;
                case DayOfWeek.Monday: return wk.MonWorkHours;
                case DayOfWeek.Tuesday: return wk.TueWorkHours;
                case DayOfWeek.Wednesday: return wk.WedWorkHours;
                case DayOfWeek.Thursday: return wk.ThuWorkHours;
                case DayOfWeek.Friday: return wk.FriWorkHours;
                case DayOfWeek.Saturday: return wk.SatWorkHours;
                default: return 0;
            }
        }
    }


}
