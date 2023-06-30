using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace VendorTest
{
    public static class WebHost
    {
        private static Type _CurrentType;

        public static Type CurrentType
        {
            get
            {
                return _CurrentType;
            }
            set
            {
                _CurrentType = value;
                CurrentPage = new ConcurrentDictionary<int, int>();
                var CurrentProperties = new List<PropertyInfo>();
                CurrentProperties.Add(Base.GetPrimary(CurrentType));
                foreach (var prop in CurrentType.GetProperties())
                    if (!CurrentProperties.Contains(prop)) CurrentProperties.Add(prop);
                CurrentTypeProperties = CurrentProperties.ToArray();
                CurrentTypePrimaryKey = Base.GetPrimaryKey(CurrentType);
            }
        }


        public static PropertyInfo[] CurrentTypeProperties { get; private set; }

        public static String CurrentTypePrimaryKey { get; private set; }

        public static Base[] GetRows(Microsoft.AspNetCore.Http.HttpRequest httpRequest)
        {
            Base[] rows;
            System.Type newType = null;
            if (httpRequest.Query.ContainsKey("table"))
                newType = WebHost.GetTypeFromCollection(httpRequest.Query["table"].ToString());

            if (httpRequest.Query.ContainsKey("key") && newType != null)
            {
                rows = QueryForReferencedItem(WebHost.CurrentType, WebHost.GetTypeFromCollection(httpRequest.Query["table"].ToString()), httpRequest.Query["key"]).ToArray();
                //if (rows.Count() > 0) 
                    WebHost.CurrentType = newType;
            }
            else
            {
                if (newType == null) return null;
                WebHost.CurrentType = newType;
                if (WebHost.CurrentType == null || WebHost.CurrentTypePrimaryKey == null) return null;
                rows = Program.MainDBCollections[WebHost.CurrentType].Values.Cast<Base>().Skip(WebHost.CurrentPage.GetOrAdd(0, 0) * 10).Take(10).ToArray();
            }
            return rows;
        }
        public static void HandleFormCollection(Microsoft.AspNetCore.Http.IFormCollection fm)
        {
            // Navigation
            var nextkey = fm.Keys.FirstOrDefault(a => a.StartsWith("_Next", StringComparison.InvariantCultureIgnoreCase));
            if (nextkey != null && nextkey.Split('_').Length > 2 && int.TryParse(nextkey.Split('_')[2], out var tablenumber))
            {
                CurrentPage[tablenumber] =+ 1;
                return;
            }
            var prevkey = fm.Keys.FirstOrDefault(a => a.StartsWith("_Previous", StringComparison.InvariantCultureIgnoreCase));
            if (prevkey != null && prevkey.Split('_').Length > 2 && int.TryParse(prevkey.Split('_')[2], out var tablenumber2))
            {
                CurrentPage[tablenumber2] =- 1;
                return;
            }

            if (fm.ContainsKey("table"))
            {
                String table = fm["table"];
                CurrentType = GetTypeFromCollection(table);
                return;
            }

            if (fm.ContainsKey("id") || fm.ContainsKey("_Add") || fm.ContainsKey("_Save"))
            {
                BindFormCollectionToDbCollection(fm);
                return;
            }
            return;
        }

        #region "Private Methods"

        private static Type GetTypeFromCollection(String typeAsString)
        {
            return Program.MainDBCollections.Keys.FirstOrDefault(a => a.FullName == typeAsString);
        }
        
        private static ConcurrentDictionary<int, int> CurrentPage { get; set; } = new ConcurrentDictionary<int, int>();
         
        private static IEnumerable<Base> QueryForReferencedItem(System.Type currentType, System.Type ForeignKeyType, String ForeignKeyNumber)
        {
            List<Base> output = new List<Base>();
            System.Reflection.PropertyInfo foundprop = FindProp(currentType, ForeignKeyType);
            if (foundprop == null) return output;
            foreach (var item in Program.MainDBCollections[ForeignKeyType].Values)
            {
                var foundvalue = foundprop.GetValue(item);
                if (foundvalue != null && foundvalue.ToString() == ForeignKeyNumber)
                    output.Add(item);
            }

            return output;
        }

        private static System.Reflection.PropertyInfo FindProp(System.Type currentType, System.Type ForeignKeyType)
        {
            foreach (var tableprop in ForeignKeyType.GetProperties())
            {
                if (Base.GetAttribute(tableprop, typeof(Base.ForeignKey), out var foreignkey))
                    if (((Base.ForeignKey)foreignkey).GetName().FullName == currentType.FullName)
                        return tableprop;
            }
            return null;
        }

        private static void BindFormCollectionToDbCollection(Microsoft.AspNetCore.Http.IFormCollection fm)
        {
            if (fm == null) return;

            if (fm.ContainsKey("_Add") && fm.ContainsKey("_Add_Type"))
            {
                Type addtype = Assembly.Load("Core").GetType(fm["_Add_Type"].ToString());
                Base.AddtoDBCollection(addtype, Program.MainDBCollections[addtype]);
                Base.SaveCollection(Program.DBLocation, addtype, Program.MainDBCollections[addtype]);
                return;
            }

            foreach (Type thistype in Program.MainDBCollections.Keys)
            {
                for (int i = 0; i < fm["type"].Count; i++)
                {
                    if (thistype.FullName != fm["type"][i]) continue;
                    if (fm.ContainsKey(i + "_Delete"))
                    {
                        Program.MainDBCollections[thistype].Remove(fm["id"][i]);
                        Base.SaveCollection(Program.DBLocation, thistype, Program.MainDBCollections[thistype]);
                        break;
                    }
                    else
                    {
                        Base item = Program.MainDBCollections[thistype][fm["id"][i]];

                        foreach (var prop in thistype.GetProperties())
                        {
                            if (Base.GetAttribute(prop, typeof(Base.PrimaryKey))) continue;
                            if (Base.GetAttribute(prop, typeof(JsonIgnoreAttribute))) continue;
                            if (Base.GetAttribute(prop, typeof(Base.ReadOnly))) continue;
                            if (fm.ContainsKey(i + "_" + prop.Name))
                                item.SetProperty(prop, fm[i + "_" + prop.Name]);
                            else if (prop.PropertyType == typeof(bool))
                                item.SetProperty(prop, "false");
                            else
                                item.SetProperty(prop, null);
                        }
                    }
                    Base.SaveCollection(Program.DBLocation, thistype, Program.MainDBCollections[thistype]);
                }
            }
        }

        #endregion
    }
}
