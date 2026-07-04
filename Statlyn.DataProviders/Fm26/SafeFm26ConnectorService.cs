using System;

namespace Statlyn.DataProviders.Fm26
{
    public sealed class SafeFm26ConnectorService
    {
        private readonly IFm26NativeConnector _connector;

        public SafeFm26ConnectorService(IFm26NativeConnector connector)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        }

        public Fm26ConnectorDiagnostic GetDiagnostic()
        {
            return _connector.GetDiagnostic();
        }

        public Fm26ProcessDiagnostic DetectFmProcess()
        {
            return _connector.DetectFmProcess();
        }
    }
}
