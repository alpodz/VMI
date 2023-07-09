using System;
using System.Collections;

namespace Interfaces
{
    public interface IDBObject
    {
        string Name { get; set;  }        
        void PopulateCollection(Type collectionType, Type listType, ref IList col);
        void SaveCollection(Type collectionType, IList col);
    }
}