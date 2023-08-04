﻿using Interfaces;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;

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
            _container = await _db.CreateContainerIfNotExistsAsync(ContainerName, "/" + PartitionKey);
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
            if (_container == null) return colListType;
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
                        foreach (Base item in result)                           
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
                if (_container == null) continue;
                var upserteditem = await _container.UpsertItemStreamAsync(savestream, new PartitionKey(item.Partition));
                item.IsDirty = false;
            }
        }

    }

}
