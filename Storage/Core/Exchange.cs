using DB.Admin;
using Interfaces;
using S22.Imap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;

namespace Core
{
    public class Exchange
    {
        #region "Private"

        private static Dictionary<Type, Dictionary<String, Base>> Cache;

        private static String smtp = "SmtpAccount", imap = "ImapAccount", user = "User", pass = "Password", admin = "AdminEmail";

        private static String[] RequiredKeys = { smtp, imap, user, pass, admin };

        private Dictionary<String, String> RequiredValues = new Dictionary<string, string>();

        public ImapClient Client { get; set; }
        public String SystemMessage { get; set; }

    public static string SetSubject(string req)
        {
            switch (req)
            {
                case nameof(EnuSendAdmin.RequestVendorOrder):               return "VENDOR ORDER NEEDED:";          //  Ask Admin For Permission to Place Order
                case nameof(EnuReceiveAdmin.RequestVendorOrderResponse):    return "Re: VENDOR ORDER NEEDED:";      //  Get Response From Admin
                case nameof(EnuSendAuto.SendVendorOrder):                   return "ORDER:";                        //  Send Vendor Order
                case nameof(EnuRecieveAuto.RecieveCustomerOrder):           return "ORDER:";                        //  Receive Vendor Order / Customer Order
                case nameof(EnuSendAuto.SendCustomerOrderResponse):         return "Re: ORDER:";                    //  Send Order Response
                case nameof(EnuSendAdmin.SendAdminCustomerFail):            return "ORDER FAILURE:";                //  If Neccessary
                case nameof(EnuRecieveAuto.RecieveOrderResponse):           return "Re: ORDER:";                    //  Receive Order Response
                case nameof(EnuSendAdmin.SendAdminVendorFail):              return "ORDER FAILURE:";                //  If Neccessary
                case nameof(EnuSendAdmin.OldUnOrdered):                     return "PENDING UNORDERED:";            //  Pending Unordered Orders - No Order Date
                case nameof(EnuSendAdmin.OldUnScheduled):                   return "PENDING UNSCHEDULED:";          //  Pending Unscheduled Orders - No Scheduled Date
                case nameof(EnuSendAdmin.OldUnCompleted):                   return "PENDING UNCOMPLETED:";          //  Pending Uncompleted Orders - No Completed Date
                case nameof(EnuReceiveAdmin.OldUnCompletedResponse):        return "Re: PENDING UNCOMPLETED:";      //  Set Completed Date
                default:                                                    return String.Empty;
            }
        }

        public enum EnuRecieveAuto
        {
            RecieveCustomerOrder,       // ReceiveCustomerOrder         In  -   Auto    ORDER                   -   Receive Vendor Order / Customer Order
            RecieveOrderResponse        // ReceiveOrderResponse         In  -   Auto    Re: ORDER               -   Receive Order Response
        }

        public enum EnuSendAuto
        {
            SendVendorOrder,            // SendVendorOrder              Out -   Auto    ORDER                   -   Send Vendor Order
            SendCustomerOrderResponse   // SendCustomerOrderResponse    Out -   Auto    Re: ORDER               -   Send Order Response
        }

        public enum EnuReceiveAdmin
        {
            RequestVendorOrderResponse, // RequestVendorOrderResponse   In  -   Man     Re: VENDOR ORDER NEEDED -   Get Response From Admin
            OldUnCompletedResponse      // OldUnCompletedResponse       In  -   Man     Re: PENDING UNCOMPLETED -   Set Completed Date
        }

        public enum EnuSendAdmin
        {
            RequestVendorOrder,         // RequestVendorOrder           Out -   Man     VENDOR ORDER NEEDED     -   Ask Admin For Permission to Place Order
            SendAdminCustomerFail,      // SendAdminCustomerFail        Out -   Man     ORDER FAILURE           -   If Neccessary
            SendAdminVendorFail,        // SendAdminVendorFail          Out -   Man     ORDER FAILURE           -   If Neccessary
            OldUnOrdered,               // OldUnOrdered                 Out -   Man     PENDING UNORDERED       -   Pending Unordered Orders - No Order Date
            OldUnScheduled,             // OldUnScheduled               Out -   Man     PENDING UNSCHEDULED     -   Pending Unscheduled Orders - No Scheduled Date
            OldUnCompleted              // OldUnCompleted               Out -   Man     PENDING UNCOMPLETED     -   Pending Uncompleted Orders - No Completed Date
        }

        #endregion


        #region Exchange

        public Exchange(ref Dictionary<Type, Dictionary<String, Base>> cache, out bool success)
        {
            Cache = cache;
            for (int loc = 0; loc < RequiredKeys.Count(); loc++)            
            {
                var item = Cache[typeof(Configuration)].Values.Cast<Configuration>().FirstOrDefault(a => a.Name == RequiredKeys[loc]);
                if (item != null)
                {
                    RequiredValues.Add(item.Name,item.Value);
                }
                else
                {
                    SystemMessage += RequiredKeys[loc] + "is missing from Configuration. <BR>";
                    success = false;
                    return;
                }
            }

            Client = new S22.Imap.ImapClient(RequiredValues[imap], 993, RequiredValues[user], RequiredValues[pass], S22.Imap.AuthMethod.Login, true);
            success = Client != null;
            return;
        }

        // retrieves requests for parts, calculate if this is possible, and send a response -- 'queues and waits for confirmation'
        // also recieves confirmations, this will set up a shipment
        public static ExchangedOrders RetrieveMail(MailMessage msg, string req)
        {
            if (msg.Subject.StartsWith(SetSubject(req)))
            {
                var incomingOrder = new ExchangedOrders();
                if (msg.Attachments.Count == 1)
                {
                    using (var att = msg.Attachments[0])
                    using (StreamReader sr = new StreamReader(att.ContentStream))
                    {
                        incomingOrder = System.Text.Json.JsonSerializer.Deserialize<ExchangedOrders>(sr.ReadToEnd());
                    }
                }
                else
                    incomingOrder.OrderedOrderID = msg.Subject.Substring(SetSubject(req).Length, 36);

                incomingOrder.subject = msg.Subject;
                incomingOrder.from = msg.From.Address;
                incomingOrder.body = msg.Body;

                if (!String.IsNullOrEmpty(incomingOrder.OrderedOrderID))
                    return incomingOrder;
            }
            return null;
        }

        public void SendEmail(Attachment attach, string To, string Subject, string Body, bool isHtml)
        {
            using (var msg = new MailMessage(RequiredValues[user], To, Subject, Body))
            using (var client = new SmtpClient(RequiredValues[smtp], 587))
            {
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(RequiredValues[user], RequiredValues[pass]);
                if (attach!=null) msg.Attachments.Add(attach);
                msg.IsBodyHtml = isHtml;
                client.Send(msg);
            }
        }

        public void SendAuto(EnuSendAuto req, ExchangedOrders order)
        {
            String seriallized = System.Text.Json.JsonSerializer.Serialize(order);
            byte[] buffer = Encoding.ASCII.GetBytes(seriallized);
            using (var str = new MemoryStream(buffer))
            using (var attach = new Attachment(str, order.OrderedOrderID + ".json"))
                SendEmail(attach, order.to, SetSubject(req.ToString()) + order.OrderedOrderID, order.body, false);
        }

        public void SendAdmin(EnuSendAdmin req, String orderID, String body)
        {
            try
            {
                SendEmail(null, RequiredValues[admin], SetSubject(req.ToString()) + orderID, body, true);
            }
            catch
            {

            }
        }

        #endregion

        private static System.Timers.Timer myTimer;
        private static Exchange email;
        private static Interfaces.IDBObject dBLocation;
        private static Dictionary<Type, Dictionary<String, Base>> MainDBCollections;

        public static void SetTimer(IDBObject DBLocation, ref Dictionary<Type, Dictionary<String, Base>> Collections)
        {
            dBLocation = DBLocation;
            MainDBCollections = Collections;
            myTimer = new Timer(10000);
            myTimer.Elapsed += OnTimedEvent;
            myTimer.AutoReset = true;
            myTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            // Stops the Timer so that 'work can be done' and keep the 10 second delay between actions
            myTimer.Stop();
            // prep the object
            var mine = new Inventory(dBLocation, ref MainDBCollections);
            // if there is already a email client setup, remove the event handler
            if (email != null && email.Client != null)
                email.Client.NewMessage -= mine.Client_NewMessage;
            try
            {
                // first time establishment
                bool success = true;
                if (email == null || email.Client == null) email = new Exchange(ref MainDBCollections, out success);
                if (!success) return;
                // attempt search, if fails, we'll reestablish
                var msgs = email.Client.Search(S22.Imap.SearchCondition.All());
            }
            catch
            {
                // if there is no email client / or it fails to do a search // reestablish email client
                email = new Exchange(ref MainDBCollections, out var success);
                if (!success) return;
                try
                {
                    // set up auto notify
                    if (email != null && email.Client != null)
                    if (email.Client.Supports("IDLE"))
                        email.Client.NewMessage += mine.Client_NewMessage;
                }
                catch
                {

                }
            }
            mine.ExecuteMaint(email);
            myTimer.Start();
        }
    }
}
