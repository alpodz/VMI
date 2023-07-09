using System;
namespace DB.Admin
{
    // WorkCenter Related -- Schedule Exception
    public class WorkcenterAvailability : Base
    {
        [PrimaryKey]
        public string id { get; set; }

        [PartitionKey]
        [ForeignKey(typeof(Workcenter))]
        public string WorkcenterID { get; set; }

        public DateTime ExceptionDate { get; set; }
        public decimal HoursAvailable { get; set; }
    }
}