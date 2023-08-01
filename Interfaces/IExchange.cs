using System.Net.Mail;

namespace Interfaces
{
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

    public interface IExchange
    {
        IMailClient Client { get; set; }

        string SetSubject(string req);
        void SendAdmin(EnuSendAdmin req, string orderID, string body);

        //void SendAuto(EnuSendAuto req, ExchangedOrders order);
        //void SendEmail(Attachment attach, string To, string Subject, string Body, bool isHtml);
    }
}