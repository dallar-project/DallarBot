using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DallarBot.Classes
{
    public class HelpCategoryAttribute : Attribute
    {
        public string Category;

        public HelpCategoryAttribute()
        {
            Category = "Uncategorized";
        }

        public HelpCategoryAttribute(string Category)
        {
            this.Category = Category;
        }

        public string GetCategory()
        {
            return Category;
        }

        public static string GetCategory(System.Type t)
        {
            // Using reflection.  
            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(t);  // Reflection.  

            // Displaying output.  
            foreach (System.Attribute attr in attrs)
            {
                if (attr is HelpCategoryAttribute)
                {
                    return ((HelpCategoryAttribute)attr).GetCategory();
                }
            }

            return null;
        }
    }
}
