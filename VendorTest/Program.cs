using Core;
using Core.Core.API;
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
    public partial class Program
    {

        public static IDBObject DBLocation { get; set; }

        public static IConfiguration Configuration;

        public static Dictionary<Type, Dictionary<String, Base>> MainDBCollections;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.CaptureStartupErrors(true); // the default
                webBuilder.UseSetting("detailedErrors", "true");
                webBuilder.UseStartup<Startup>();
            });

        public void Init(IConfiguration configfromstartup)
        {
            Configuration = configfromstartup;
            //DBLocation = new FileObject(Configuration["DBLocation"]);
            DBLocation = new CosmosDB.CosmoObject(Configuration["ConnectionStrings:AzureCosmos"]);
            MainDBCollections = Base.PopulateMainCollection(DBLocation);
            WorkCenterPartAPI.DBCollection = MainDBCollections;
            OrderAPI.MainDBCollections = MainDBCollections;
            Exchange.SetTimer(DBLocation, ref MainDBCollections);
        }
               
    }
}


