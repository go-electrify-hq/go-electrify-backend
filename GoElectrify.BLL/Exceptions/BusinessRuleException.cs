using System;

namespace GoElectrify.BLL.Exceptions
{
    public sealed class BusinessRuleException : Exception
    {
        public string Code { get; }

        public BusinessRuleException(string code, string message) : base(message)
        {
            Code = code;
        }

        public static BusinessRuleException WalletInsufficient(decimal missing)
            => new("WALLET_INSUFFICIENT",
                   $"Số dư ví không đủ. Cần thêm {missing:N0} VND.");

        public static BusinessRuleException SubscriptionInsufficient(decimal needKwh, decimal availKwh)
            => new("SUBSCRIPTION_INSUFFICIENT",
                   $"Gói không đủ kWh. Cần {needKwh:F2} kWh, còn {availKwh:F2} kWh.");

        public static BusinessRuleException BothInsufficient(decimal missing, decimal needKwh, decimal availKwh)
            => new("BOTH_INSUFFICIENT",
                   $"Ví và gói đều không đủ. Cần thêm {missing:N0} VND hoặc {needKwh:F2} kWh (hiện còn {availKwh:F2} kWh).");
    }
}

