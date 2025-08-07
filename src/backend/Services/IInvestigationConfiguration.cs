using System.ComponentModel;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Interface for investigation business rule configuration.
    /// Provides externalized thresholds and parameters for investigation logic.
    /// Implements Open/Closed Principle - open for extension, closed for modification.
    /// </summary>
    public interface IInvestigationConfiguration
    {
        /// <summary>
        /// Gets invoice-specific investigation configuration.
        /// </summary>
        IInvoiceInvestigationConfig Invoice { get; }

        /// <summary>
        /// Gets waybill-specific investigation configuration.
        /// </summary>
        IWaybillInvestigationConfig Waybill { get; }
    }

    /// <summary>
    /// Configuration for invoice anomaly detection rules.
    /// </summary>
    public interface IInvoiceInvestigationConfig
    {
        /// <summary>
        /// Maximum tax ratio allowed (e.g., 0.5 = 50%).
        /// Default: 0.5 (50% of total amount)
        /// </summary>
        [DefaultValue(0.5)]
        decimal MaxTaxRatio { get; }

        /// <summary>
        /// Whether to check for negative amounts.
        /// Default: true
        /// </summary>
        [DefaultValue(true)]
        bool CheckNegativeAmounts { get; }

        /// <summary>
        /// Whether to check for future dates.
        /// Default: true
        /// </summary>
        [DefaultValue(true)]
        bool CheckFutureDates { get; }

        /// <summary>
        /// Maximum allowed days in the future for invoice dates.
        /// Default: 0 (no future dates allowed)
        /// </summary>
        [DefaultValue(0)]
        int MaxFutureDays { get; }
    }

    /// <summary>
    /// Configuration for waybill delivery investigation rules.
    /// </summary>
    public interface IWaybillInvestigationConfig
    {
        /// <summary>
        /// Hours threshold for "expiring soon" detection.
        /// Default: 24 (1 day)
        /// </summary>
        [DefaultValue(24)]
        int ExpiringSoonHours { get; }

        /// <summary>
        /// Days threshold for legacy waybill cutoff (waybills without due dates).
        /// Default: 7 days
        /// </summary>
        [DefaultValue(7)]
        int LegacyCutoffDays { get; }

        /// <summary>
        /// Whether to check for overdue deliveries (past due date).
        /// Default: true
        /// </summary>
        [DefaultValue(true)]
        bool CheckOverdueDeliveries { get; }

        /// <summary>
        /// Whether to check for deliveries expiring soon.
        /// Default: true
        /// </summary>
        [DefaultValue(true)]
        bool CheckExpiringSoon { get; }

        /// <summary>
        /// Whether to check legacy waybills (those without due dates).
        /// Default: true
        /// </summary>
        [DefaultValue(true)]
        bool CheckLegacyWaybills { get; }
    }
}