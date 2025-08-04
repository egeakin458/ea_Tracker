using System;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Default implementation of <see cref="IInvestigatorFactory"/> using DI to resolve types.
    /// </summary>
    public class InvestigatorFactory : IInvestigatorFactory
    {
        private readonly IServiceProvider _provider;

        public InvestigatorFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Investigator Create(string kind)
        {
            return kind.ToLowerInvariant() switch
            {
                "invoice" => (Investigator)_provider.GetRequiredService(typeof(InvoiceInvestigator)),
                "waybill" => (Investigator)_provider.GetRequiredService(typeof(WaybillInvestigator)),
                _ => throw new ArgumentException($"Unknown investigator kind: {kind}", nameof(kind)),
            };
        }
    }
}
