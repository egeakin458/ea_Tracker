using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Enhanced factory interface for creating investigator instances.
    /// Supports configuration injection and type-safe registration.
    /// Implements Factory Pattern with Strategy Pattern registration.
    /// </summary>
    public interface IInvestigatorFactory
    {
        /// <summary>
        /// Creates a new Investigator of the specified type.
        /// </summary>
        /// <param name="kind">The investigator kind (e.g. "invoice", "waybill").</param>
        /// <returns>A fresh Investigator instance.</returns>
        Investigator Create(string kind);

        /// <summary>
        /// Creates a new Investigator with custom configuration.
        /// Configuration overrides default settings from database/appsettings.
        /// </summary>
        /// <param name="kind">The investigator kind.</param>
        /// <param name="customConfiguration">Custom JSON configuration to apply.</param>
        /// <returns>A configured Investigator instance.</returns>
        Investigator Create(string kind, string? customConfiguration);

        /// <summary>
        /// Creates a new Investigator from an InvestigatorInstance database entity.
        /// Automatically applies custom configuration if present.
        /// </summary>
        /// <param name="investigatorInstance">The investigator instance configuration from database.</param>
        /// <returns>A configured Investigator instance.</returns>
        Investigator Create(InvestigatorInstance investigatorInstance);

        /// <summary>
        /// Gets all supported investigator types that can be created.
        /// Useful for UI dropdowns and validation.
        /// </summary>
        /// <returns>Collection of supported investigator type codes.</returns>
        IEnumerable<string> GetSupportedTypes();

        /// <summary>
        /// Checks if the factory supports creating investigators of the specified type.
        /// </summary>
        /// <param name="kind">The investigator kind to check.</param>
        /// <returns>True if the type is supported, false otherwise.</returns>
        bool IsTypeSupported(string kind);
    }

    /// <summary>
    /// Enhanced factory implementation with registration-based strategy pattern.
    /// Eliminates hardcoded switch statements and enables dynamic registration.
    /// </summary>
    public interface IInvestigatorRegistry
    {
        /// <summary>
        /// Registers a factory function for a specific investigator type.
        /// </summary>
        /// <typeparam name="TInvestigator">The investigator type to register.</typeparam>
        /// <param name="typeCode">The string identifier for this investigator type.</param>
        /// <param name="factory">Factory function that creates instances.</param>
        void Register<TInvestigator>(string typeCode, Func<IServiceProvider, TInvestigator> factory) 
            where TInvestigator : Investigator;

        /// <summary>
        /// Gets the factory function for a specific type.
        /// </summary>
        /// <param name="typeCode">The investigator type code.</param>
        /// <returns>Factory function if registered, null otherwise.</returns>
        Func<IServiceProvider, Investigator>? GetFactory(string typeCode);

        /// <summary>
        /// Gets all registered type codes.
        /// </summary>
        IEnumerable<string> GetRegisteredTypes();
    }
}
