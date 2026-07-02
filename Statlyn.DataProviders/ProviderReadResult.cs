using System;
using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders
{
    public sealed class ProviderReadResult<T>
    {
        private ProviderReadResult(bool success, T? value, DiagnosticReport diagnostics, string message)
        {
            Success = success;
            Value = value;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            Message = message ?? string.Empty;
        }

        public bool Success { get; }

        public T? Value { get; }

        public DiagnosticReport Diagnostics { get; }

        public string Message { get; }

        public static ProviderReadResult<T> FromSuccess(T value, DiagnosticReport diagnostics)
        {
            return new ProviderReadResult<T>(true, value, diagnostics, string.Empty);
        }

        public static ProviderReadResult<T> FromFailure(string message, DiagnosticReport diagnostics)
        {
            return new ProviderReadResult<T>(false, default(T), diagnostics, message);
        }
    }
}
