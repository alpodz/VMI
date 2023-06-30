namespace DB.Vendor
{
    public class Customer : Base, IBase
    {
        [PrimaryKey]


        public string CustomerID { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string EmailAddress { get; set; }


    }
}