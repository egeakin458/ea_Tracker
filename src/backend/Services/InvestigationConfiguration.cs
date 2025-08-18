using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Strongly-typed configuration options for invoice investigation settings.
    /// Provides validation and strongly-typed access to configuration values.
    /// </summary>
    public class InvoiceInvestigationOptions
    {
        /// <summary>
        /// Configuration section name for invoice investigation settings.
        /// </summary>
        public const string SectionName = "Investigation:Invoice";

        /// <summary>
        /// Maximum tax ratio allowed (e.g., 0.5 = 50%).
        /// </summary>
        [Range(0.0, 2.0, ErrorMessage = "MaxTaxRatio must be between 0.0 and 2.0")]
        public decimal MaxTaxRatio { get; set; } = 0.5m;

        /// <summary>
        /// Whether to check for negative amounts.
        /// </summary>
        public bool CheckNegativeAmounts { get; set; } = true;

        /// <summary>
        /// Whether to check for future dates.
        /// </summary>
        public bool CheckFutureDates { get; set; } = true;

        /// <summary>
        /// Maximum allowed days in the future for invoice dates.
        /// </summary>
        [Range(0, 365, ErrorMessage = "MaxFutureDays must be between 0 and 365")]
        public int MaxFutureDays { get; set; } = 0;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>Validation results.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, context, results, validateAllProperties: true);
            return results;
        }
    }

    /// <summary>
    /// Strongly-typed configuration options for waybill investigation settings.
    /// Provides validation and strongly-typed access to configuration values.
    /// </summary>
    public class WaybillInvestigationOptions
    {
        /// <summary>
        /// Configuration section name for waybill investigation settings.
        /// </summary>
        public const string SectionName = "Investigation:Waybill";

        /// <summary>
        /// Hours threshold for "expiring soon" detection.
        /// </summary>
        [Range(1, 168, ErrorMessage = "ExpiringSoonHours must be between 1 and 168 (7 days)")]
        public int ExpiringSoonHours { get; set; } = 24;

        /// <summary>
        /// Days threshold for legacy waybill cutoff (waybills without due dates).
        /// </summary>
        [Range(1, 365, ErrorMessage = "LegacyCutoffDays must be between 1 and 365")]
        public int LegacyCutoffDays { get; set; } = 7;

        /// <summary>
        /// Whether to check for overdue deliveries (past due date).
        /// </summary>
        public bool CheckOverdueDeliveries { get; set; } = true;

        /// <summary>
        /// Whether to check for deliveries expiring soon.
        /// </summary>
        public bool CheckExpiringSoon { get; set; } = true;

        /// <summary>
        /// Whether to check legacy waybills (those without due dates).
        /// </summary>
        public bool CheckLegacyWaybills { get; set; } = true;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>Validation results.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, context, results, validateAllProperties: true);
            return results;
        }
    }

    /// <summary>
    /// Strongly-typed configuration options for general investigation settings.
    /// Provides cross-cutting configuration that applies to all investigation types.
    /// </summary>
    public class GeneralInvestigationOptions
    {
        /// <summary>
        /// Configuration section name for general investigation settings.
        /// </summary>
        public const string SectionName = "Investigation:General";

        /// <summary>
        /// Default investigation cooldown period in hours.
        /// </summary>
        [Range(0.1, 168, ErrorMessage = "InvestigationCooldownHours must be between 0.1 and 168")]
        public double InvestigationCooldownHours { get; set; } = 1.0;

        /// <summary>
        /// Maximum days without investigation before marking as priority.
        /// </summary>
        [Range(1, 365, ErrorMessage = "MaxDaysWithoutInvestigation must be between 1 and 365")]
        public int MaxDaysWithoutInvestigation { get; set; } = 7;

        /// <summary>
        /// Whether to enable automatic investigation scheduling.
        /// </summary>
        public bool EnableAutoScheduling { get; set; } = true;

        /// <summary>
        /// Maximum concurrent investigations allowed.
        /// </summary>
        [Range(1, 50, ErrorMessage = "MaxConcurrentInvestigations must be between 1 and 50")]
        public int MaxConcurrentInvestigations { get; set; } = 5;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>Validation results.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            var context = new ValidationContext(this);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, context, results, validateAllProperties: true);
            return results;
        }
    }

    /// <summary>
    /// Configuration migration utilities for updating existing configurations.
    /// Provides backward compatibility and smooth configuration transitions.
    /// </summary>
    public static class ConfigurationMigrationUtilities
    {
        /// <summary>
        /// Migrates legacy configuration format to strongly-typed options.
        /// </summary>
        /// <param name="legacyJsonConfiguration">Legacy JSON configuration string.</param>
        /// <returns>Tuple of migrated configuration options.</returns>
        public static (InvoiceInvestigationOptions Invoice, WaybillInvestigationOptions Waybill, GeneralInvestigationOptions General) 
            MigrateLegacyConfiguration(string? legacyJsonConfiguration)
        {
            var invoiceOptions = new InvoiceInvestigationOptions();
            var waybillOptions = new WaybillInvestigationOptions();
            var generalOptions = new GeneralInvestigationOptions();

            if (string.IsNullOrWhiteSpace(legacyJsonConfiguration))
                return (invoiceOptions, waybillOptions, generalOptions);

            try
            {
                var jsonDoc = JsonDocument.Parse(legacyJsonConfiguration);
                var root = jsonDoc.RootElement;

                // Migrate invoice configuration
                if (root.TryGetProperty("Invoice", out var invoiceElement) || 
                    root.TryGetProperty("invoice", out invoiceElement))
                {
                    MigrateInvoiceConfiguration(invoiceElement, invoiceOptions);
                }

                // Migrate waybill configuration
                if (root.TryGetProperty("Waybill", out var waybillElement) || 
                    root.TryGetProperty("waybill", out waybillElement))
                {
                    MigrateWaybillConfiguration(waybillElement, waybillOptions);
                }

                // Migrate general configuration
                if (root.TryGetProperty("General", out var generalElement) || 
                    root.TryGetProperty("general", out generalElement))
                {
                    MigrateGeneralConfiguration(generalElement, generalOptions);
                }
            }
            catch (JsonException)
            {
                // If migration fails, use default values
                // Log warning in production scenarios
            }

            return (invoiceOptions, waybillOptions, generalOptions);
        }

        /// <summary>
        /// Validates a complete configuration set and returns validation errors.
        /// </summary>
        /// <param name="invoiceOptions">Invoice configuration options.</param>
        /// <param name="waybillOptions">Waybill configuration options.</param>
        /// <param name="generalOptions">General configuration options.</param>
        /// <returns>Collection of validation errors, empty if valid.</returns>
        public static IEnumerable<string> ValidateConfiguration(
            InvoiceInvestigationOptions invoiceOptions,
            WaybillInvestigationOptions waybillOptions,
            GeneralInvestigationOptions generalOptions)
        {
            var errors = new List<string>();

            errors.AddRange(invoiceOptions.Validate().Select(v => $"Invoice: {v.ErrorMessage}"));
            errors.AddRange(waybillOptions.Validate().Select(v => $"Waybill: {v.ErrorMessage}"));
            errors.AddRange(generalOptions.Validate().Select(v => $"General: {v.ErrorMessage}"));

            return errors;
        }

        private static void MigrateInvoiceConfiguration(JsonElement element, InvoiceInvestigationOptions options)
        {
            if (element.TryGetProperty("MaxTaxRatio", out var maxTaxRatio) && maxTaxRatio.ValueKind == JsonValueKind.Number)
                options.MaxTaxRatio = maxTaxRatio.GetDecimal();

            if (element.TryGetProperty("CheckNegativeAmounts", out var checkNegative) && checkNegative.ValueKind == JsonValueKind.True || checkNegative.ValueKind == JsonValueKind.False)
                options.CheckNegativeAmounts = checkNegative.GetBoolean();

            if (element.TryGetProperty("CheckFutureDates", out var checkFuture) && checkFuture.ValueKind == JsonValueKind.True || checkFuture.ValueKind == JsonValueKind.False)
                options.CheckFutureDates = checkFuture.GetBoolean();

            if (element.TryGetProperty("MaxFutureDays", out var maxFutureDays) && maxFutureDays.ValueKind == JsonValueKind.Number)
                options.MaxFutureDays = maxFutureDays.GetInt32();
        }

        private static void MigrateWaybillConfiguration(JsonElement element, WaybillInvestigationOptions options)
        {
            if (element.TryGetProperty("ExpiringSoonHours", out var expiringSoon) && expiringSoon.ValueKind == JsonValueKind.Number)
                options.ExpiringSoonHours = expiringSoon.GetInt32();

            if (element.TryGetProperty("LegacyCutoffDays", out var legacyCutoff) && legacyCutoff.ValueKind == JsonValueKind.Number)
                options.LegacyCutoffDays = legacyCutoff.GetInt32();

            if (element.TryGetProperty("CheckOverdueDeliveries", out var checkOverdue) && checkOverdue.ValueKind == JsonValueKind.True || checkOverdue.ValueKind == JsonValueKind.False)
                options.CheckOverdueDeliveries = checkOverdue.GetBoolean();

            if (element.TryGetProperty("CheckExpiringSoon", out var checkExpiring) && checkExpiring.ValueKind == JsonValueKind.True || checkExpiring.ValueKind == JsonValueKind.False)
                options.CheckExpiringSoon = checkExpiring.GetBoolean();

            if (element.TryGetProperty("CheckLegacyWaybills", out var checkLegacy) && checkLegacy.ValueKind == JsonValueKind.True || checkLegacy.ValueKind == JsonValueKind.False)
                options.CheckLegacyWaybills = checkLegacy.GetBoolean();
        }

        private static void MigrateGeneralConfiguration(JsonElement element, GeneralInvestigationOptions options)
        {
            if (element.TryGetProperty("InvestigationCooldownHours", out var cooldown) && cooldown.ValueKind == JsonValueKind.Number)
                options.InvestigationCooldownHours = cooldown.GetDouble();

            if (element.TryGetProperty("MaxDaysWithoutInvestigation", out var maxDays) && maxDays.ValueKind == JsonValueKind.Number)
                options.MaxDaysWithoutInvestigation = maxDays.GetInt32();

            if (element.TryGetProperty("EnableAutoScheduling", out var autoScheduling) && autoScheduling.ValueKind == JsonValueKind.True || autoScheduling.ValueKind == JsonValueKind.False)
                options.EnableAutoScheduling = autoScheduling.GetBoolean();

            if (element.TryGetProperty("MaxConcurrentInvestigations", out var maxConcurrent) && maxConcurrent.ValueKind == JsonValueKind.Number)
                options.MaxConcurrentInvestigations = maxConcurrent.GetInt32();
        }
    }
    /// <summary>
    /// Enhanced investigation configuration that supports both legacy and strongly-typed configurations.
    /// Provides backward compatibility while enabling migration to strongly-typed options.
    /// </summary>
    public class EnhancedInvestigationConfiguration : IInvestigationConfiguration
    {
        public IInvoiceInvestigationConfig Invoice { get; }
        public IWaybillInvestigationConfig Waybill { get; }
        
        // Strongly-typed options for new configuration approach
        public InvoiceInvestigationOptions InvoiceOptions { get; }
        public WaybillInvestigationOptions WaybillOptions { get; }
        public GeneralInvestigationOptions GeneralOptions { get; }

        public EnhancedInvestigationConfiguration(IConfiguration configuration)
        {
            // Legacy interface implementations for backward compatibility
            Invoice = new InvoiceInvestigationConfig(configuration);
            Waybill = new WaybillInvestigationConfig(configuration);
            
            // Strongly-typed options
            InvoiceOptions = new InvoiceInvestigationOptions();
            WaybillOptions = new WaybillInvestigationOptions();
            GeneralOptions = new GeneralInvestigationOptions();
            
            // Bind configuration sections to strongly-typed options
            configuration.GetSection(InvoiceInvestigationOptions.SectionName).Bind(InvoiceOptions);
            configuration.GetSection(WaybillInvestigationOptions.SectionName).Bind(WaybillOptions);
            configuration.GetSection(GeneralInvestigationOptions.SectionName).Bind(GeneralOptions);
        }

        public EnhancedInvestigationConfiguration(
            InvoiceInvestigationOptions invoiceOptions,
            WaybillInvestigationOptions waybillOptions,
            GeneralInvestigationOptions generalOptions)
        {
            InvoiceOptions = invoiceOptions;
            WaybillOptions = waybillOptions;
            GeneralOptions = generalOptions;
            
            // Create legacy interface implementations from strongly-typed options
            Invoice = new StronglyTypedInvoiceConfig(invoiceOptions);
            Waybill = new StronglyTypedWaybillConfig(waybillOptions);
        }

        /// <summary>
        /// Creates enhanced configuration from JSON string with migration support.
        /// </summary>
        /// <param name="jsonConfiguration">JSON configuration string.</param>
        /// <returns>Enhanced investigation configuration instance.</returns>
        public static EnhancedInvestigationConfiguration FromJson(string? jsonConfiguration)
        {
            var (invoiceOptions, waybillOptions, generalOptions) = 
                ConfigurationMigrationUtilities.MigrateLegacyConfiguration(jsonConfiguration);
                
            return new EnhancedInvestigationConfiguration(invoiceOptions, waybillOptions, generalOptions);
        }

        /// <summary>
        /// Validates all configuration options and returns any errors.
        /// </summary>
        /// <returns>Collection of validation errors, empty if valid.</returns>
        public IEnumerable<string> ValidateConfiguration()
        {
            return ConfigurationMigrationUtilities.ValidateConfiguration(InvoiceOptions, WaybillOptions, GeneralOptions);
        }
    }

    /// <summary>
    /// Default implementation of investigation configuration using .NET configuration system.
    /// Supports appsettings.json, environment variables, and JSON configuration from database.
    /// </summary>
    public class InvestigationConfiguration : IInvestigationConfiguration
    {
        public IInvoiceInvestigationConfig Invoice { get; }
        public IWaybillInvestigationConfig Waybill { get; }

        public InvestigationConfiguration(IConfiguration configuration)
        {
            Invoice = new InvoiceInvestigationConfig(configuration);
            Waybill = new WaybillInvestigationConfig(configuration);
        }

        /// <summary>
        /// Creates configuration from JSON string (used for database-stored configurations).
        /// </summary>
        public static InvestigationConfiguration FromJson(string? jsonConfiguration)
        {
            if (string.IsNullOrWhiteSpace(jsonConfiguration))
            {
                return new InvestigationConfiguration(new ConfigurationBuilder().Build());
            }

            try
            {
                var jsonDoc = JsonDocument.Parse(jsonConfiguration);
                var configurationBuilder = new ConfigurationBuilder();
                
                // Add JSON as in-memory configuration source
                var configData = new Dictionary<string, string?>();
                ParseJsonToConfiguration(jsonDoc.RootElement, "", configData);
                configurationBuilder.AddInMemoryCollection(configData);
                
                return new InvestigationConfiguration(configurationBuilder.Build());
            }
            catch (JsonException)
            {
                // If JSON is invalid, fall back to defaults
                return new InvestigationConfiguration(new ConfigurationBuilder().Build());
            }
        }

        private static void ParseJsonToConfiguration(JsonElement element, string prefix, Dictionary<string, string?> configData)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    ParseJsonToConfiguration(property.Value, key, configData);
                }
                else
                {
                    configData[key] = property.Value.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Legacy interface adapter for strongly-typed invoice options.
    /// Provides backward compatibility for existing code.
    /// </summary>
    public class StronglyTypedInvoiceConfig : IInvoiceInvestigationConfig
    {
        private readonly InvoiceInvestigationOptions _options;

        public StronglyTypedInvoiceConfig(InvoiceInvestigationOptions options)
        {
            _options = options;
        }

        public decimal MaxTaxRatio => _options.MaxTaxRatio;
        public bool CheckNegativeAmounts => _options.CheckNegativeAmounts;
        public bool CheckFutureDates => _options.CheckFutureDates;
        public int MaxFutureDays => _options.MaxFutureDays;
    }

    /// <summary>
    /// Legacy interface adapter for strongly-typed waybill options.
    /// Provides backward compatibility for existing code.
    /// </summary>
    public class StronglyTypedWaybillConfig : IWaybillInvestigationConfig
    {
        private readonly WaybillInvestigationOptions _options;

        public StronglyTypedWaybillConfig(WaybillInvestigationOptions options)
        {
            _options = options;
        }

        public int ExpiringSoonHours => _options.ExpiringSoonHours;
        public int LegacyCutoffDays => _options.LegacyCutoffDays;
        public bool CheckOverdueDeliveries => _options.CheckOverdueDeliveries;
        public bool CheckExpiringSoon => _options.CheckExpiringSoon;
        public bool CheckLegacyWaybills => _options.CheckLegacyWaybills;
    }

    /// <summary>
    /// Invoice investigation configuration implementation.
    /// </summary>
    public class InvoiceInvestigationConfig : IInvoiceInvestigationConfig
    {
        private readonly IConfiguration _configuration;

        public InvoiceInvestigationConfig(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public decimal MaxTaxRatio => 
            _configuration.GetValue<decimal>("Investigation:Invoice:MaxTaxRatio", 0.5m);

        public bool CheckNegativeAmounts => 
            _configuration.GetValue<bool>("Investigation:Invoice:CheckNegativeAmounts", true);

        public bool CheckFutureDates => 
            _configuration.GetValue<bool>("Investigation:Invoice:CheckFutureDates", true);

        public int MaxFutureDays => 
            _configuration.GetValue<int>("Investigation:Invoice:MaxFutureDays", 0);
    }

    /// <summary>
    /// Waybill investigation configuration implementation.
    /// </summary>
    public class WaybillInvestigationConfig : IWaybillInvestigationConfig
    {
        private readonly IConfiguration _configuration;

        public WaybillInvestigationConfig(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int ExpiringSoonHours => 
            _configuration.GetValue<int>("Investigation:Waybill:ExpiringSoonHours", 24);

        public int LegacyCutoffDays => 
            _configuration.GetValue<int>("Investigation:Waybill:LegacyCutoffDays", 7);

        public bool CheckOverdueDeliveries => 
            _configuration.GetValue<bool>("Investigation:Waybill:CheckOverdueDeliveries", true);

        public bool CheckExpiringSoon => 
            _configuration.GetValue<bool>("Investigation:Waybill:CheckExpiringSoon", true);

        public bool CheckLegacyWaybills => 
            _configuration.GetValue<bool>("Investigation:Waybill:CheckLegacyWaybills", true);
    }
}