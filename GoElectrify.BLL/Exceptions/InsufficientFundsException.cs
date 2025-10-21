using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Exceptions
{
    public sealed class InsufficientFundsException : Exception
    {
        public decimal Need { get; }
        public decimal Balance { get; }
        public InsufficientFundsException(decimal need, decimal balance)
            : base("Insufficient wallet balance.")
        {
            Need = need;
            Balance = balance;
        }
    }
}
