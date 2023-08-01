using Core;
using Core.Core;
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

namespace VendorTest
{
    public class Exchange : IExchange
    {
        #region "Private"

        private static Dictionary<Type, Dictionary<string, Base>> Cache;
        private static IConfigHelper ConfigHelper;

        public IMailClient Client { get; set; }

        public string SetSubject(string req)
        {
            switch (req)
            {
                case nameof(EnuSendAdmin.RequestVendorOrder): return "VENDOR ORDER NEEDED:";          //  Ask Admin For Permission to Place Order
                case nameof(EnuReceiveAdmin.RequestVendorOrderResponse): return "Re: VENDOR ORDER NEEDED:";      //  Get Response From Admin
                case nameof(EnuSendAuto.SendVendorOrder): return "ORDER:";                        //  Send Vendor Order
                case nameof(EnuRecieveAuto.RecieveCustomerOrder): return "ORDER:";                        //  Receive Vendor Order / Customer Order
                case nameof(EnuSendAuto.SendCustomerOrderResponse): return "Re: ORDER:";                    //  Send Order Response
                case nameof(EnuSendAdmin.SendAdminCustomerFail): return "ORDER FAILURE:";                //  If Neccessary
                case nameof(EnuRecieveAuto.RecieveOrderResponse): return "Re: ORDER:";                    //  Receive Order Response
                case nameof(EnuSendAdmin.SendAdminVendorFail): return "ORDER FAILURE:";                //  If Neccessary
                case nameof(EnuSendAdmin.OldUnOrdered): return "PENDING UNORDERED:";            //  Pending Unordered Orders - No Order Date
                case nameof(EnuSendAdmin.OldUnScheduled): return "PENDING UNSCHEDULED:";          //  Pending Unscheduled Orders - No Scheduled Date
                case nameof(EnuSendAdmin.OldUnCompleted): return "PENDING UNCOMPLETED:";          //  Pending Uncompleted Orders - No Completed Date
                case nameof(EnuReceiveAdmin.OldUnCompletedResponse): return "Re: PENDING UNCOMPLETED:";      //  Set Completed Date
                default: return string.Empty;
            }
        }


        #endregion


        #region Exchange

        public Exchange(IInventory inventory, IMailClient mailClient, ref Dictionary<Type, Dictionary<string, Base>> cache, out bool success)
        {
            Cache = cache;
            ConfigHelper = new ConfigHelper(cache[typeof(Configuration)]);
            Client = mailClient.GetMailClient(ConfigHelper, inventory);
            success = Client != null;
            return;
        }

        // retrieves requests for parts, calculate if this is possible, and send a response -- 'queues and waits for confirmation'
        // also recieves confirmations, this will set up a shipment
        public ExchangedOrders RetrieveMail(MailMessage msg, string req)
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

                if (!string.IsNullOrEmpty(incomingOrder.OrderedOrderID))
                    return incomingOrder;
            }
            return null;
        }

        public void SendAuto(EnuSendAuto req, ExchangedOrders order)
        {
            string seriallized = System.Text.Json.JsonSerializer.Serialize(order);
            byte[] buffer = Encoding.ASCII.GetBytes(seriallized);
            using (var str = new MemoryStream(buffer))
            using (var attach = new Attachment(str, order.OrderedOrderID + ".json"))
                Client.SendEmail(attach, order.to, SetSubject(req.ToString()) + order.OrderedOrderID, order.body, false);
        }

        public void SendAdmin(EnuSendAdmin req, string orderID, string body)
        {
            try
            {
                Client.SendEmail(null, ConfigHelper.Values["admin"], SetSubject(req.ToString()) + orderID, body, true);
            }
            catch
            {

            }
        }

        #endregion

        private static Timer myTimer;
        private static IExchange email;
        private static IDBObject dBLocation;
        private static Dictionary<Type, Dictionary<string, Base>> MainDBCollections;

        public static void SetTimer(IDBObject DBLocation, ref Dictionary<Type, Dictionary<string, Base>> Collections)
        {
            dBLocation = DBLocation;
            MainDBCollections = Collections;
            myTimer = new Timer(10000);
            myTimer.Elapsed += OnTimedEvent;
            myTimer.AutoReset = true;
            myTimer.Enabled = true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            // Stops the Timer so that 'work can be done' and keep the 10 second delay between actions
            myTimer.Stop();
            // prep the object
            ConfigHelper = new ConfigHelper(MainDBCollections[typeof(Configuration)]);
            IInventory mine = new Inventory(dBLocation, ref MainDBCollections);
            IMailClient mailClient = new SMTPEmailClient.SMTPEmailClient().GetMailClient(ConfigHelper, mine);

            bool success = true;
            if (email == null || email.Client == null) email = new Exchange(mine, mailClient, ref MainDBCollections, out success);
            if (success)
            {
                try
                {
                    // test connection
                    var msgs = email.Client.Search();
                    mine.ExecuteMaint(email);
                }
                catch { }
            }
            myTimer.Start();
        }
    }
}
