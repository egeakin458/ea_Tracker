using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Services.Interfaces
{
    /// <summary>
    /// Service interface for investigator administration operations.
    /// Encapsulates all investigator instance management business logic.
    /// </summary>
    public interface IInvestigatorAdminService
    {
        // =====================================
        // Standard CRUD Operations
        // =====================================
        
        /// <summary>
        /// Gets all active investigator instances with their types.
        /// </summary>
        Task<IEnumerable<InvestigatorInstanceResponseDto>> GetInvestigatorsAsync();
        
        /// <summary>
        /// Gets a specific investigator instance by ID.
        /// </summary>
        Task<InvestigatorInstanceResponseDto?> GetInvestigatorAsync(Guid id);
        
        /// <summary>
        /// Creates a new investigator instance.
        /// </summary>
        Task<InvestigatorInstanceResponseDto> CreateInvestigatorAsync(CreateInvestigatorInstanceDto createDto);
        
        /// <summary>
        /// Updates an existing investigator instance.
        /// </summary>
        Task<InvestigatorInstanceResponseDto> UpdateInvestigatorAsync(Guid id, UpdateInvestigatorInstanceDto updateDto);
        
        /// <summary>
        /// Deletes an investigator instance.
        /// </summary>
        Task<bool> DeleteInvestigatorAsync(Guid id);
        
        // =====================================
        // Business Query Operations
        // =====================================
        
        /// <summary>
        /// Gets investigator instances by type code.
        /// </summary>
        Task<IEnumerable<InvestigatorInstanceResponseDto>> GetInvestigatorsByTypeAsync(string typeCode);
        
        /// <summary>
        /// Gets summary statistics for all investigators.
        /// </summary>
        Task<InvestigatorSummaryDto> GetSummaryAsync();
        
        /// <summary>
        /// Gets available investigator types.
        /// </summary>
        Task<IEnumerable<InvestigatorTypeDto>> GetTypesAsync();
    }
}