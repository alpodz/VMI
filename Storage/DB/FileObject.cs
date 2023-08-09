using Interfaces;
using System.Collections.Generic;
using System;
using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;

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
    }
}
