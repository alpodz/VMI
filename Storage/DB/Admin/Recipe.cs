using DB.Vendor;

namespace DB.Admin
{
    public class Recipe: Base
    {
        [PrimaryKey]
        public string id { get; set; }
        [PartitionKey]
        [ForeignKey(typeof(Part))]
        public string CreatedPartID { get; set; }

        [ForeignKey(typeof(Part))]
        public string PartID { get; set; }
        public int NumberOfParts { get; set; }
    }
}
