using Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

    public interface IBase
{
    string GetPrimaryKeyValue();
    void PopulateDerivedFields(IDBObject DBLocation, ref Dictionary<Type, Dictionary<String, IBase>> MainDB);
}
