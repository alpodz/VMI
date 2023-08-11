using Interfaces;
using System.Collections;
using System.Reflection;

public class Base : IBase
{
    private IDBObject? _DBLocation;
    private Dictionary<Type, Dictionary<String, IBase>>? _MainDBCollections;
    public static IQueueService? _SendOrderService;
    public static IQueueService? _AdjInventoryService;

    public bool IsNew = true;
    public bool IsDirty = false;
    public bool IsDeleted = false;

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
    public class DisplayProperty : System.Attribute
    {
        public String _value;

        public String GetValue()
        {
            return _value;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class PartitionKey : System.Attribute
    {
        public String _value;

        public String GetValue()
        {
            return _value;
        }
    }

    public static String Icon = "";

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class TabIcon : System.Attribute
    {
        public TabIcon(String icon)
        {
            Icon = icon;
        }
    }

    public class DefaultValue : DisplayProperty
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

    #region Property Related
    public static PropertyInfo? GetKey(Type item, String TypeOfKey)
    {
        if (item == null || String.IsNullOrEmpty(TypeOfKey)) return null;
        foreach (var prop in item.GetProperties())
            foreach (var ca in prop.CustomAttributes)
                if (ca.AttributeType.Name == TypeOfKey)
                    return prop;
        return null;
    }

    public static bool GetAttribute(PropertyInfo prop, Type attrib, out Object? output)
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

    public static string? GetPrimaryKey(Type item)
    {
        var KeyProperty = GetPrimary(item);
        if (KeyProperty == null) return null;
        return KeyProperty.Name;
    }
    public static PropertyInfo? GetPrimary(Type item)
    {
        return GetKey(item, "PrimaryKey");
    }

    public string? GetPrimaryKeyValue()
    {
        return GetPrimary(this.GetType()).GetValue(this).ToString();
    }

    public void SetProperty(PropertyInfo propertyinfo, string PropertyValue)
    {
        if (propertyinfo == null) return;
        if (propertyinfo.GetValue(this) == null && PropertyValue == null) return;
        if (propertyinfo.PropertyType == typeof(string) && propertyinfo.GetValue(this) == null && PropertyValue == string.Empty) return;
        if ((propertyinfo.GetValue(this) == null && PropertyValue != null) || propertyinfo.GetValue(this).ToString() != PropertyValue)
        {
            if (PropertyValue == null) propertyinfo.SetValue(this, null);
            else if (propertyinfo.PropertyType == typeof(decimal)) propertyinfo.SetValue(this, Convert.ToDecimal(PropertyValue));
            else if (propertyinfo.PropertyType == typeof(Int32)) propertyinfo.SetValue(this, Convert.ToInt32(PropertyValue));
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
            else if (propertyinfo.PropertyType == typeof(String)) propertyinfo.SetValue(this, Convert.ToString(PropertyValue));
            else if (propertyinfo.PropertyType == typeof(bool)) propertyinfo.SetValue(this, Convert.ToBoolean(PropertyValue));
            else propertyinfo.SetValue(this, PropertyValue);
            IsDirty = true;
        }
    }

    public static void AddtoDBCollection(Type item, Dictionary<String, IBase> instance)
    {
        Base newi = (Base)Activator.CreateInstance(item);
        if (newi == null) return;
        var guid = Guid.NewGuid().ToString();
        newi.IsDirty = true;
        item.GetRuntimeProperty(GetPrimaryKey(item)).SetValue(newi, guid);
        instance.Add(guid, newi);
    }

    #endregion

    public async static Task<Dictionary<String, IBase>?> PopulateDictionary(IDBObject DBLocation, Type item)
    {
        var col = await DBLocation.PopulateCollectionAsync(item);
        return col.Cast<IBase>().ToDictionary(a => a.GetPrimaryKeyValue());
    }

    public async static Task<Dictionary<Type, Dictionary<String, IBase>>> PopulateMainCollection(IDBObject DBLocation)
    {
        var DBClassObjects = Assembly.Load("Core").GetTypes().Where(t => t.IsSubclassOf(typeof(Base)));
        var MainDBCollections = new Dictionary<Type, Dictionary<String, IBase>>();

        foreach (Type item in DBClassObjects)
        {
            MainDBCollections.Add(item, await PopulateDictionary(DBLocation, item));
            // for some collections, I want to include 'None option'            
            if (item == typeof(DB.Admin.Workcenter) && !MainDBCollections[typeof(DB.Admin.Workcenter)].ContainsKey("0"))
            {
                var DefaultWC = new DB.Admin.Workcenter
                {
                    id = "0",
                    Name = "Shipment From Vendor"
                };
                MainDBCollections[typeof(DB.Admin.Workcenter)].Add("0", DefaultWC);
            }
        }
        return MainDBCollections;
    }

    public async static Task SaveDictionary(IDBObject DBLocation, Type CollectionType, Dictionary<String, IBase> CollectionToSave)
    {
        IList col = CollectionToSave.Values.ToList();
        await DBLocation.SaveCollectionAsync(CollectionType, col);
    }

    //public static async Task SaveObject(IDBObject DBLocation, Type ObjectType, IBase Obj)
    //{
    //    await DBLocation.SaveObject( ObjectType, Obj, false);
    //}

    //public ICollection Collection(Type Table)
    //{
    //    return MainDBCollections[Table].Values;
    //}

    // Note: This is here so that children can implement
    public void PopulateDerivedFields(IDBObject DBLocation, ref Dictionary<Type, Dictionary<String, IBase>> MainDB) { }
    [DisplayWidth(0)]
    public string Partition { get { return $"_{this.GetType().Name}_{Base.GetKey(this.GetType(), "PartitionKey").GetValue(this)}"; } }

    Dictionary<Type, Dictionary<string, IBase>> IBase.MainDBCollections { get => _MainDBCollections; set => _MainDBCollections = value; }
    IDBObject IBase.DBLocation { get => _DBLocation; set => _DBLocation = value; }

    IQueueService IBase.AdjInventoryService { get => _AdjInventoryService; set => _AdjInventoryService = value; }
    IQueueService IBase.SendOrderService { get => _SendOrderService; set => _SendOrderService = value; }

    public void MarkDeleted() { IsDeleted = true; }
    public void MarkOld() { IsNew = false; }
}


