using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace Interfaces
{
    public interface IMailClient
    {
        IEnumerable<uint> Search();

        MailMessage GetMessage(uint msgnum);

        void DeleteMessage(uint msgnum);

        IMailClient GetMailClient(IConfigHelper configHelper, IInventory inventory);

        String SystemMessage();

        void SendEmail(Attachment attach, string To, string Subject, string Body, bool isHtml);
    }
}
