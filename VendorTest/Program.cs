using Core;
using Core.DB;
using Interfaces;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

namespace VendorTest
{
    public static partial class Program
    {

        public static IDBObject DBLocation { get; set; }

        public static IConfiguration Configuration;

        public static Dictionary<Type, Dictionary<String, Base>> MainDBCollections;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.CaptureStartupErrors(true); // the default
                webBuilder.UseSetting("detailedErrors", "true");
                webBuilder.UseStartup<Startup>();
            });

        private static System.Timers.Timer myTimer;
        private static Core.Exchange email;

        public static void Init(IConfiguration configfromstartup)
        {
            Configuration = configfromstartup;
            //DBLocation = new FileObject(Configuration["DBLocation"]);
            DBLocation = new CosmosDB.CosmoObject(Configuration["ConnectionStrings:AzureCosmos"]);
            MainDBCollections = Base.PopulateMainCollection(DBLocation);

            
            //SetTimer();
        }

        //private static void SetTimer()
        //{
        //    myTimer = new Timer(10000);
        //    myTimer.Elapsed += OnTimedEvent;
        //    myTimer.AutoReset = true;
        //    myTimer.Enabled = true;
        //}

        //private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        //{
        //    // Stops the Timer so that 'work can be done' and keep the 10 second delay between actions
        //    myTimer.Stop();
        //    // prep the object
        //    var mine = new Core.Inventory(DBLocation, ref MainDBCollections);
        //    // if there is already a email client setup, remove the event handler
        //    if (email != null)
        //        email.Client.NewMessage -= mine.Client_NewMessage;            
        //    try
        //    {
        //        // first time establishment
        //        if (email == null || email.Client == null)  email = new Core.Exchange(ref MainDBCollections);
        //        // attempt search, if fails, we'll reestablish
        //        var msgs = email.Client.Search(S22.Imap.SearchCondition.All());
        //    }
        //    catch
        //    {
        //        // if there is no email client / or it fails to do a search // reestablish email client
        //        email = new Core.Exchange(ref MainDBCollections);
        //        try
        //        {
        //            // set up auto notify
        //            if (email.Client.Supports("IDLE"))
        //                email.Client.NewMessage += mine.Client_NewMessage;
        //        }
        //        catch
        //        {

        //        }
        //    }
        //    mine.ExecuteMaint(email);
        //    myTimer.Start();
        //}
    }
}


