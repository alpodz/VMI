using System;
using System.Collections;
using System.Collections.Generic;

    public interface IBase
{
    void PopulateDerivedFields(string DBLocation, ref Dictionary<Type, Dictionary<String, Base>> MainDB);
}
