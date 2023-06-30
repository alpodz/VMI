using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;

public class Base : IBase
{
    public string DBLocation;
    public Dictionary<Type, Dictionary<String, Base>> MainDBCollections;

    #region CustomAttributes

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class PrimaryKey : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ForeignKey : System.Attribute
    {
        readonly Type name;
        public ForeignKey(Type name)
        {
            this.name = name;
        }
        public Type GetName()
        {
            return name;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]   
    public class DisplayProperty: System.Attribute
    {       
        public String _value;

        public String GetValue()
        {
            return _value;
        }
    }

    public static String Icon = "";

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TabIcon: System.Attribute
    {       
        public TabIcon(String icon)
        {
            Icon = icon;
        }
    }

    public class DefaultValue: DisplayProperty
    {
        public DefaultValue(String DefaultValue)
        {
            _value = DefaultValue;
        }
    }

    public class Label : DisplayProperty
    { 
        public Label(String value)
        {
            _value = value;
  //          DisplayProperties.Add(typeof(Label), this);
        }
    }

    public class DisplayWidth : DisplayProperty
    {
        public DisplayWidth(int width)
        {
            _value = width.ToString();
    //        DisplayProperties.Add(typeof(DisplayWidth), this);
        }
    }

    //public class Label : System.Attribute
    //{
    //    readonly String value;
    //    public Label(String value)
    //    {
    //        this.value = value;
    //    }
    //    public String GetValue()
    //    {
    //        return value;
    //    }
    //}

    //[System.AttributeUsage(System.AttributeTargets.Property)]
    //public class DisplayWidth : System.Attribute
    //{
    //    readonly int _width;
    //    public DisplayWidth(int width)
    //    {
    //      this._width = width;
    //    }
    //    public int GetWidth()
    //    {
    //        return _width;
    //    }
    //}

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class ReadOnly : System.Attribute { }

    #endregion

    public static PropertyInfo GetKey(Type item, String TypeOfKey)
    {
        if (item == null || String.IsNullOrEmpty(TypeOfKey)) return null;
        foreach (var prop in item.GetProperties())
            foreach (var ca in prop.CustomAttributes)
                if (ca.AttributeType.Name == TypeOfKey)
                    return prop;
        return null;
    }

    public static bool GetAttribute(PropertyInfo prop, Type attrib, out Object output)
    {
        output = null;
        var attribs = prop.GetCustomAttributes(attrib, false);
        if (attribs.Length == 0) return false;
        output = attribs[0];
        return true;
    }

    public static bool GetAttribute(PropertyInfo prop, Type attrib)
    {
        return prop.GetCustomAttributes(attrib, false).Length > 0;
    }

    public static string GetDisplayProperty(PropertyInfo prop, Type attrib, string DefaultValue)
    {
        var attribs = prop.GetCustomAttributes(attrib, false);
        if (attribs.Length == 0) return DefaultValue;
        return ((DisplayProperty)attribs[0]).GetValue();
    }
    
    public static string GetPrimaryKey(Type item)
    {
        var KeyProperty = GetPrimary(item);
        if (KeyProperty == null) return null;
        return KeyProperty.Name;
    }
    public static PropertyInfo GetPrimary(Type item)
    {
        return GetKey(item, "PrimaryKey");
    }

    public string GetPrimaryKeyValue()
    {
        return GetPrimary(this.GetType()).GetValue(this).ToString();
    }

    public void SetProperty(PropertyInfo propertyinfo, string PropertyValue)
    {
        if (propertyinfo == null)                               return;
        if (PropertyValue == null)                              propertyinfo.SetValue(this, null);
        else if (propertyinfo.PropertyType == typeof(decimal))  propertyinfo.SetValue(this, Convert.ToDecimal(PropertyValue));
        else if (propertyinfo.PropertyType == typeof(Int32))    propertyinfo.SetValue(this, Convert.ToInt32(PropertyValue));
        else if (propertyinfo.PropertyType == typeof(DateTime))
        {
            var convert = DateTime.MinValue;
            if (PropertyValue != String.Empty) convert = Convert.ToDateTime(PropertyValue);
            propertyinfo.SetValue(this, convert);
        }
        else if (propertyinfo.PropertyType == typeof(DateTime?))
        {
            if (PropertyValue != String.Empty) propertyinfo.SetValue(this, Convert.ToDateTime(PropertyValue));
            else propertyinfo.SetValue(this, null);
        }
        else if (propertyinfo.PropertyType == typeof(String))   propertyinfo.SetValue(this, Convert.ToString(PropertyValue));
        else if (propertyinfo.PropertyType == typeof(bool))     propertyinfo.SetValue(this, Convert.ToBoolean(PropertyValue));
        else                                                    propertyinfo.SetValue(this, PropertyValue);
    }

    public static void AddtoDBCollection(Type item, Dictionary<String, Base> instance)
    {
        var newi = (Base)Activator.CreateInstance(item);
        var guid = Guid.NewGuid().ToString();
        item.GetRuntimeProperty(GetPrimaryKey(item)).SetValue(newi, guid);
        instance.Add(guid, newi);
    }
    
    private static string FormatFile = "{0}.json";

    public static Dictionary<Type, Dictionary<String, Base>> PopulateMainCollection(string DBLocation)
    {
        var DBClassObjects = Assembly.Load("Core").GetTypes().Where(t => t.IsSubclassOf(typeof(Base)));
        var MainDBCollections = new Dictionary<Type, Dictionary<String, Base>>();
        var listType = typeof(List<>);

        foreach (Type item in DBClassObjects)
        {
            var constr = listType.MakeGenericType(item);
            var instance = (IList)Activator.CreateInstance(constr);
            var DBFileName = string.Format(DBLocation + FormatFile, item.Name);
            if (File.Exists(DBFileName))
            {
                var jsonreader = new System.Text.Json.Utf8JsonReader(File.ReadAllBytes(DBFileName));
                var populatedlist = (IList)System.Text.Json.JsonSerializer.Deserialize(ref jsonreader, constr);
                if (populatedlist != null) instance = populatedlist;
            }
            MainDBCollections.Add(item, instance.Cast<Base>().ToDictionary(a => a.GetPrimaryKeyValue()));
            // for some collections, I want to include 'None option'            
            if (item == typeof(DB.Admin.Workcenter) && !MainDBCollections[typeof(DB.Admin.Workcenter)].ContainsKey("0"))
            {
                var DefaultWC = new DB.Admin.Workcenter
                {
                    WorkcenterID = "0",
                    Name = "Shipment From Vendor"
                };
                MainDBCollections[typeof(DB.Admin.Workcenter)].Add("0", DefaultWC);
            }
        }
        return MainDBCollections;
    }

    private static string SaveCollection(Type CollectionType, IList CollectionToSave)
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
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
    }

    public static void SaveCollection(String DBLocation, Type CollectionType, Dictionary<String, Base> CollectionToSave)
    {        
        IList col = CollectionToSave.Values.ToList();
        string json = SaveCollection(CollectionType, col);
        var DBFileName = string.Format(DBLocation + FormatFile, CollectionType.Name);
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, json);
        if (File.Exists(DBFileName))
            File.Delete(DBFileName);
        File.Move(tempFile, DBFileName);
    }

    public ICollection Collection(Type Table)
    {
        return MainDBCollections[Table].Values;
    }

    // Note: This is here so that children can implement
    public void PopulateDerivedFields(String DBLocation, ref Dictionary<Type, Dictionary<String, Base>> MainDB) { }

}
    


