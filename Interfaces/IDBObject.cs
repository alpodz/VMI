using System;
using System.Collections;

namespace Interfaces
{
    public interface IDBObject
    {
        string Name { get; set;  }
        void PopulateCollection(Type collectionType, Type listType, ref IList col, string ID);
        void SaveCollection(Type collectionType, IList col);
        void SaveObject(Type objectType, IBase item, bool listsave);
    }
}