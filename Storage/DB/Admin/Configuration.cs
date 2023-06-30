namespace DB.Admin
{
    [TabIcon(@"~/images/Settings.png")]
    public class Configuration : Base
    {
        [PrimaryKey]
        public string ConfigurationID { get; set; }
        
        public string Name { get; set; }
        public string Value { get; set; }
    }
}