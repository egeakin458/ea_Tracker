# Phase 3: Investigator & Completed Investigations Service Layer Implementation Plan

## Executive Summary

This document provides a **complete, step-by-step implementation plan** for refactoring the remaining two controllers (`InvestigatorController` and `CompletedInvestigationsController`) to align with the successful service layer architecture already implemented for `InvoicesController` and `WaybillsController`.

**Objective**: Achieve architectural consistency across all controllers by introducing service layer abstraction.

**Expected Outcome**: 
- ~40-50% code reduction in controllers
- Complete separation of concerns
- Unified architecture pattern
- Improved testability and maintainability

---

## Current State Analysis

### ❌ Architectural Violations Identified

#### 1. **InvestigatorController** (296 lines)
```csharp
// VIOLATION: Direct repository injection
private readonly IInvestigatorRepository _investigatorRepository;
private readonly IGenericRepository<InvestigatorType> _typeRepository;
```
- Contains business logic for investigator management
- Performs manual DTO mapping (no AutoMapper)
- Direct repository access violates service layer pattern

#### 2. **CompletedInvestigationsController** (149 lines)
```csharp
// VIOLATION: Direct DbContext injection
private readonly ApplicationDbContext _context;
```
- Complex LINQ queries directly in controller
- Anonymous type projections instead of DTOs
- No service layer abstraction

### ✅ Target Architecture (Already Achieved)
```csharp
// InvoicesController - CLEAN ARCHITECTURE
private readonly IInvoiceService _invoiceService;
// All business logic delegated to service layer
```

---

## Implementation Plan - Part A: InvestigatorController Refactoring

### Step A1: Create Service Interface
**File**: `src/backend/Services/Interfaces/IInvestigatorAdminService.cs`

```csharp
using ea_Tracker.Models.Dtos;
using ea_Tracker.Models.Common;

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
```

### Step A2: Create Service Implementation
**File**: `src/backend/Services/Implementations/InvestigatorAdminService.cs`

```csharp
using AutoMapper;
using ea_Tracker.Exceptions;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Services.Interfaces;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service implementation for investigator administration operations.
    /// </summary>
    public class InvestigatorAdminService : IInvestigatorAdminService
    {
        private readonly IInvestigatorRepository _investigatorRepository;
        private readonly IGenericRepository<InvestigatorType> _typeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<InvestigatorAdminService> _logger;

        public InvestigatorAdminService(
            IInvestigatorRepository investigatorRepository,
            IGenericRepository<InvestigatorType> typeRepository,
            IMapper mapper,
            ILogger<InvestigatorAdminService> logger)
        {
            _investigatorRepository = investigatorRepository;
            _typeRepository = typeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<InvestigatorInstanceResponseDto>> GetInvestigatorsAsync()
        {
            _logger.LogDebug("Retrieving all active investigators");
            
            var investigators = await _investigatorRepository.GetActiveWithTypesAsync();
            return investigators.Select(i => _mapper.Map<InvestigatorInstanceResponseDto>(i));
        }

        public async Task<InvestigatorInstanceResponseDto?> GetInvestigatorAsync(Guid id)
        {
            _logger.LogDebug("Retrieving investigator {InvestigatorId}", id);
            
            var investigator = await _investigatorRepository.GetWithDetailsAsync(id);
            if (investigator == null)
            {
                _logger.LogWarning("Investigator {InvestigatorId} not found", id);
                return null;
            }
            
            return _mapper.Map<InvestigatorInstanceResponseDto>(investigator);
        }

        public async Task<InvestigatorInstanceResponseDto> CreateInvestigatorAsync(CreateInvestigatorInstanceDto createDto)
        {
            _logger.LogDebug("Creating investigator of type {TypeCode}", createDto.TypeCode);
            
            // Validate the investigator type exists
            var investigatorType = await _typeRepository.GetFirstOrDefaultAsync(
                t => t.Code == createDto.TypeCode && t.IsActive);
            
            if (investigatorType == null)
            {
                throw new ValidationException($"Invalid investigator type code: {createDto.TypeCode}");
            }

            // Map DTO to entity
            var investigator = _mapper.Map<InvestigatorInstance>(createDto);
            investigator.Id = Guid.NewGuid();
            investigator.TypeId = investigatorType.Id;
            investigator.CreatedAt = DateTime.UtcNow;
            investigator.IsActive = true;

            await _investigatorRepository.AddAsync(investigator);
            await _investigatorRepository.SaveChangesAsync();

            // Reload with navigation properties
            var createdInvestigator = await _investigatorRepository.GetWithDetailsAsync(investigator.Id);
            
            _logger.LogInformation("Created investigator {InvestigatorId} of type {TypeCode}", 
                investigator.Id, createDto.TypeCode);
            
            return _mapper.Map<InvestigatorInstanceResponseDto>(createdInvestigator!);
        }

        public async Task<InvestigatorInstanceResponseDto> UpdateInvestigatorAsync(Guid id, UpdateInvestigatorInstanceDto updateDto)
        {
            _logger.LogDebug("Updating investigator {InvestigatorId}", id);
            
            var investigator = await _investigatorRepository.GetByIdAsync(id);
            if (investigator == null)
            {
                throw new ValidationException($"Investigator with ID {id} not found");
            }

            // Map updates to entity
            _mapper.Map(updateDto, investigator);

            _investigatorRepository.Update(investigator);
            await _investigatorRepository.SaveChangesAsync();

            // Reload with navigation properties
            var updatedInvestigator = await _investigatorRepository.GetWithDetailsAsync(id);
            
            _logger.LogInformation("Updated investigator {InvestigatorId}", id);
            
            return _mapper.Map<InvestigatorInstanceResponseDto>(updatedInvestigator!);
        }

        public async Task<bool> DeleteInvestigatorAsync(Guid id)
        {
            _logger.LogDebug("Deleting investigator {InvestigatorId}", id);
            
            var investigator = await _investigatorRepository.GetByIdAsync(id);
            if (investigator == null)
            {
                _logger.LogWarning("Investigator {InvestigatorId} not found for deletion", id);
                return false;
            }

            _investigatorRepository.Remove(investigator);
            await _investigatorRepository.SaveChangesAsync();
            
            _logger.LogInformation("Deleted investigator {InvestigatorId}", id);
            return true;
        }

        public async Task<IEnumerable<InvestigatorInstanceResponseDto>> GetInvestigatorsByTypeAsync(string typeCode)
        {
            _logger.LogDebug("Retrieving investigators of type {TypeCode}", typeCode);
            
            var investigators = await _investigatorRepository.GetByTypeAsync(typeCode);
            return investigators.Select(i => _mapper.Map<InvestigatorInstanceResponseDto>(i));
        }

        public async Task<InvestigatorSummaryDto> GetSummaryAsync()
        {
            _logger.LogDebug("Calculating investigator summary");
            
            var summary = await _investigatorRepository.GetSummaryAsync();
            return _mapper.Map<InvestigatorSummaryDto>(summary);
        }

        public async Task<IEnumerable<InvestigatorTypeDto>> GetTypesAsync()
        {
            _logger.LogDebug("Retrieving investigator types");
            
            var types = await _typeRepository.GetAsync(
                t => t.IsActive, 
                orderBy: q => q.OrderBy(t => t.DisplayName));
            
            return types.Select(t => _mapper.Map<InvestigatorTypeDto>(t));
        }
    }
}
```

### Step A3: Update AutoMapper Configuration
**File**: `src/backend/Mapping/AutoMapperProfile.cs`
**Location**: Update the `ConfigureInvestigatorMappings()` method (lines 64-68)

```csharp
private void ConfigureInvestigatorMappings()
{
    // InvestigatorType to DTO mapping
    CreateMap<InvestigatorType, InvestigatorTypeDto>();
    
    // Create InvestigatorInstance mappings
    CreateMap<CreateInvestigatorInstanceDto, InvestigatorInstance>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.TypeId, opt => opt.Ignore())
        .ForMember(dest => dest.Type, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.LastExecutedAt, opt => opt.Ignore())
        .ForMember(dest => dest.IsActive, opt => opt.Ignore())
        .ForMember(dest => dest.Status, opt => opt.Ignore())
        .ForMember(dest => dest.TotalResultCount, opt => opt.Ignore());
    
    // Update InvestigatorInstance mappings
    CreateMap<UpdateInvestigatorInstanceDto, InvestigatorInstance>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.TypeId, opt => opt.Ignore())
        .ForMember(dest => dest.Type, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.LastExecutedAt, opt => opt.Ignore())
        .ForMember(dest => dest.Status, opt => opt.Ignore())
        .ForMember(dest => dest.TotalResultCount, opt => opt.Ignore());
    
    // InvestigatorInstance to Response mapping
    CreateMap<InvestigatorInstance, InvestigatorInstanceResponseDto>()
        .ForMember(dest => dest.DisplayName, 
            opt => opt.MapFrom(src => src.CustomName ?? src.Type.DisplayName));
    
    // InvestigatorSummary to DTO mapping
    CreateMap<InvestigatorSummary, InvestigatorSummaryDto>();
}
```

### Step A4: Refactor InvestigatorController
**File**: `src/backend/Controllers/InvestigatorController.cs`
**Action**: Replace entire file with thin controller

```csharp
using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Exceptions;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing investigator instances with service layer architecture.
    /// Delegates business logic to InvestigatorAdminService for better separation of concerns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvestigatorController : ControllerBase
    {
        private readonly IInvestigatorAdminService _investigatorService;
        private readonly ILogger<InvestigatorController> _logger;

        public InvestigatorController(
            IInvestigatorAdminService investigatorService,
            ILogger<InvestigatorController> logger)
        {
            _investigatorService = investigatorService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvestigatorInstanceResponseDto>>> GetInvestigators()
        {
            try
            {
                var investigators = await _investigatorService.GetInvestigatorsAsync();
                return Ok(investigators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigators");
                return StatusCode(500, "An error occurred while retrieving investigators");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvestigatorInstanceResponseDto>> GetInvestigator(Guid id)
        {
            try
            {
                var investigator = await _investigatorService.GetInvestigatorAsync(id);
                if (investigator == null)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }
                return Ok(investigator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator {InvestigatorId}", id);
                return StatusCode(500, "An error occurred while retrieving the investigator");
            }
        }

        [HttpPost]
        public async Task<ActionResult<InvestigatorInstanceResponseDto>> CreateInvestigator(CreateInvestigatorInstanceDto createDto)
        {
            try
            {
                var createdInvestigator = await _investigatorService.CreateInvestigatorAsync(createDto);
                return CreatedAtAction(nameof(GetInvestigator), new { id = createdInvestigator.Id }, createdInvestigator);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Investigator validation failed: {ValidationErrors}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating investigator");
                return StatusCode(500, "An error occurred while creating the investigator");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvestigatorInstanceResponseDto>> UpdateInvestigator(Guid id, UpdateInvestigatorInstanceDto updateDto)
        {
            try
            {
                var updatedInvestigator = await _investigatorService.UpdateInvestigatorAsync(id, updateDto);
                return Ok(updatedInvestigator);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Investigator update validation failed: {ValidationErrors}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating investigator {InvestigatorId}", id);
                return StatusCode(500, "An error occurred while updating the investigator");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvestigator(Guid id)
        {
            try
            {
                var deleted = await _investigatorService.DeleteInvestigatorAsync(id);
                if (!deleted)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting investigator {InvestigatorId}", id);
                return StatusCode(500, "An error occurred while deleting the investigator");
            }
        }

        [HttpGet("by-type/{typeCode}")]
        public async Task<ActionResult<IEnumerable<InvestigatorInstanceResponseDto>>> GetInvestigatorsByType(string typeCode)
        {
            try
            {
                var investigators = await _investigatorService.GetInvestigatorsByTypeAsync(typeCode);
                return Ok(investigators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigators by type {TypeCode}", typeCode);
                return StatusCode(500, "An error occurred while retrieving investigators");
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<InvestigatorSummaryDto>> GetSummary()
        {
            try
            {
                var summary = await _investigatorService.GetSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator summary");
                return StatusCode(500, "An error occurred while retrieving the summary");
            }
        }

        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<InvestigatorTypeDto>>> GetTypes()
        {
            try
            {
                var types = await _investigatorService.GetTypesAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator types");
                return StatusCode(500, "An error occurred while retrieving investigator types");
            }
        }
    }
}
```

---

## Implementation Plan - Part B: CompletedInvestigationsController Refactoring

### Step B1: Create Service Interface
**File**: `src/backend/Services/Interfaces/ICompletedInvestigationService.cs`

```csharp
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
        Task<IEnumerable<CompletedInvestigationSummaryDto>> GetAllCompletedAsync();
        
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
```

### Step B2: Create Required DTOs
**File**: `src/backend/Models/Dtos/CompletedInvestigationDtos.cs`

```csharp
namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// DTO for completed investigation summary information.
    /// </summary>
    public class CompletedInvestigationSummaryDto
    {
        public int ExecutionId { get; set; }
        public Guid InvestigatorId { get; set; }
        public string InvestigatorName { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public int ResultCount { get; set; }
        public int AnomalyCount { get; set; }
    }
    
    /// <summary>
    /// DTO for investigation detail with results.
    /// </summary>
    public class InvestigationDetailDto
    {
        public CompletedInvestigationSummaryDto Summary { get; set; } = null!;
        public IEnumerable<InvestigationResultDto> DetailedResults { get; set; } = null!;
    }
    
    /// <summary>
    /// DTO for individual investigation results.
    /// </summary>
    public class InvestigationResultDto
    {
        public string InvestigatorId { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = null!;
        public string? Payload { get; set; }
    }
    
    /// <summary>
    /// DTO for clear investigations operation result.
    /// </summary>
    public class ClearInvestigationsResultDto
    {
        public string Message { get; set; } = null!;
        public int ResultsDeleted { get; set; }
        public int ExecutionsDeleted { get; set; }
    }
    
    /// <summary>
    /// DTO for delete investigation operation result.
    /// </summary>
    public class DeleteInvestigationResultDto
    {
        public string Message { get; set; } = null!;
        public int ResultsDeleted { get; set; }
    }
}
```

### Step B3: Create Service Implementation
**File**: `src/backend/Services/Implementations/CompletedInvestigationService.cs`

```csharp
using AutoMapper;
using ea_Tracker.Data;
using ea_Tracker.Enums;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service implementation for completed investigation operations.
    /// </summary>
    public class CompletedInvestigationService : ICompletedInvestigationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CompletedInvestigationService> _logger;

        public CompletedInvestigationService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<CompletedInvestigationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<CompletedInvestigationSummaryDto>> GetAllCompletedAsync()
        {
            _logger.LogDebug("Retrieving all completed investigations");

            var completedExecutions = await _context.InvestigationExecutions
                .Include(e => e.Investigator)
                .Where(e => e.ResultCount > 0)
                .OrderByDescending(e => e.StartedAt)
                .Select(e => new CompletedInvestigationSummaryDto
                {
                    ExecutionId = e.Id,
                    InvestigatorId = e.InvestigatorId,
                    InvestigatorName = e.Investigator.CustomName ?? "Investigation",
                    StartedAt = e.StartedAt,
                    CompletedAt = e.CompletedAt ?? e.StartedAt,
                    ResultCount = e.ResultCount,
                    AnomalyCount = _context.InvestigationResults
                        .Count(r => r.ExecutionId == e.Id && 
                              (r.Severity == ResultSeverity.Anomaly || 
                               r.Severity == ResultSeverity.Critical))
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} completed investigations", completedExecutions.Count);
            return completedExecutions;
        }

        public async Task<InvestigationDetailDto?> GetInvestigationDetailAsync(int executionId)
        {
            _logger.LogDebug("Retrieving investigation detail for execution {ExecutionId}", executionId);

            var execution = await _context.InvestigationExecutions
                .Include(e => e.Investigator)
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution == null)
            {
                _logger.LogWarning("Investigation execution {ExecutionId} not found", executionId);
                return null;
            }

            var anomalyCount = await _context.InvestigationResults
                .CountAsync(r => r.ExecutionId == executionId && 
                      (r.Severity == ResultSeverity.Anomaly || 
                       r.Severity == ResultSeverity.Critical));

            var summary = new CompletedInvestigationSummaryDto
            {
                ExecutionId = execution.Id,
                InvestigatorId = execution.InvestigatorId,
                InvestigatorName = execution.Investigator.CustomName ?? "Investigation",
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt ?? execution.StartedAt,
                ResultCount = execution.ResultCount,
                AnomalyCount = anomalyCount
            };

            var results = await _context.InvestigationResults
                .Where(r => r.ExecutionId == executionId)
                .OrderBy(r => r.Timestamp)
                .Take(100)
                .Select(r => new InvestigationResultDto
                {
                    InvestigatorId = execution.InvestigatorId.ToString(),
                    Timestamp = r.Timestamp,
                    Message = r.Message,
                    Payload = r.Payload
                })
                .ToListAsync();

            return new InvestigationDetailDto
            {
                Summary = summary,
                DetailedResults = results
            };
        }

        public async Task<ClearInvestigationsResultDto> ClearAllCompletedInvestigationsAsync()
        {
            _logger.LogInformation("Clearing all completed investigations");

            var resultsDeleted = await _context.InvestigationResults.ExecuteDeleteAsync();
            var executionsDeleted = await _context.InvestigationExecutions.ExecuteDeleteAsync();
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleared {Results} results and {Executions} executions", 
                resultsDeleted, executionsDeleted);

            return new ClearInvestigationsResultDto
            {
                Message = "All investigation results cleared successfully",
                ResultsDeleted = resultsDeleted,
                ExecutionsDeleted = executionsDeleted
            };
        }

        public async Task<DeleteInvestigationResultDto> DeleteInvestigationExecutionAsync(int executionId)
        {
            _logger.LogInformation("Deleting investigation execution {ExecutionId}", executionId);

            var resultsDeleted = await _context.InvestigationResults
                .Where(r => r.ExecutionId == executionId)
                .ExecuteDeleteAsync();
            
            var execution = await _context.InvestigationExecutions.FindAsync(executionId);
            if (execution != null)
            {
                _context.InvestigationExecutions.Remove(execution);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Deleted execution {ExecutionId} with {Results} results", 
                executionId, resultsDeleted);

            return new DeleteInvestigationResultDto
            {
                Message = $"Investigation execution {executionId} deleted successfully",
                ResultsDeleted = resultsDeleted
            };
        }
    }
}
```

### Step B4: Refactor CompletedInvestigationsController
**File**: `src/backend/Controllers/CompletedInvestigationsController.cs`
**Action**: Replace entire file with thin controller

```csharp
using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing completed investigations with service layer architecture.
    /// Delegates business logic to CompletedInvestigationService for better separation of concerns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CompletedInvestigationsController : ControllerBase
    {
        private readonly ICompletedInvestigationService _investigationService;
        private readonly ILogger<CompletedInvestigationsController> _logger;

        public CompletedInvestigationsController(
            ICompletedInvestigationService investigationService,
            ILogger<CompletedInvestigationsController> logger)
        {
            _investigationService = investigationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompleted()
        {
            try
            {
                var completedInvestigations = await _investigationService.GetAllCompletedAsync();
                return Ok(completedInvestigations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed investigations");
                return StatusCode(500, "An error occurred while retrieving completed investigations");
            }
        }

        [HttpGet("{executionId}")]
        public async Task<IActionResult> GetInvestigationDetail(int executionId)
        {
            try
            {
                var detail = await _investigationService.GetInvestigationDetailAsync(executionId);
                if (detail == null)
                {
                    return NotFound($"Investigation execution with ID {executionId} not found.");
                }
                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigation detail for execution {ExecutionId}", executionId);
                return StatusCode(500, "An error occurred while retrieving investigation details");
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllCompletedInvestigations()
        {
            try
            {
                var result = await _investigationService.ClearAllCompletedInvestigationsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing investigation results");
                return StatusCode(500, new { message = "Failed to clear investigation results", error = ex.Message });
            }
        }

        [HttpDelete("{executionId}")]
        public async Task<IActionResult> DeleteInvestigationExecution(int executionId)
        {
            try
            {
                var result = await _investigationService.DeleteInvestigationExecutionAsync(executionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting investigation execution {ExecutionId}", executionId);
                return StatusCode(500, new { message = "Failed to delete investigation execution", error = ex.Message });
            }
        }
    }
}
```

---

## Implementation Plan - Part C: Dependency Injection Registration

### Step C1: Update Program.cs
**File**: `src/backend/Program.cs`
**Location**: Add after line 50 (after existing service registrations)

```csharp
// Register entity-specific service layer (Phase 1)
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.IInvoiceService, ea_Tracker.Services.Implementations.InvoiceService>();
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.IWaybillService, ea_Tracker.Services.Implementations.WaybillService>();

// Phase 3: Register Investigator and Completed Investigation services
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.IInvestigatorAdminService, ea_Tracker.Services.Implementations.InvestigatorAdminService>();
builder.Services.AddScoped<ea_Tracker.Services.Interfaces.ICompletedInvestigationService, ea_Tracker.Services.Implementations.CompletedInvestigationService>();
```

---

## Testing Strategy

### Unit Test Requirements

#### 1. InvestigatorAdminService Tests
Create: `tests/backend/unit/Services/InvestigatorAdminServiceTests.cs`
- Test all CRUD operations
- Test validation logic
- Test error handling
- Mock repositories and mapper

#### 2. CompletedInvestigationService Tests
Create: `tests/backend/unit/Services/CompletedInvestigationServiceTests.cs`
- Test query operations
- Test deletion operations
- Test DTO mapping
- Mock DbContext

#### 3. Controller Tests Update
Update existing controller tests to mock services instead of repositories:
- `tests/backend/unit/ControllerIntegrationTests.cs`
- `tests/backend/unit/ControllerValidationTests.cs`

---

## Migration Checklist

### Pre-Implementation
- [ ] Create feature branch: `feature/phase3-investigator-service-layer`
- [ ] Review existing controller functionality
- [ ] Identify any custom business logic

### Implementation Steps
- [ ] **Part A: InvestigatorController**
  - [ ] Create IInvestigatorAdminService interface
  - [ ] Create InvestigatorAdminService implementation
  - [ ] Update AutoMapperProfile
  - [ ] Refactor InvestigatorController
  - [ ] Update Program.cs registration
  
- [ ] **Part B: CompletedInvestigationsController**
  - [ ] Create ICompletedInvestigationService interface
  - [ ] Create required DTOs
  - [ ] Create CompletedInvestigationService implementation
  - [ ] Refactor CompletedInvestigationsController
  - [ ] Update Program.cs registration

### Testing & Validation
- [ ] Run existing unit tests
- [ ] Create new service layer tests
- [ ] Test all API endpoints manually
- [ ] Verify backward compatibility
- [ ] Performance testing

### Post-Implementation
- [ ] Code review
- [ ] Update API documentation
- [ ] Merge to main branch
- [ ] Deploy to staging environment

---

## Git Commit Strategy

### 📊 **Recommended Commit Structure (7 Atomic Commits)**

This strategy provides logical boundaries, enables easy rollback, and allows for incremental testing and validation.

#### **Commit 1: Documentation & Planning**
**Files**: `docs/PHASE3_INVESTIGATOR_SERVICE_IMPLEMENTATION_PLAN.md`
```bash
git add docs/PHASE3_INVESTIGATOR_SERVICE_IMPLEMENTATION_PLAN.md
git commit -m "docs: add Phase 3 service layer implementation plan

- Complete implementation guide for InvestigatorController refactoring
- Complete implementation guide for CompletedInvestigationsController refactoring  
- Service interfaces, implementations, and DTO definitions
- AutoMapper configurations and DI registration details
- Testing strategy and migration checklist"
```
**Push**: ✅ **Safe to push immediately** (documentation only)

---

#### **Commit 2: InvestigatorAdminService Foundation**
**Files**: 
- `src/backend/Services/Interfaces/IInvestigatorAdminService.cs`
- `src/backend/Services/Implementations/InvestigatorAdminService.cs`

```bash
git add src/backend/Services/Interfaces/IInvestigatorAdminService.cs
git add src/backend/Services/Implementations/InvestigatorAdminService.cs
git commit -m "feat: add InvestigatorAdminService with business logic extraction

- Create IInvestigatorAdminService interface with all CRUD operations
- Implement InvestigatorAdminService with repository pattern
- Extract business logic from InvestigatorController
- Add proper logging and error handling
- Prepare for AutoMapper integration"
```
**Push**: ⏸️ **Hold** (test locally first)

---

#### **Commit 3: AutoMapper Configuration for Investigators**
**Files**: `src/backend/Mapping/AutoMapperProfile.cs`
```bash
git add src/backend/Mapping/AutoMapperProfile.cs
git commit -m "feat: add AutoMapper configurations for investigator DTOs

- Configure InvestigatorInstance to InvestigatorInstanceResponseDto mapping
- Configure CreateInvestigatorInstanceDto to InvestigatorInstance mapping
- Configure UpdateInvestigatorInstanceDto to InvestigatorInstance mapping
- Configure InvestigatorType to InvestigatorTypeDto mapping
- Configure InvestigatorSummary to InvestigatorSummaryDto mapping
- Enable automatic mapping in service layer"
```
**Push**: ⏸️ **Hold**

---

#### **Commit 4: Refactor InvestigatorController** ⭐ **MILESTONE**
**Files**: 
- `src/backend/Controllers/InvestigatorController.cs`
- `src/backend/Program.cs`

```bash
git add src/backend/Controllers/InvestigatorController.cs
git add src/backend/Program.cs
git commit -m "refactor: migrate InvestigatorController to service layer architecture

ACHIEVEMENT: 49% Controller Size Reduction (296 → ~150 lines)
✅ Remove direct repository injection violations
✅ Delegate all business logic to InvestigatorAdminService
✅ Implement clean error handling with ValidationException
✅ Register IInvestigatorAdminService in DI container
✅ Maintain 100% API compatibility

Part A Complete: InvestigatorController now follows established pattern"
```
**Push**: ✅ **PUSH AFTER TESTING** (Part A complete - major milestone)
**Testing**: Verify all `/api/investigator/*` endpoints work correctly

---

#### **Commit 5: CompletedInvestigation DTOs & Service**
**Files**: 
- `src/backend/Models/Dtos/CompletedInvestigationDtos.cs`
- `src/backend/Services/Interfaces/ICompletedInvestigationService.cs`
- `src/backend/Services/Implementations/CompletedInvestigationService.cs`

```bash
git add src/backend/Models/Dtos/CompletedInvestigationDtos.cs
git add src/backend/Services/Interfaces/ICompletedInvestigationService.cs
git add src/backend/Services/Implementations/CompletedInvestigationService.cs
git commit -m "feat: add CompletedInvestigationService with DTO mappings

- Create CompletedInvestigationSummaryDto for clean data transfer
- Create InvestigationDetailDto and InvestigationResultDto
- Create ClearInvestigationsResultDto and DeleteInvestigationResultDto
- Implement ICompletedInvestigationService interface
- Extract complex LINQ queries from controller
- Replace anonymous types with proper DTOs"
```
**Push**: ⏸️ **Hold**

---

#### **Commit 6: Refactor CompletedInvestigationsController** ⭐ **MILESTONE**
**Files**: 
- `src/backend/Controllers/CompletedInvestigationsController.cs`
- `src/backend/Program.cs`

```bash
git add src/backend/Controllers/CompletedInvestigationsController.cs
git add src/backend/Program.cs
git commit -m "refactor: migrate CompletedInvestigationsController to service layer

ACHIEVEMENT: 46% Controller Size Reduction (149 → ~80 lines)
✅ Remove direct DbContext injection violation
✅ Delegate complex queries to CompletedInvestigationService
✅ Replace anonymous types with proper DTOs
✅ Register ICompletedInvestigationService in DI container
✅ Maintain 100% API compatibility

Part B Complete: CompletedInvestigationsController follows established pattern"
```
**Push**: ✅ **PUSH AFTER TESTING** (Part B complete)
**Testing**: Verify all `/api/completedinvestigations/*` endpoints work correctly

---

#### **Commit 7: Tests & Validation**
**Files**: 
- `tests/backend/unit/Services/InvestigatorAdminServiceTests.cs`
- `tests/backend/unit/Services/CompletedInvestigationServiceTests.cs`
- Updated controller tests

```bash
git add tests/backend/unit/Services/InvestigatorAdminServiceTests.cs
git add tests/backend/unit/Services/CompletedInvestigationServiceTests.cs
git add tests/backend/unit/ControllerIntegrationTests.cs
git add tests/backend/unit/ControllerValidationTests.cs
git commit -m "test: add unit tests for investigator and completed investigation services

- Add comprehensive InvestigatorAdminService tests
- Add CompletedInvestigationService tests  
- Update controller tests to mock services instead of repositories
- Verify service layer business logic isolation
- Ensure backward compatibility maintained"
```
**Push**: ✅ **FINAL PUSH** (Complete refactoring with tests)

---

### **🚀 Push Strategy Summary**

**3 Strategic Push Points:**
1. **After Commit 1**: Documentation (immediate - safe)
2. **After Commit 4**: Part A complete (InvestigatorController fully refactored) 
3. **After Commits 6-7**: Part B complete + tests (full Phase 3 done)

### **🛡️ Rollback Safety**
- **If Part A fails**: `git revert HEAD~3..HEAD` (revert commits 2-4)
- **If Part B fails**: `git revert HEAD~2..HEAD` (revert commits 5-6)
- **Individual commit rollback**: Each commit is atomic and independently reversible

### **✅ Testing Checkpoints**
- **After Commit 4**: Full `/api/investigator/*` endpoint testing
- **After Commit 6**: Full `/api/completedinvestigations/*` endpoint testing  
- **After Commit 7**: Complete regression test suite

---

## Expected Outcomes

### Metrics
- **InvestigatorController**: ~296 lines → ~150 lines (49% reduction)
- **CompletedInvestigationsController**: ~149 lines → ~80 lines (46% reduction)
- **Total Code Reduction**: ~445 lines → ~230 lines (48% reduction)

### Architecture Benefits
1. ✅ Complete separation of concerns
2. ✅ Unified architecture across all controllers
3. ✅ Improved testability (mock services vs repositories)
4. ✅ Centralized business logic
5. ✅ Consistent error handling
6. ✅ Better maintainability

### Risk Mitigation
- **Risk**: Breaking existing functionality
- **Mitigation**: Comprehensive testing, maintain API contracts

- **Risk**: Performance degradation
- **Mitigation**: Use AutoMapper projections, optimize queries

- **Risk**: Complex migration
- **Mitigation**: Incremental approach, one controller at a time

---

## Appendix: Command Reference

### Build & Test Commands
```bash
# Build the backend
cd src/backend
dotnet build

# Run tests
dotnet test

# Run specific test file
dotnet test --filter "FullyQualifiedName~InvestigatorAdminServiceTests"
```

### Git Commands
```bash
# Create feature branch
git checkout -b feature/phase3-investigator-service-layer

# Commit with conventional message
git commit -m "refactor: implement service layer for InvestigatorController

- Create IInvestigatorAdminService interface
- Implement InvestigatorAdminService with business logic
- Update AutoMapper configuration for investigator DTOs
- Refactor controller to use service layer
- Achieve ~49% code reduction in controller"
```

---

## Conclusion

This implementation plan provides a complete, step-by-step guide to refactoring the remaining controllers to align with the established service layer architecture. Following this plan will result in:

1. **Architectural Consistency**: All controllers following the same pattern
2. **Code Quality**: ~48% reduction in controller code
3. **Maintainability**: Centralized business logic in service layer
4. **Testability**: Improved unit testing capabilities

The plan is designed to be executed incrementally with minimal risk to existing functionality.