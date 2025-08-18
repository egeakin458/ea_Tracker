using AutoMapper;
using ea_Tracker.Exceptions;
using ea_Tracker.Models;
using ea_Tracker.Models.Common;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service implementation for waybill business operations.
    /// Encapsulates business logic extracted from WaybillsController.cs.
    /// Provides both DTO-based operations for controllers and entity-based operations for investigation system.
    /// </summary>
    public class WaybillService : IWaybillService
    {
        private readonly IGenericRepository<Waybill> _repository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WaybillService> _logger;

        public WaybillService(
            IGenericRepository<Waybill> repository,
            IMapper mapper,
            IConfiguration configuration,
            ILogger<WaybillService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
        }

        // =====================================
        // Standard CRUD Operations (Returns DTOs)
        // =====================================

        public async Task<WaybillResponseDto?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving waybill with ID {WaybillId}", id);

            var waybill = await _repository.GetByIdAsync(id);
            if (waybill == null)
            {
                _logger.LogWarning("Waybill with ID {WaybillId} not found", id);
                return null;
            }

            return _mapper.Map<WaybillResponseDto>(waybill);
        }

        public async Task<IEnumerable<WaybillResponseDto>> GetAllAsync(WaybillFilterDto? filter = null)
        {
            _logger.LogDebug("Retrieving waybills with filters");

            var waybills = await _repository.GetAsync(
                filter: BuildFilterExpression(filter),
                orderBy: q => q.OrderByDescending(w => w.CreatedAt)
            );

            var result = waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
            _logger.LogInformation("Retrieved {WaybillCount} waybills", result.Count());
            return result;
        }

        public async Task<WaybillResponseDto> CreateAsync(CreateWaybillDto createDto)
        {
            _logger.LogDebug("Creating waybill for recipient {RecipientName}", createDto.RecipientName);

            // Validate business rules
            var validationResult = await ValidateWaybillAsync(createDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Waybill validation failed: {ValidationErrors}", string.Join("; ", validationResult.Errors));
                throw new ValidationException(validationResult);
            }

            // Map DTO to entity
            var waybill = _mapper.Map<Waybill>(createDto);

            // Create and save
            var createdWaybill = await _repository.AddAsync(waybill);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Created new waybill {WaybillId} for recipient {RecipientName}", 
                createdWaybill.Id, createDto.RecipientName);

            return _mapper.Map<WaybillResponseDto>(createdWaybill);
        }

        public async Task<WaybillResponseDto> UpdateAsync(int id, UpdateWaybillDto updateDto)
        {
            _logger.LogDebug("Updating waybill {WaybillId}", id);

            var waybill = await _repository.GetByIdAsync(id);
            if (waybill == null)
            {
                _logger.LogWarning("Waybill with ID {WaybillId} not found for update", id);
                throw new ValidationException($"Waybill with ID {id} not found");
            }

            // Store original values for logging
            var originalRecipient = waybill.RecipientName;

            // Map updates to entity
            _mapper.Map(updateDto, waybill);

            // Validate business rules after update
            var validationResult = ValidateWaybillEntitySync(waybill);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Waybill update validation failed for ID {WaybillId}: {ValidationErrors}", 
                    id, string.Join("; ", validationResult.Errors));
                throw new ValidationException(validationResult);
            }

            _repository.Update(waybill);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Updated waybill {WaybillId} (recipient changed from {OriginalRecipient} to {NewRecipient})", 
                id, originalRecipient, waybill.RecipientName);

            return _mapper.Map<WaybillResponseDto>(waybill);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogDebug("Attempting to delete waybill {WaybillId}", id);

            var waybill = await _repository.GetByIdAsync(id);
            if (waybill == null)
            {
                _logger.LogWarning("Waybill with ID {WaybillId} not found for deletion", id);
                return false;
            }

            // Check if deletion is allowed
            if (!await CanDeleteAsync(id))
            {
                _logger.LogWarning("Waybill {WaybillId} cannot be deleted due to business rules", id);
                return false;
            }

            _repository.Remove(waybill);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Deleted waybill {WaybillId}", id);
            return true;
        }

        // =====================================
        // Business Query Operations (Returns DTOs)
        // =====================================

        public async Task<IEnumerable<WaybillResponseDto>> GetAnomalousWaybillsAsync()
        {
            _logger.LogDebug("Retrieving anomalous waybills");

            var waybills = await _repository.GetAsync(
                filter: w => w.HasAnomalies,
                orderBy: q => q.OrderByDescending(w => w.LastInvestigatedAt)
            );

            return waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
        }

        public async Task<IEnumerable<WaybillResponseDto>> GetOverdueWaybillsAsync()
        {
            var today = DateTime.UtcNow.Date;
            _logger.LogDebug("Retrieving overdue waybills (past due date {Today})", today);

            var waybills = await _repository.GetAsync(
                filter: w => w.DueDate.HasValue && w.DueDate.Value.Date < today,
                orderBy: q => q.OrderBy(w => w.DueDate)
            );

            return waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
        }

        public async Task<IEnumerable<WaybillResponseDto>> GetWaybillsExpiringSoonAsync(int? days = null)
        {
            var configuredHours = _configuration.GetValue<int>("Investigation:Waybill:ExpiringSoonHours", 24);
            var daysToCheck = days ?? (configuredHours / 24);
            var cutoffDate = DateTime.UtcNow.Date.AddDays(daysToCheck);

            _logger.LogDebug("Retrieving waybills expiring within {Days} days (by {CutoffDate})", daysToCheck, cutoffDate);

            var waybills = await _repository.GetAsync(
                filter: w => w.DueDate.HasValue && 
                           w.DueDate.Value.Date <= cutoffDate && 
                           w.DueDate.Value.Date >= DateTime.UtcNow.Date,
                orderBy: q => q.OrderBy(w => w.DueDate)
            );

            return waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
        }

        public async Task<IEnumerable<WaybillResponseDto>> GetLegacyWaybillsAsync(int? cutoffDays = null)
        {
            var configuredDays = _configuration.GetValue<int>("Investigation:Waybill:LegacyCutoffDays", 7);
            var daysOld = cutoffDays ?? configuredDays;
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-daysOld);

            _logger.LogDebug("Retrieving legacy waybills (older than {Days} days, before {CutoffDate})", daysOld, cutoffDate);

            var waybills = await _repository.GetAsync(
                filter: w => w.GoodsIssueDate < cutoffDate,
                orderBy: q => q.OrderBy(w => w.GoodsIssueDate)
            );

            return waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
        }

        public async Task<IEnumerable<WaybillResponseDto>> GetLateDeliveryWaybillsAsync(int daysLate = 7)
        {
            var cutoffDate = DateTime.UtcNow.Date.AddDays(-daysLate);
            _logger.LogDebug("Retrieving waybills with late deliveries (due before {CutoffDate})", cutoffDate);

            var waybills = await _repository.GetAsync(
                filter: w => w.DueDate.HasValue && w.DueDate.Value.Date < cutoffDate,
                orderBy: q => q.OrderBy(w => w.DueDate)
            );

            return waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
        }

        public async Task<IEnumerable<WaybillResponseDto>> GetWaybillsByDateRangeAsync(DateTime from, DateTime to)
        {
            _logger.LogDebug("Retrieving waybills from {FromDate} to {ToDate}", from, to);

            var waybills = await _repository.GetAsync(
                filter: w => w.GoodsIssueDate >= from && w.GoodsIssueDate <= to,
                orderBy: q => q.OrderBy(w => w.GoodsIssueDate)
            );

            return waybills.Select(w => _mapper.Map<WaybillResponseDto>(w));
        }

        // =====================================
        // Business Logic & Validation
        // =====================================

        public Task<ValidationResult> ValidateWaybillAsync(CreateWaybillDto createDto)
        {
            var errors = new List<string>();

            // DTO-level validation
            if (string.IsNullOrWhiteSpace(createDto.RecipientName))
                errors.Add("Recipient name is required");

            if (!string.IsNullOrEmpty(createDto.RecipientName) && createDto.RecipientName.Length > 200)
                errors.Add("Recipient name cannot exceed 200 characters");

            if (!string.IsNullOrEmpty(createDto.ShippedItems) && createDto.ShippedItems.Length > 1000)
                errors.Add("Shipped items description cannot exceed 1000 characters");

            // Create temporary entity for business rule validation
            var tempWaybill = _mapper.Map<Waybill>(createDto);
            var entityValidation = ValidateWaybillEntitySync(tempWaybill);
            errors.AddRange(entityValidation.Errors);

            return Task.FromResult(new ValidationResult(errors));
        }

        public async Task<bool> CanDeleteAsync(int id)
        {
            var waybill = await _repository.GetByIdAsync(id);
            if (waybill == null) return false;

            // Business rule: Cannot delete waybills that have been investigated and have anomalies
            if (waybill.HasAnomalies && waybill.LastInvestigatedAt.HasValue)
                return false;

            // Business rule: Cannot delete waybills older than 30 days (audit requirements)
            if (waybill.CreatedAt < DateTime.UtcNow.AddDays(-30))
                return false;

            // Business rule: Cannot delete overdue waybills (business compliance)
            if (waybill.DueDate.HasValue && waybill.DueDate.Value < DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task<WaybillStatisticsDto> GetStatisticsAsync()
        {
            _logger.LogDebug("Calculating waybill statistics");

            var allWaybills = await _repository.GetAllAsync();
            var waybillsList = allWaybills.ToList();

            var today = DateTime.UtcNow.Date;
            var configuredExpiringSoonHours = _configuration.GetValue<int>("Investigation:Waybill:ExpiringSoonHours", 24);
            var expiringSoonDate = today.AddHours(configuredExpiringSoonHours);
            
            var configuredLegacyDays = _configuration.GetValue<int>("Investigation:Waybill:LegacyCutoffDays", 7);
            var legacyCutoffDate = today.AddDays(-configuredLegacyDays);

            var stats = new WaybillStatisticsDto
            {
                TotalCount = waybillsList.Count,
                AnomalousCount = waybillsList.Count(w => w.HasAnomalies),
                OverdueCount = waybillsList.Count(w => w.DueDate.HasValue && w.DueDate.Value.Date < today),
                ExpiringSoonCount = waybillsList.Count(w => w.DueDate.HasValue && 
                    w.DueDate.Value <= expiringSoonDate && w.DueDate.Value.Date >= today),
                LegacyCount = waybillsList.Count(w => w.GoodsIssueDate < legacyCutoffDate),
                LateDeliveryCount = waybillsList.Count(w => w.DueDate.HasValue && w.DueDate.Value.Date < today.AddDays(-7)),
                OnTimeDeliveryCount = waybillsList.Count(w => !w.DueDate.HasValue || w.DueDate.Value.Date >= today)
            };

            // Calculate weight statistics (if waybill has weight property)
            // Note: Need to check if Waybill entity has Weight property
            // stats.TotalWeight = waybillsList.Sum(w => w.Weight ?? 0);

            _logger.LogInformation("Calculated statistics: {TotalCount} total waybills, {AnomalousCount} anomalous, {OverdueCount} overdue",
                stats.TotalCount, stats.AnomalousCount, stats.OverdueCount);

            return stats;
        }

        // =====================================
        // Investigation System Compatibility (Returns Entities)
        // =====================================

        public async Task<IEnumerable<Waybill>> GetAllEntitiesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Waybill?> GetEntityByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Waybill>> GetWaybillsForInvestigationAsync(DateTime? lastInvestigated = null)
        {
            if (lastInvestigated.HasValue)
            {
                return await _repository.GetAsync(
                    filter: w => !w.LastInvestigatedAt.HasValue || w.LastInvestigatedAt < lastInvestigated,
                    orderBy: q => q.OrderBy(w => w.LastInvestigatedAt ?? w.CreatedAt)
                );
            }

            return await _repository.GetAllAsync();
        }

        public async Task UpdateAnomalyStatusAsync(int waybillId, bool hasAnomalies, DateTime investigatedAt)
        {
            var waybill = await _repository.GetByIdAsync(waybillId);
            if (waybill == null)
            {
                _logger.LogWarning("Cannot update anomaly status - waybill {WaybillId} not found", waybillId);
                return;
            }

            waybill.HasAnomalies = hasAnomalies;
            waybill.LastInvestigatedAt = investigatedAt;
            
            _repository.Update(waybill);
            await _repository.SaveChangesAsync();

            _logger.LogDebug("Updated anomaly status for waybill {WaybillId}: HasAnomalies={HasAnomalies}", 
                waybillId, hasAnomalies);
        }

        public async Task BatchUpdateAnomalyStatusAsync(IEnumerable<(int WaybillId, bool HasAnomalies, DateTime InvestigatedAt)> updates)
        {
            var updatesList = updates.ToList();
            _logger.LogDebug("Batch updating anomaly status for {Count} waybills", updatesList.Count);

            foreach (var (waybillId, hasAnomalies, investigatedAt) in updatesList)
            {
                await UpdateAnomalyStatusAsync(waybillId, hasAnomalies, investigatedAt);
            }
        }

        // =====================================
        // Private Helper Methods
        // =====================================

        private System.Linq.Expressions.Expression<Func<Waybill, bool>>? BuildFilterExpression(WaybillFilterDto? filter)
        {
            if (filter == null) return null;

            var today = DateTime.UtcNow.Date;
            
            return w => (filter.HasAnomalies == null || w.HasAnomalies == filter.HasAnomalies) &&
                       (filter.FromIssueDate == null || w.GoodsIssueDate >= filter.FromIssueDate) &&
                       (filter.ToIssueDate == null || w.GoodsIssueDate <= filter.ToIssueDate) &&
                       (filter.FromExpectedDeliveryDate == null || (w.DueDate.HasValue && w.DueDate.Value >= filter.FromExpectedDeliveryDate)) &&
                       (filter.ToExpectedDeliveryDate == null || (w.DueDate.HasValue && w.DueDate.Value <= filter.ToExpectedDeliveryDate)) &&
                       (filter.RecipientName == null || w.RecipientName!.Contains(filter.RecipientName)) &&
                       // (filter.SenderName == null || w.SenderName!.Contains(filter.SenderName)) && // SenderName not in Waybill model
                       (filter.IsOverdue == null || (filter.IsOverdue.Value == (w.DueDate.HasValue && w.DueDate.Value.Date < today)));
        }

        private ValidationResult ValidateWaybillEntitySync(Waybill waybill)
        {
            var errors = new List<string>();

            // Business rule: Goods issue date cannot be in future
            if (waybill.GoodsIssueDate > DateTime.UtcNow.Date)
                errors.Add("Goods issue date cannot be in the future");

            // Business rule: Goods issue date cannot be too old (more than 5 years)
            if (waybill.GoodsIssueDate < DateTime.UtcNow.AddYears(-5).Date)
                errors.Add("Goods issue date cannot be more than 5 years old");

            // Business rule: Due date must be after goods issue date
            if (waybill.DueDate.HasValue && waybill.DueDate.Value.Date < waybill.GoodsIssueDate.Date)
                errors.Add("Due date cannot be earlier than goods issue date");

            // Business rule: Due date cannot be too far in future (more than 1 year)
            if (waybill.DueDate.HasValue && waybill.DueDate.Value > DateTime.UtcNow.AddYears(1))
                errors.Add("Due date cannot be more than 1 year in the future");

            return new ValidationResult(errors);
        }
    }
}