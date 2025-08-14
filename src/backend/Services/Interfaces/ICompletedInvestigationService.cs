using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Services.Interfaces
{
    /// <summary>
    /// Service interface for completed investigation operations.
    /// Encapsulates all investigation result viewing and management logic.
    /// </summary>
    public interface ICompletedInvestigationService
    {
        /// <summary>
        /// Gets all completed investigations with summary information.
        /// </summary>
        Task<IEnumerable<CompletedInvestigationDto>> GetAllCompletedAsync();
        
        /// <summary>
        /// Gets detailed information about a specific investigation execution.
        /// </summary>
        Task<InvestigationDetailDto?> GetInvestigationDetailAsync(int executionId);
        
        /// <summary>
        /// Clears all completed investigation data.
        /// </summary>
        Task<ClearInvestigationsResultDto> ClearAllCompletedInvestigationsAsync();
        
        /// <summary>
        /// Deletes a specific investigation execution.
        /// </summary>
        Task<DeleteInvestigationResultDto> DeleteInvestigationExecutionAsync(int executionId);
    }
}