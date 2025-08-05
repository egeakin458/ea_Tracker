using ea_Tracker.Models;
using ea_Tracker.Enums;

namespace ea_Tracker.Repositories
{
    /// <summary>
    /// Repository interface for investigator-related operations.
    /// </summary>
    public interface IInvestigatorRepository : IGenericRepository<InvestigatorInstance>
    {
        /// <summary>
        /// Gets all active investigator instances with their types loaded.
        /// </summary>
        Task<IEnumerable<InvestigatorInstance>> GetActiveWithTypesAsync();

        /// <summary>
        /// Gets investigator instances by type code (e.g., "invoice", "waybill").
        /// </summary>
        Task<IEnumerable<InvestigatorInstance>> GetByTypeAsync(string typeCode);

        /// <summary>
        /// Gets investigator instance with full navigation properties loaded.
        /// </summary>
        Task<InvestigatorInstance?> GetWithDetailsAsync(Guid id);

        /// <summary>
        /// Gets recent execution history for an investigator.
        /// </summary>
        Task<IEnumerable<InvestigationExecution>> GetExecutionHistoryAsync(Guid investigatorId, int take = 10);

        /// <summary>
        /// Gets summary statistics for all investigators.
        /// </summary>
        Task<InvestigatorSummary> GetSummaryAsync();

        /// <summary>
        /// Updates the last executed timestamp for an investigator.
        /// </summary>
        Task UpdateLastExecutedAsync(Guid investigatorId, DateTime timestamp);
    }

    /// <summary>
    /// Summary statistics for all investigators.
    /// </summary>
    public class InvestigatorSummary
    {
        public int TotalInvestigators { get; set; }
        public int ActiveInvestigators { get; set; }
        public int RunningInvestigators { get; set; }
        public int TotalExecutions { get; set; }
        public long TotalResults { get; set; }
    }
}