using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Exceptions
{
    public sealed class OtpLockedException : Exception
    {
        public int? RetryAfterSeconds { get; }
        public OtpLockedException(int? retryAfterSeconds = null)
            : base("OTP is locked. Try again later.") => RetryAfterSeconds = retryAfterSeconds;

        public OtpLockedException(string message, int? retryAfterSeconds = null)
            : base(message) => RetryAfterSeconds = retryAfterSeconds;

        public OtpLockedException(string message, Exception? inner, int? retryAfterSeconds = null)
            : base(message, inner) => RetryAfterSeconds = retryAfterSeconds;
    }
}
