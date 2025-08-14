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
            return summary; // Repository already returns DTO
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