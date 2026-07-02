using System;
using Statlyn.Core.Diagnostics;

namespace Statlyn.Core
{
    public sealed class SnapshotResult<T>
    {
        private SnapshotResult(bool success, T? value, DiagnosticReport diagnostics, string message)
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

        public static SnapshotResult<T> FromSuccess(T value, DiagnosticReport diagnostics)
        {
            return new SnapshotResult<T>(true, value, diagnostics, string.Empty);
        }

        public static SnapshotResult<T> FromFailure(string message, DiagnosticReport diagnostics)
        {
            return new SnapshotResult<T>(false, default(T), diagnostics, message);
        }
    }
}
