using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace VendorTest.Pages
{
    public class IndexModel : PageModel
    {
        public static List<System.Type> ReferencedTables(System.Type currentType)
        {
            List<System.Type> typelist = new List<System.Type>();

            foreach (Type table in Program.MainDBCollections.Keys)
                foreach (PropertyInfo tableprop in GetProperties(table))
                    if (Base.GetAttribute(tableprop, typeof(Base.ForeignKey), out var foreignkey))
                        if (((Base.ForeignKey)foreignkey).GetName() == currentType && !typelist.Contains(table))
                            typelist.Add(table);
            return typelist;
        }

        [UnconditionalSuppressMessage("Don't worry about it", "IL2070")]
        public static IEnumerable<PropertyInfo> GetProperties(Type entityType)
        {
            IEnumerable<PropertyInfo> result = entityType.GetProperties();
            return result;
        }

        public static bool IsPrimaryKey(PropertyInfo prop)
        {
            return WebHost.CurrentTypePrimaryKey == prop.Name;
        }

        public static bool PropertyNotVisible(PropertyInfo prop)
        {
            return Base.GetDisplayProperty(prop, typeof(Base.DisplayWidth), "") == "0";
        }

        public static string GetLabel(PropertyInfo prop)
        {
            return Base.GetDisplayProperty(prop, typeof(Base.Label), prop.Name);
        }
    }
}
