using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VendorTest.Pages
{
    public class IndexModel : PageModel
    {
        public static List<System.Type> ReferencedTables(System.Type currentType)
        {
            List<System.Type> typelist = new List<System.Type>();

            foreach (var table in Program.MainDBCollections.Keys)
                foreach (var tableprop in table.GetProperties())
                    if (Base.GetAttribute(tableprop, typeof(Base.ForeignKey), out var foreignkey))
                        if (((Base.ForeignKey)foreignkey).GetName() == currentType && !typelist.Contains(table))
                            typelist.Add(table);
            return typelist;
        }

        
    }
}
