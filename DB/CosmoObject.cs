using DB.Vendor;
using Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Azure.Cosmos.Spatial;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CosmosDB
{
    public class CosmoObject : IDBObject
    {
        CosmosClient _client;
        Container? _container;
        Database? _db;
        //Type? _typeofContainer;
        
        string ContainerName { get { return "Objects"; } }
        string PartitionKey { get { return "Partition"; } }

        public CosmoObject(String connectionString)
        {
            _client = new CosmosClient(connectionString);
            var task = PrepConnection();
            task.Wait();
        }

        private async Task PrepConnection()
        {
            _db = await _client.CreateDatabaseIfNotExistsAsync("VMI");
            await _db.DeleteAsync();
            _db = await _client.CreateDatabaseIfNotExistsAsync("VMI");
            if (_db == null || ContainerName == null || PartitionKey == null) return;
            _container = await _db.CreateContainerIfNotExistsAsync(ContainerName, "/" + PartitionKey, 1000);
        }

        string IDBObject.Name { get; set; }

        public void PopulateCollection(Type coltype, Type collistType, ref IList col)
        {
            var task = PopulationCollectionAsync(coltype, collistType, col);
            task.Wait();
            var result = task.Result;
            if (result == null) return;
            col = task.Result;                      
        }

        private async Task<IList> PopulationCollectionAsync(Type colType, Type listType, IList colListType)
        {
            var query = new QueryDefinition($"SELECT * FROM _{ContainerName} c WHERE c.Partition LIKE '_{colType.Name}_%'");
            using (FeedIterator iter = this._container.GetItemQueryStreamIterator(query))
            {
                while (iter.HasMoreResults)
                {
                    using ResponseMessage response = await iter.ReadNextAsync();
                    if (response == null) return colListType;
                    String output = new StreamReader(response.Content).ReadToEnd();
                    var obj = JObject.Parse(output);
                    obj.TryGetValue("Documents", StringComparison.InvariantCultureIgnoreCase,out var _documents);
                    var result = (IList) JsonConvert.DeserializeObject(_documents.ToString(), listType);
                    if (colListType.Count == 0) colListType = result;
                    else
                        foreach ( var item in result)
                            colListType.Add(item);
                }
            }
            return colListType;
        }

        //private static MyCosmosResponse Mine(byte[] contents)
        //{          
        //    var jsonreader = new System.Text.Json.Utf8JsonReader(contents);
        //    //var next = jsonreader.GetString();

        //    var response = System.Text.Json.JsonSerializer.Deserialize<MyCosmosResponse>(ref jsonreader); //, MyCosmosResponse);
        //    return response;
        //}

        public async void SaveCollection(Type collectionType, IList col)
        {            
            foreach (Base item in col)
            {
                if (!item.IsDirty) continue;
                using MemoryStream savestream = new();
                using StreamWriter writer = new(savestream);
                writer.Write(System.Text.Json.JsonSerializer.Serialize(item, collectionType)); 
                writer.Flush();
                savestream.Position = 0;
                var upserteditem = await _container.UpsertItemStreamAsync(savestream, new PartitionKey(item.Partition));
                item.IsDirty = false;
            }
        }
    }
}
