using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DallarBot.Classes
{
    public class CategoryAttribute : Attribute
    {
        public string Category;

        public CategoryAttribute(string Category)
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
                if (attr is CategoryAttribute)
                {
                    return ((CategoryAttribute)attr).GetCategory();
                }
            }

            return null;
        }
    }
}
