using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Exceptions
{
    public sealed class OtpRateLimitedException : Exception
    {
        public int RetryAfterSeconds { get; }
        public OtpRateLimitedException(int retryAfterSeconds)
            : base("Too many OTP requests. Try again later.") => RetryAfterSeconds = retryAfterSeconds;

        public OtpRateLimitedException(string message, int retryAfterSeconds)
            : base(message) => RetryAfterSeconds = retryAfterSeconds;

        public OtpRateLimitedException(string message, Exception? inner, int retryAfterSeconds)
            : base(message, inner) => RetryAfterSeconds = retryAfterSeconds;
    }
}
