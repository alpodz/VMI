using Interfaces;
using System.Collections;
using System.Text.Json;

namespace Core.DB
{
    public class FileObject : IDBObject
    {
        public FileObject(string dBLocation) { _DBLocation = dBLocation; }

        private static string _DBLocation = string.Empty;

        private static readonly string FileFormat = ".json";

        private static string? _Name;
        private static string? Name
        {
            get { return $"{_DBLocation}{_Name}{FileFormat}"; }
            set { _Name = value; }
        }

        public async Task<IList> PopulateCollectionAsync(Type itemType, Type colType, IList col, string ID)
        {
            Name = itemType.Name;
            if (!File.Exists(Name)) throw new Exception(Name + "does not exist.");
            using FileStream openStream = File.OpenRead(Name);
            var returned = await JsonSerializer.DeserializeAsync(openStream, colType);
            if (returned == null) return col;
            return (IList)returned;
        }

        public async Task SaveCollectionAsync(Type CollectionType, IList CollectionToSave)
        {
            var listType = typeof(List<>);
            var constr = listType.MakeGenericType(CollectionType);
            if (Activator.CreateInstance(constr) is not IList instance)
                throw new Exception(CollectionType.Name + " has no parameterless constructor");
            foreach (var obj in CollectionToSave)
                instance.Add(obj);
            using MemoryStream ms = new();
            using var writer = new Utf8JsonWriter(ms);
            System.Text.Json.JsonSerializer.Serialize(writer, instance, constr);
            await SaveToFileSystem(System.Text.Encoding.UTF8.GetString(ms.ToArray()), CollectionType);
        }

        private static async Task SaveToFileSystem(string json, Type CollectionType)
        {
            Name = CollectionType.Name;
            string tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, json);
            if (File.Exists(Name))
                File.Delete(Name);
            File.Move(tempFile, Name);
        }

        public static Task AsyncException()
        {
            throw new Exception("Not Implemented!");
        }

        public async Task SaveObjectAsync<T>(IBase item)
        {
            await AsyncException();
        }

        public async Task<IList> PopulateCollectionAsync(Type type)
        {
            var listType = typeof(List<>);
            var constr = listType.MakeGenericType(type);
            if (Activator.CreateInstance(constr) is not IList instance)
                throw new Exception(type.Name + " has no parameterless constructor");


            return await PopulateCollectionAsync(type, constr, instance, string.Empty);
        }

        public async Task<IBase?> GetObjectAsync<T>(string ID)
        {
            await AsyncException();
            return null;
        }

    }
}
