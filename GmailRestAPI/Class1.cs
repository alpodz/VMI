namespace GmailRestAPI
{
    using System;
    using System.Collections.Generic;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Gmail.v1;
    using Google.Apis.Gmail.v1.Data;
    using Google.Apis.Services;
    using System.Linq;
    using Google.Apis.Util.Store;

        public class Program
        {
            public static async Task Main(string[] args)
            {
                // Replace with your JSON file path containing client secrets
                string clientSecretsPath = "C:\\Users\\Al\\Desktop\\client_secret_173415804745-kb7n26nckiogfnqa5hlus3auivte4gi7.apps.googleusercontent.com.json";

                // Create Gmail service
                GmailService service = await CreateGmailService(clientSecretsPath);

                // Fetch unread emails
                List<Message> unreadEmails = GetUnreadEmails(service);

                // Print email subjects
                foreach (var email in unreadEmails)
                {
                    Console.WriteLine("Subject: " + GetMessageSubject(service, email.Id));
                }

                Console.ReadLine();
            }

            private async static Task<GmailService> CreateGmailService(string clientSecretsPath)
            {
                // Load the client secrets file and create the Gmail service
                GoogleCredential credential;
            UserCredential credential2;

            using (var stream = new System.IO.FileStream(clientSecretsPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    credential2 = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        new[] { GmailService.Scope.MailGoogleCom },
                        "vendormanaged@gmail.com", CancellationToken.None, new FileDataStore("Books.ListMyLibrar"));
//                }

                //credential = GoogleCredential.FromStream(stream);
//                        .CreateScoped(GmailService.Scope.MailGoogleCom);
                }

                return new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential2,
                    ApplicationName = "Gmail API Example"
                });
            }

            private static List<Message> GetUnreadEmails(GmailService service)
            {
                UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
                request.Q = "is:unread"; // Fetch only unread emails
                ListMessagesResponse response = request.Execute();
                return (List<Message>)response.Messages;
            }

            private static string GetMessageSubject(GmailService service, string messageId)
            {
                UsersResource.MessagesResource.GetRequest request = service.Users.Messages.Get("me", messageId);
                request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                request.MetadataHeaders = new[] { "Subject" };
                Message message = request.Execute();
            return message.Payload.Headers.Where(x => x.Name == "Subject").First().Value;
            }
        }
    }