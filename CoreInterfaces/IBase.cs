using Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

    public interface IBase
{
    void MarkOld();
    void MarkDeleted();
    string? GetPrimaryKeyValue();
    void PopulateDerivedFields(IDBObject DBLocation, ref Dictionary<Type, Dictionary<String, IBase>> MainDB);
    Dictionary<Type, Dictionary<String, IBase>> MainDBCollections { get; set; }
    IDBObject DBLocation { get; set; }
    IQueueService SendOrderService { get; set; }
    IQueueService AdjInventoryService { get; set; }

}
