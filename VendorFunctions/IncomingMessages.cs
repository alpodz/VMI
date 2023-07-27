using System;
using Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace VendorFunctions
{
    public class IncomingMessages
    {
        [FunctionName("ExecuteWorkAgainstMailMessage")]
        public void Run([QueueTrigger("incomingmessages", Connection = "")]Exchange.Message mail, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {mail.Subject}");
            // RequestVendorOrderResponse   In  -   Man     Re: VENDOR ORDER NEEDED -   Get Response From Admin              
            // SendVendorOrder              Out -   Auto    ORDER                   -   Send Vendor Order
            if (CheckForVendorOrderResponse(Exchange.RetrieveMail(mail, Exchange.EnuReceiveAdmin.RequestVendorOrderResponse.ToString()))) return;
            // ReceiveCustomerOrder         In  -   Auto    ORDER                   -   Receive Vendor Order / Customer Order
            // SendCustomerOrderResponse    Out -   Auto    Re: ORDER               -   Send Order Response
            if (GetOrders(email, Exchange.RetrieveMail(mail, Exchange.EnuRecieveAuto.RecieveCustomerOrder.ToString()))) return;
            // ReceiveOrderResponse         In  -   Auto    Re: ORDER               -   Receive Order Response
            if (GetResponsesToOrders(Exchange.RetrieveMail(mail, Exchange.EnuRecieveAuto.RecieveOrderResponse.ToString()))) return;
        }
    }
}