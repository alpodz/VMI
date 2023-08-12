namespace DB.Admin
{
    [TabIcon(@"~/images/Settings.png")]
    public class Configuration : Base
    {
        [PartitionKey]
        [PrimaryKey]
        public string? id { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}