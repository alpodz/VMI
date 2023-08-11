using DB.Admin;

namespace DB.Vendor
{
    public class WorkcenterPart : Base
    {
        [PrimaryKey]
        public string? id { get; set; }
        [ForeignKey(typeof(Workcenter))]
        [PartitionKey]
        public string? WorkcenterID { get; set; }
        [ForeignKey(typeof(Part))]
        public string? PartID { get; set; }
        public int PriorityLevel { get; set; }
        public decimal PartsPerMinute { get; set; }
        public decimal SetupTimeInMinutes { get; set; }       
    }
}