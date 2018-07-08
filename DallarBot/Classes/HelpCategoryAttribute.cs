using System;

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
    }
}
