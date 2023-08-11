using Interfaces;
using System.Collections;
using System.Text.Json;

namespace Core.DB
{
    public class FileObject : IDBObject
    {
        public FileObject(string dBLocation) { _DBLocation = dBLocation; }

        private static string _DBLocation = string.Empty;

        public static string FileFormat = ".json";

        public string _Name = string.Empty;
        public string Name
        {
            get { return $"{_DBLocation}{_Name}{FileFormat}"; }
            set { _Name = value; }
        }

        public async Task<IList?> PopulateCollectionAsync(Type itemType, Type colType, IList col, string ID)
        {
            if (!File.Exists(Name)) return null;
            using FileStream openStream = File.OpenRead(Name);
            return (IList?)await JsonSerializer.DeserializeAsync(openStream, colType);
        }

        public async Task SaveCollectionAsync(Type CollectionType, IList CollectionToSave)
        {
            var listType = typeof(List<>);
            var constr = listType.MakeGenericType(CollectionType);
            var instance = (IList?)Activator.CreateInstance(constr);
            foreach (var obj in CollectionToSave)
            {
                instance?.Add(obj);
            }
            using MemoryStream ms = new();
            using var writer = new Utf8JsonWriter(ms);
            System.Text.Json.JsonSerializer.Serialize(writer, instance, constr);
            await SaveToFileSystem(System.Text.Encoding.UTF8.GetString(ms.ToArray()));
        }

        private async Task SaveToFileSystem(string json)
        {
            string tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, json);
            if (File.Exists(Name))
                File.Delete(Name);
            File.Move(tempFile, Name);
        }

        public Task SaveObjectAsync<T>(IBase item)
        {
            throw new NotImplementedException();
        }

        public async Task<IList?> PopulateCollectionAsync(Type type)
        {
            var listType = typeof(List<>);
            var constr = listType.MakeGenericType(type);
            return await PopulateCollectionAsync(type, constr, null, string.Empty);
        }

        public async Task<IBase?> GetObjectAsync<T>(string ID)
        {
            throw new NotImplementedException();
        }

    }
}
