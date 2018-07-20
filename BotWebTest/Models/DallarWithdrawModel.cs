using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotWebTest.Models
{
    public class DallarWithdrawModel
    {
        public string DallarAddress { get; set; }
        public decimal Amount { get; set; }
    }

    public class DallarWithdrawResultModel
    {
        public bool bSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string DallarAddress { get; set; }
        public decimal Amount { get; set; }
    }
}
