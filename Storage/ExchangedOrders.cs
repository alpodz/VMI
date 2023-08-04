using DB.Vendor;
using System;
using System.Collections.Generic;
using static Core.ExchangedOrders;

namespace Core
{
    public class ExchangedOrders
    {
        // Timer Initiated:
        // OutgoingMessageType.AskAdmin_SendOrder       VENDOR ORDER NEEDED:      - Ask Admin For Permission to Place Order  askadmin_sendorder -> sendadmin
        // IncomingMessageType.adminresponse_sendorder  Re: VENDOR ORDER NEEDED   - Get Response From Admin                  getemail -> adminresponse_sendorder -> sendorder
        // OutgoingMessageType.sendorder                ORDER                     - Send Vendor Order                        sendorder -> sendauto

        // IncomimgMessageType.getsendorder             ORDER                     - Receive Vendor Order / Customer Order   getemail -> getsendorder -> replyorder
        // OutgoingMessageType.replyorder               RE: ORDER                 - Send Order Response (Vendor/Customer)   replyorder -> sendauto

        // IncomingMessageType.getreplyorder            Re: ORDER                 - Receive Order Response                  getemail -> getreplyorder

        // Front-end:
        // OutgoingMessageType.sendorder                ORDER                     - Send Vendor Order                       sendorder -> sendauto

        public enum OutgoingMessageType
        {
            AskAdmin_SendOrder,             // VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
            sendorder,                      // ORDER                   -   Send Vendor Order
            replyorder,                     // Re: ORDER               -   Send Order Response
            RemindAdmin_UnOrdered,          // PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
            RemindAdmin_UnScheduled,        // PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
            RemindAdmin_UnCompleted,           // PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date
            Unknown
        }
        public static string SetSubject(OutgoingMessageType req)
        {
            switch (req)
            {
                case OutgoingMessageType.AskAdmin_SendOrder: 
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
            getsendorder,                   //  ORDER                  -   Receive Vendor Order / Customer Order
            getreplyorder,                  //  Re: ORDER              -   Receive Order Response
            adminresponse_sendorder,        //  Re: VENDOR ORDER NEEDED -   Get Response From Admin
            unknown
        }

        public static IncomingMessageType ParseSubject(string emailsubject)
        {
            if (emailsubject.StartsWith("Re: " + SetSubject(OutgoingMessageType.AskAdmin_SendOrder))) return IncomingMessageType.adminresponse_sendorder;
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