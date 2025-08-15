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
        
        /// <summary>
        /// Exports multiple investigation results to a single file based on the provided execution IDs.
        /// </summary>
        /// <param name="request">The bulk export request containing execution IDs and the desired format.</param>
        /// <returns>An export DTO containing the generated file's data and metadata.</returns>
        Task<InvestigationExportDto?> ExportInvestigationsAsync(BulkExportRequestDto request);
    }
}