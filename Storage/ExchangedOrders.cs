using DB.Vendor;
using System;
using System.Collections.Generic;

namespace Core
{
    public class ExchangedOrders
    {
        // Timer causes below:
        // OutgoingMessageType.AskAdmin_SendOrder       VENDOR ORDER NEEDED:      - Ask Admin For Permission to Place Order  askadmin_sendorder -> sendadmin
        // IncomingMessageType.adminresponse_sendorder  Re: VENDOR ORDER NEEDED   - Get Response From Admin                  getemail -> adminresponse_sendorder -> sendorder
        // OutgoingMessageType.sendorder                ORDER                     - Send Vendor Order                        sendorder -> sendauto

        // Flow is from above (and also from external orders)
        // IncomimgMessageType.getsendorder             ORDER                     - Receive Vendor Order / Customer Order   getemail -> getsendorder -> replyorder
        // OutgoingMessageType.replyorder               RE: ORDER                 - Send Order Response (Vendor/Customer)   replyorder -> sendauto

        // IncomingMessageType.getreplyorder            Re: ORDER                 - Receive Order Response                  getemail -> getreplyorder

        // Front-end:
        // OutgoingMessageType.sendorder                ORDER                     - Send Vendor Order                       sendorder -> sendauto

        public enum OutgoingMessageType
        {
            askadmin_sendorder,             // VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
            sendorder,                      // ORDER                   -   Send Vendor Order
            replyorder,                     // Re: ORDER               -   Send Order Response
            RemindAdmin_UnOrdered,          // PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
            RemindAdmin_UnScheduled,        // PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
            RemindAdmin_UnCompleted,        // PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date
            Unknown
        }
        public static string SetSubject(OutgoingMessageType req)
        {
            switch (req)
            {
                case OutgoingMessageType.askadmin_sendorder: 
                    return "VENDOR ORDER NEEDED:";          //  Ask Admin For Permission to Place Order
                case OutgoingMessageType.sendorder: 
                    return "ORDER:";                        //  Send Vendor Order
                case OutgoingMessageType.replyorder: 
                    return "Re: ORDER:";                    //  Send Order Response
                case OutgoingMessageType.RemindAdmin_UnOrdered: 
                    return "PENDING UNORDERED:";            //  Pending Unordered Orders - No Order Date
                case OutgoingMessageType.RemindAdmin_UnScheduled: 
                    return "PENDING UNSCHEDULED:";          //  Pending Unscheduled Orders - No Scheduled Date
                case OutgoingMessageType.RemindAdmin_UnCompleted: 
                    return "PENDING UNCOMPLETED:";          //  Pending Uncompleted Orders - No Completed Date
                default: return String.Empty;
            }
        }

        public enum IncomingMessageType
        {
            getaskadmin_sendorder,          //  Re: VENDOR ORDER NEEDED -   Get Response From Admin
            getsendorder,                   //  ORDER                  -   Receive Vendor Order / Customer Order
            getreplyorder,                  //  Re: ORDER              -   Receive Order Response
            unknown
        }

        public static IncomingMessageType ParseSubject(string emailsubject)
        {
            if (emailsubject.StartsWith("Re: " + SetSubject(OutgoingMessageType.askadmin_sendorder))) return IncomingMessageType.getaskadmin_sendorder;
            if (emailsubject.StartsWith(SetSubject(OutgoingMessageType.sendorder))) return IncomingMessageType.getsendorder;
            if (emailsubject.StartsWith(SetSubject(OutgoingMessageType.replyorder))) return IncomingMessageType.getreplyorder;
            return IncomingMessageType.unknown;
        }

        public string to { get; set; }
        public string from { get; set; }

        public string OrderedOrderID { get; set; }
        public string OrderedPartName { get; set; }
        public int OrderedPartTotal { get; set; }
        public DateTime RequiredBy { get; set; }

        public IList<Order> orders { get; set; } = new List<Order>();

    }
}