using System;
using DB.Vendor;

namespace WorkCenterScheduling

{
    public class WorkCenterSchedulingQueueItem
    {
        public int BatchNumber = 0;
        public int intAmt = 0;
        public DateTime BeginSchedule;
        public DateTime EndSchedule;
        public WorkcenterPart WorkcenterPart;
    }


}
