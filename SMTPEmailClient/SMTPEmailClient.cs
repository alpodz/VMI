using Core.Core;
using DB.Admin;
using Interfaces;
using S22.Imap;
using System.Net.Mail;

namespace SMTPEmailClient;

public class SMTPEmailClient : IMailClient
{
    private String _systemMessage = string.Empty;
    private ImapClient? _imapClient = null;
    private IInventory? Inventory = null;
    private IConfigHelper? _configHelper;
    private static String smtp = "SmtpAccount", imap = "ImapAccount", user = "User", pass = "Password", admin = "AdminEmail";
    private static String[] RequiredKeys = { smtp, imap, user, pass, admin };

    public void DeleteMessage(uint msgnum)
    {
        throw new NotImplementedException();
    }

    public void Client_NewMessage(object sender, S22.Imap.IdleMessageEventArgs e)
    {
        if (Inventory!=null)
            Inventory.ExecuteWorkAgainstMailMessage(e.MessageUID);
    }

    public IMailClient GetMailClient(IConfigHelper configHelper, IInventory inventory)
    {
        _systemMessage = configHelper.HasRequiredValues(RequiredKeys);
        _configHelper = configHelper;

        _imapClient = new S22.Imap.ImapClient(configHelper.Values[imap], 993, configHelper.Values[user], configHelper.Values[pass], S22.Imap.AuthMethod.Login, true);
        if (_imapClient == null) return null;

        try
        {
            if (_imapClient.Supports("IDLE"))
            {
                _imapClient.NewMessage += Client_NewMessage;
            }
        }
        catch { }

        return this;
    }

    public void SendEmail(Attachment attach, string To, string Subject, string Body, bool isHtml)
    {
        if (_configHelper == null) return;
        using (var msg = new MailMessage(_configHelper.Values[user], To, Subject, Body))
        using (var client = new SmtpClient(_configHelper.Values[smtp], 587))
        {
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(_configHelper.Values[user], _configHelper.Values[pass]);
            if (attach != null) msg.Attachments.Add(attach);
            msg.IsBodyHtml = isHtml;
            client.Send(msg);
        }
    }

    public MailMessage GetMessage(uint msgnum)
    {
        if (_imapClient == null) return null; 
        return _imapClient.GetMessage(msgnum);
    }

    public IEnumerable<uint> Search()
    {
        if (_imapClient == null) return Enumerable.Empty<uint>();
        return _imapClient.Search(S22.Imap.SearchCondition.All());
    }

    public string SystemMessage()
    {
        return _systemMessage;
    }
}