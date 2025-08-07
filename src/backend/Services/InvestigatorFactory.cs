using System;
using System.Collections.Concurrent;
using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Registry implementation for investigator factory functions.
    /// Thread-safe registration and lookup of investigator types.
    /// </summary>
    public class InvestigatorRegistry : IInvestigatorRegistry
    {
        private readonly ConcurrentDictionary<string, Func<IServiceProvider, Investigator>> _factories = new();

        public void Register<TInvestigator>(string typeCode, Func<IServiceProvider, TInvestigator> factory) 
            where TInvestigator : Investigator
        {
            var normalizedTypeCode = typeCode.ToLowerInvariant();
            _factories[normalizedTypeCode] = serviceProvider => factory(serviceProvider);
        }

        public Func<IServiceProvider, Investigator>? GetFactory(string typeCode)
        {
            var normalizedTypeCode = typeCode.ToLowerInvariant();
            return _factories.TryGetValue(normalizedTypeCode, out var factory) ? factory : null;
        }

        public IEnumerable<string> GetRegisteredTypes()
        {
            return _factories.Keys.ToList();
        }
    }

    /// <summary>
    /// Enhanced implementation of <see cref="IInvestigatorFactory"/> with registration-based strategy pattern.
    /// Eliminates hardcoded switch statements and supports configuration injection.
    /// </summary>
    public class InvestigatorFactory : IInvestigatorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IInvestigatorRegistry _registry;
        private readonly IInvestigationConfiguration _defaultConfiguration;

        public InvestigatorFactory(
            IServiceProvider serviceProvider, 
            IInvestigatorRegistry registry,
            IInvestigationConfiguration defaultConfiguration)
        {
            _serviceProvider = serviceProvider;
            _registry = registry;
            _defaultConfiguration = defaultConfiguration;
        }

        public Investigator Create(string kind)
        {
            return Create(kind, customConfiguration: null);
        }

        public Investigator Create(string kind, string? customConfiguration)
        {
            var factory = _registry.GetFactory(kind);
            if (factory == null)
            {
                throw new ArgumentException($"Unknown investigator kind: {kind}. Supported types: {string.Join(", ", GetSupportedTypes())}", nameof(kind));
            }

            var investigator = factory(_serviceProvider);
            
            // Apply configuration if provided
            if (!string.IsNullOrWhiteSpace(customConfiguration))
            {
                ApplyCustomConfiguration(investigator, customConfiguration);
            }

            return investigator;
        }

        public Investigator Create(InvestigatorInstance investigatorInstance)
        {
            if (investigatorInstance?.Type?.Code == null)
            {
                throw new ArgumentException("InvestigatorInstance must have a valid Type with Code", nameof(investigatorInstance));
            }

            return Create(investigatorInstance.Type.Code, investigatorInstance.CustomConfiguration);
        }

        public IEnumerable<string> GetSupportedTypes()
        {
            return _registry.GetRegisteredTypes();
        }

        public bool IsTypeSupported(string kind)
        {
            return _registry.GetFactory(kind) != null;
        }

        /// <summary>
        /// Applies custom JSON configuration to an investigator.
        /// This is where investigator-specific configuration logic would be implemented.
        /// </summary>
        private void ApplyCustomConfiguration(Investigator investigator, string customConfiguration)
        {
            // For now, this is a placeholder for configuration application
            // In a full implementation, this would parse the JSON and apply
            // configuration-specific settings to the investigator instance
            
            // Example: Parse JSON and set properties on investigator
            // var config = JsonSerializer.Deserialize<Dictionary<string, object>>(customConfiguration);
            // investigator.ApplyConfiguration(config);
            
            // For the current phase, we'll leave this as a placeholder
            // since the current investigators don't have configuration properties
        }
    }

    /// <summary>
    /// Extension methods for convenient investigator registration.
    /// Provides fluent API for factory configuration.
    /// </summary>
    public static class InvestigatorRegistryExtensions
    {
        /// <summary>
        /// Registers the standard investigator types (Invoice and Waybill).
        /// Called during application startup to configure the factory.
        /// </summary>
        public static IInvestigatorRegistry RegisterStandardTypes(this IInvestigatorRegistry registry, IServiceProvider serviceProvider)
        {
            // Register Invoice Investigator
            registry.Register<InvoiceInvestigator>("invoice", sp => sp.GetRequiredService<InvoiceInvestigator>());
            
            // Register Waybill Investigator  
            registry.Register<WaybillInvestigator>("waybill", sp => sp.GetRequiredService<WaybillInvestigator>());
            
            return registry;
        }

        /// <summary>
        /// Registers a custom investigator type.
        /// Enables extensibility for future investigator types.
        /// </summary>
        public static IInvestigatorRegistry RegisterCustomType<TInvestigator>(
            this IInvestigatorRegistry registry, 
            string typeCode) 
            where TInvestigator : Investigator
        {
            registry.Register<TInvestigator>(typeCode, sp => sp.GetRequiredService<TInvestigator>());
            return registry;
        }
    }
}
