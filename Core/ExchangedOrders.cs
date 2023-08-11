using System;

namespace Core
{
    public class ExchangedOrders
    {
        // TTAskAdmin_SendOrder -> [QUEUE "sendadmin"] (sends email)
        // OutgoingMessageType.AskAdmin_SendOrder       VENDOR ORDER NEEDED:      - Ask Admin For Permission to Place Order 

        // Incoming Email (from admin):
        // [QUEUE "getemail"] TTGetEmail -> [QUEUE "getaskadmin_sendorder"] [string] ->

        // IncomingMessageType.adminresponse_sendorder  Re: VENDOR ORDER NEEDED   - Get Response From Admin                  

        // QTGetAskAdmin_SendOrder [sets DateOrdered] -> [QUEUE "sendorder"]  QTSendOrder (Order) ->

        // OutgoingMessageType.sendorder                ORDER                     - Send Vendor Order                       

        // a) (Manual) [QUEUE "sendemail"] (text) -> To Vendor Email
        // b) (Auto)   [QUEUE "getsendorder"] (ExchangedOrder) -> [QUEUE "replyorder"] QTReplyOrder

        // IncomimgMessageType.getsendorder             ORDER                     - Receive Vendor Order / Customer Order          
        // OutgoingMessageType.replyorder               RE: ORDER                 - Send Order Response (Vendor/Customer)   replyorder -> sendauto

        // IncomingMessageType.getreplyorder            Re: ORDER                 - Receive Order Response                  getemail -> getreplyorder

        // Front-end:
        // OutgoingMessageType.sendorder                ORDER                     - Send Vendor Order                       sendorder -> sendauto

        public enum InternalServices
        {
            adjinventory
        }

        public enum RequiredConfiguration
        {
            AdminEmail
        }


        public enum OutgoingEmailType
        {
            sendadmin,
            sendauto
        }

        public enum OutgoingMessageType
        {
            requiredorder,                 // VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
            generateorder,                 // ORDER                   -   Send Vendor Order
            requirecompleteorder,          // Re: ORDER               -   Ask Vendor Admin for Confirmation of Generated Order
            remindadminunordered,          // PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
            remindadminunscheduled,        // PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
            remindadminuncompleted,        // PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date
            Unknown
        }

        public static string SetSubject(OutgoingMessageType req)
        {
            switch (req)
            {
                case OutgoingMessageType.requiredorder: 
                    return "VENDOR ORDER NEEDED:";          //  Ask Admin For Permission to Place Order
                case OutgoingMessageType.generateorder: 
                    return "ORDER:";                        //  Send Vendor Order
                case OutgoingMessageType.requirecompleteorder: 
                    return "Re: ORDER:";                    //  Send Order Response
                case OutgoingMessageType.remindadminunordered: 
                    return "PENDING UNORDERED:";            //  Pending Unordered Orders - No Order Date
                case OutgoingMessageType.remindadminunscheduled: 
                    return "PENDING UNSCHEDULED:";          //  Pending Unscheduled Orders - No Scheduled Date
                case OutgoingMessageType.remindadminuncompleted: 
                    return "PENDING UNCOMPLETED:";          //  Pending Uncompleted Orders - No Completed Date
                default: return String.Empty;
            }
        }

        public enum IncomingMessageType
        {
            processrequiredorder,          //  Re: VENDOR ORDER NEEDED -   Get Response From Admin
            processorder,                   //  ORDER                  -   Receive Vendor Order / Customer Order
            completeorder,                  //  Re: ORDER              -   Receive Order Response
            unknown
        }

        public static IncomingMessageType ParseSubject(string emailsubject)
        {
            if (emailsubject.StartsWith("Re: " + SetSubject(OutgoingMessageType.requiredorder))) return IncomingMessageType.processrequiredorder;
            if (emailsubject.StartsWith(SetSubject(OutgoingMessageType.generateorder))) return IncomingMessageType.processorder;
            if (emailsubject.StartsWith(SetSubject(OutgoingMessageType.requirecompleteorder))) return IncomingMessageType.completeorder;
            return IncomingMessageType.unknown;
        }
    }
}