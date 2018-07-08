using System;

namespace DallarBot.Classes
{
    public class CostAttribute : Attribute
    {
        protected float Cost;

        public CostAttribute()
        {
            Cost = 1.0f;
        }

        public CostAttribute(float Cost)
        {
            this.Cost = Cost;
        }

        public float GetCost()
        {
            return Cost;
        }
    }
}
