using Interfaces;
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

        #region Private Generic Methods
                
        public async Task SaveCollection(Type type, IList<IBase> col)
        {
            foreach (IBase item in col)
                await Save(type, item, true);
        }

        private async Task Save(Type type, IBase item, bool listsave)
        {
            if (_container == null) return;
            var BaseItem = (Base)item;
            if (BaseItem.IsDeleted)
                await _container.DeleteItemStreamAsync(item.GetPrimaryKeyValue(), new PartitionKey(BaseItem.Partition));
            if (listsave && !BaseItem.IsDirty && !BaseItem.IsNew) return;
            using MemoryStream savestream = new();
            using StreamWriter writer = new(savestream);
            writer.Write(System.Text.Json.JsonSerializer.Serialize(item, type));
            writer.Flush();
            savestream.Position = 0;
            var upserteditem = await _container.UpsertItemStreamAsync(savestream, new PartitionKey(BaseItem.Partition));
            BaseItem.IsDirty = false;
        }

        #endregion

        public async Task<IList> PopulateTypeCollection(Type item, String ID = "%")
        {
            var listType = typeof(List<>);
            var constr = listType.MakeGenericType(item);
            if (Activator.CreateInstance(constr) is not object actinstance)
                throw new Exception("The Type: " + item.Name + " has no constructor which has no parameter!");
            var instance = (IList)actinstance;
            return await PopulateCollectionAsync(item, constr, instance, ID);
        }

        public async Task SaveObjectAsync<T>(IBase item)
        {
            await Save(typeof(T), item, false);
        }

        public async Task<IList> PopulateCollectionAsync(Type type)
        {
            return await PopulateTypeCollection(type);
        }

        public async Task SaveCollectionAsync(Type type, IList col)
        {
            foreach (IBase item in col)
                await Save(type, item, true);
        }

        public async Task<IBase?> GetObjectAsync<T>(string ID)
        {
            var col = await PopulateTypeCollection(typeof(T), ID);
            return (IBase?)(col == null ? default : col[0]);
        }

        public async Task<IList> PopulateCollectionAsync(Type itemType, Type listType, IList col, string ID)
        {
            var query = new QueryDefinition($"SELECT * FROM _{ContainerName} c WHERE c.Partition LIKE '_{itemType.Name}_{ID}'");
            
            if (_container == null) 
                return col;
            
            using (FeedIterator iter = this._container.GetItemQueryStreamIterator(query))
            {
                while (iter.HasMoreResults)
                {
                    using ResponseMessage response = await iter.ReadNextAsync();
                    if (response == null)
                        return col;

                    using var reader = new StreamReader(response.Content);
                    String output = await reader.ReadToEndAsync();
                    var obj = JObject.Parse(output);

                    if (!obj.TryGetValue("Documents", StringComparison.InvariantCultureIgnoreCase, out var _documents))
                        return col;

                    var result = JsonConvert.DeserializeObject(_documents.ToString(), listType);
                    
                    if (result == null)
                        return col;

                    var resultList = (IList)result;
                    foreach (IBase item in resultList)
                    {
                        item.MarkOld();
                        col.Add(item);
                    }
                }
            }

            return col;
        }
    }

}
