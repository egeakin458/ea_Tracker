using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace ea_Tracker.Services
{
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