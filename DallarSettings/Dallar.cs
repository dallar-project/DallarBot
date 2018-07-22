using System;
using System.Collections.Generic;
using System.Text;

namespace Dallar
{
    public class DallarAccount
    {
        public string AccountPrefix { get; set; }
        public string AccountId { get; set; }
        public string UniqueAccountName { get { return AccountPrefix + AccountId; } }
        public string KnownAddress { get { return knownAddress; } set { knownAddress = value; IsAddressKnown = true; } }
        public bool IsAddressKnown { get; internal set; }

        protected string knownAddress;
    }

    public interface IDallarAccountOverrider
    {
        bool OverrideDallarAccountIfNeeded(ref DallarAccount Account);
    }
}
