using Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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

        public void PopulateCollection(Type coltype, Type collistType, ref IList? col, string ID)
        {
            if (col == null) return;
            var task = PopulationCollectionAsync(coltype, collistType, col, ID);
            task.Wait();
            var result = task.Result;
            if (result == null) return;
            col = task.Result;
        }

        private async Task<IList?> PopulationCollectionAsync(Type colType, Type listType, IList colListType, String QueryAddOn)
        {
            var query = new QueryDefinition($"SELECT * FROM _{ContainerName} c WHERE c.Partition LIKE '_{colType.Name}_{QueryAddOn}'");
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
                    if (_documents == null) return colListType;
                    var result = JsonConvert.DeserializeObject(_documents.ToString(), listType);
                    if (result == null) return colListType;
                    var result2 = (IList)result;
                    //if (colListType.Count == 0) colListType = result2;
                    //else
                        foreach (Base item in result2)
                    {
                        item.IsNew = false;
                        colListType.Add(item);
                    }
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
                await Save(collectionType, item, true);
        }

        private async Task Save(Type objectType, IBase item, bool listsave)
        {
            if (_container == null) return;
            var BaseItem = (Base)item;
            if (BaseItem.IsDeleted)
                await _container.DeleteItemStreamAsync(item.GetPrimaryKeyValue(), new PartitionKey(BaseItem.Partition));
            if (listsave && !BaseItem.IsDirty && !BaseItem.IsNew) return;
            using MemoryStream savestream = new();
            using StreamWriter writer = new(savestream);
            writer.Write(System.Text.Json.JsonSerializer.Serialize(item, objectType));
            writer.Flush();
            savestream.Position = 0;
            var upserteditem = await _container.UpsertItemStreamAsync(savestream, new PartitionKey(BaseItem.Partition));
            BaseItem.IsDirty = false;
        }

        public async void SaveObject(Type objectType, IBase item, bool listsave)
        {
            await Save(objectType, item, listsave);
        }
    }

}
