using Interfaces;
using System.Collections.Generic;
using System;
using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        public void PopulateCollection(Type itemType, Type colType, ref IList col, string ID)
        {
            if (!File.Exists(Name)) return;
            var jsonreader = new System.Text.Json.Utf8JsonReader(File.ReadAllBytes(Name));
            var populatedlist = (IList)System.Text.Json.JsonSerializer.Deserialize(ref jsonreader, colType);
            if (populatedlist != null) col = populatedlist;
        }

        public void SaveCollection(Type CollectionType, IList CollectionToSave)
        {
            var listType = typeof(List<>);
            var constr = listType.MakeGenericType(CollectionType);
            var instance = (IList)Activator.CreateInstance(constr);
            foreach (var obj in CollectionToSave)
            {
                instance.Add(obj);
            }
            using (MemoryStream ms = new MemoryStream())
            using (var writer = new System.Text.Json.Utf8JsonWriter(ms))
            {
                System.Text.Json.JsonSerializer.Serialize(writer, instance, constr);
                SaveToFileSystem(System.Text.Encoding.UTF8.GetString(ms.ToArray()));
            }
        }

        private void SaveToFileSystem(string json)
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);
            if (File.Exists(Name))
                File.Delete(Name);
            File.Move(tempFile, Name);
        }

        public void SaveObject(Type objectType, IBase item, bool listsave)
        {
            throw new NotImplementedException();
        }

        public void SaveCollection<T>(IList<IBase> col)
        {
            throw new NotImplementedException();
        }

        public Task SaveObjectAsync<T>(IBase item)
        {
            throw new NotImplementedException();
        }

        public Task<IList> PopulateCollectionAsync(Type type)
        {
            throw new NotImplementedException();
        }

        public Task<IList> PopulateCollectionAsync(Type itemType, Type listType, IList col, string ID)
        {
            throw new NotImplementedException();
        }

        public Task SaveCollectionAsync<T>(IList<IBase> col)
        {
            throw new NotImplementedException();
        }

        public Task<IBase> GetObjectAsync<T>(string ID)
        {
            throw new NotImplementedException();
        }

        public Task SaveCollectionAsync(Type type, IList<IBase> col)
        {
            throw new NotImplementedException();
        }
    }
}
