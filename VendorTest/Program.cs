using Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace VendorTest
{
    public partial class Program
    {

        public static IDBObject DBLocation { get; set; }

        public static IConfiguration Configuration;

        public static Dictionary<Type, Dictionary<String, IBase>> MainDBCollections;

        public static IQueueService SendOrderService;
        public static IQueueService AdjInventoryService;

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
            SendOrderService = new CosmosDB.AzureQueue(Configuration["AzureWebJobsStorage"], "Outgoing");
            AdjInventoryService = new CosmosDB.AzureQueue(Configuration["AzureWebJobsStorage"], "adjinventory");
        }
               
    }
}


