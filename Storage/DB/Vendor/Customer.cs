namespace DB.Vendor
{
    public class Customer : Base, IBase
    {
        [PartitionKey]
        [PrimaryKey]
        public string id { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string EmailAddress { get; set; }


    }
}