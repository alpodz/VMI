using System.Collections;

namespace Interfaces
{
    public interface IDBObject
    {
        Task SaveObjectAsync<T>(IBase item);
        Task<IList> PopulateCollectionAsync(Type type);
        Task SaveCollectionAsync(Type type, IList col);
        Task<IBase?> GetObjectAsync<T>(string ID);
    }
}