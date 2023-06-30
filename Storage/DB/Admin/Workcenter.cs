namespace DB.Admin
{
    // WorkCenter Related -- Schedule
    public class Workcenter : Base
    {
        [PrimaryKey]
        
        public string WorkcenterID { get; set; }

        public string Name { get; set; }

        [Label("Sunday Hours")]
        [DisplayWidth(6)]
        public int SunWorkHours { get; set; }
        [Label("Monday Hours")]
        [DisplayWidth(6)]

        public int MonWorkHours { get; set; }
        [Label("Tuesday Hours")]
        [DisplayWidth(6)]

        public int TueWorkHours { get; set; }
        [Label("Wednesday Hours")]
        [DisplayWidth(6)]

        public int WedWorkHours { get; set; }
        [Label("Thursday Hours")]
        [DisplayWidth(6)]

        public int ThuWorkHours { get; set; }
        [Label("Friday Hours")]
        [DisplayWidth(6)]

        public int FriWorkHours { get; set; }
        [Label("Saturday Hours")]
        [DisplayWidth(6)]

        public int SatWorkHours { get; set; }
                     
    }
}