using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Net.Mail;
using System.Text;

namespace GMailClient
{
    public class Class1
    {
        private async static Task<GmailService> CreateGmailService(string clientSecretsPath)
        {
            // Load the client secrets file and create the Gmail service
            UserCredential credential2;

            using (var stream = new System.IO.FileStream(clientSecretsPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                credential2 = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { GmailService.Scope.MailGoogleCom },
                    "vendormanaged@gmail.com", CancellationToken.None, new FileDataStore("Books.ListMyLibrar"));
            }

            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential2,
                ApplicationName = "Gmail API Example"
            });
        }

        public static async Task Login(string[] args)
        {
            try
            {
                // Replace with your JSON file path containing client secrets
                string clientSecretsPath = "C:\\Users\\Al\\Desktop\\client_secret_173415804745-kb7n26nckiogfnqa5hlus3auivte4gi7.apps.googleusercontent.com.json";

                // Create Gmail service
                GmailService service = await CreateGmailService(clientSecretsPath);

                List<Message> unreadMessages = ListMessages(service, "label:unread");

                Console.WriteLine("Unread Emails:");
                foreach (var message in unreadMessages)
                {
                    Console.WriteLine($"Subject: {message.Payload.Headers.Where(x => x.Name == "Subject").First().Value}");
                    Console.WriteLine($"From: {message.Payload.Headers.Where(x => x.Name == "From").First().Value}");
                    Console.WriteLine($"Date: {message.Payload.Headers.Where(x => x.Name == "Date").First().Value}");

                    // Check if the email has attachments                    
                    bool hasAttachments = HasAttachments(service, message.Id);
                    Console.WriteLine($"Has Attachments: {hasAttachments}");

                              if (hasAttachments)
                              {
                                  List<MessagePart> attachmentParts = GetAttachmentParts(service, message.Id);
                     DownloadAttachments(service, message.Id, attachmentParts);
                    Console.WriteLine("Attachments downloaded.");
                                   }

                    // Mark the email as read (optional)
                     MarkAsRead(service, message.Id);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.ReadKey();
        }


        // Helper function to list messages based on query
        private static List<Message> ListMessages(GmailService service, string query)
        {
            var listRequest = service.Users.Messages.List("me");
            listRequest.Q = query;
            var messages = new List<Message>();

            do
            {
                try
                {
                    var listResult = listRequest.Execute();
                    if (listResult.Messages != null)
                    {
                        messages.AddRange(listResult.Messages);
                    }
                    listRequest.PageToken = listResult.NextPageToken;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            } while (!string.IsNullOrEmpty(listRequest.PageToken));

            return messages;
        }

        // Helper function to check if an email has attachments
        private static bool HasAttachments(GmailService service, string messageId)
        {
            try
            {
                var message = service.Users.Messages.Get("me", messageId).Execute();
                return message.Payload?.Parts != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking attachments: {ex.Message}");
                return false;
            }
        }

        // Helper function to get attachment parts from an email
        private static List<MessagePart> GetAttachmentParts(GmailService service, string messageId)
        {
            var attachmentParts = new List<MessagePart>();
            try
            {
                var message = service.Users.Messages.Get("me", messageId).Execute();
                var parts = message.Payload?.Parts;

                if (parts != null)
                {
                    foreach (var part in parts)
                    {
                        if (part.Filename != null && part.Body.AttachmentId != null)
                        {
                            attachmentParts.Add(part);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting attachment parts: {ex.Message}");
            }

            return attachmentParts;
        }

        // Helper function to download attachments from an email
        private static void DownloadAttachments(GmailService service, string messageId, List<MessagePart> attachmentParts)
        {
            try
            {
                foreach (var part in attachmentParts)
                {
                    var attachment = service.Users.Messages.Attachments.Get("me", messageId, part.Body.AttachmentId).Execute();
                    byte[] data = Convert.FromBase64String(attachment.Data);
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), part.Filename);

                    // Save the attachment to a local file
                    File.WriteAllBytes(filePath, data);
                    Console.WriteLine($"Attachment saved: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading attachments: {ex.Message}");
            }
        }

        // Helper function to mark an email as read
        private static void MarkAsRead(GmailService service, string messageId)
        {
            try
            {
                ModifyMessageRequest mods = new ModifyMessageRequest();
                mods.RemoveLabelIds = new List<string>() { "UNREAD" };
                service.Users.Messages.Modify(mods, "me", messageId).Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking email as read: {ex.Message}");
            }
        }

        private static void SendEmailWithAttachment(GmailService service, string to, string subject, string body, string attachmentFilePath)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("your-email@gmail.com");
                mailMessage.To.Add(to);
                mailMessage.Subject = subject;
                mailMessage.Body = body;

                Attachment attachment = new Attachment(attachmentFilePath);
                mailMessage.Attachments.Add(attachment);

                var mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mailMessage);
                var sb = new StringBuilder();
                using (var memoryStream = new MemoryStream())
                {
                    mimeMessage.WriteTo(memoryStream);
                    string encodedEmail = Base64UrlEncode(memoryStream.ToArray());
                    var message = new Message { Raw = encodedEmail };
                    service.Users.Messages.Send(message, "me").Execute();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

        // Helper function to base64 encode the message
        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }
    }
}
