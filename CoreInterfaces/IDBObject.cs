using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IDBObject
    {
        string Name { get; set;  }
        Task SaveObjectAsync<T>(IBase item);
        Task<IList?> PopulateCollectionAsync(Type type);
        Task<IList?> PopulateCollectionAsync(Type itemType, Type listType, IList col, string ID);
        Task SaveCollectionAsync(Type type, IList col);
        Task<IBase?> GetObjectAsync<T>(string ID);
    }
}