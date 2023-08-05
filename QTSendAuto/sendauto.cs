using System;
using Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace QTSendAuto
{
    public class sendauto
    {
        [FunctionName("sendauto")]
        public void Run([QueueTrigger("sendauto")]ExchangedOrders outgoingorder, ILogger log,[QueueTrigger("sendemail")] string sendemail)
        {
            // need to make a 'friendly' outgoing order email

            sendemail = "";
        }
    }
}
