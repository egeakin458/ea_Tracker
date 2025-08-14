using AutoMapper;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Mapping
{
    /// <summary>
    /// AutoMapper profile for configuring entity to DTO mappings.
    /// Centralizes all mapping logic to reduce code duplication in controllers and services.
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            ConfigureInvoiceMappings();
            ConfigureWaybillMappings();
            ConfigureInvestigatorMappings();
        }

        private void ConfigureInvoiceMappings()
        {
            // Create Invoice mappings
            CreateMap<CreateInvoiceDto, Invoice>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.HasAnomalies, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.LastInvestigatedAt, opt => opt.Ignore());

            // Update Invoice mappings
            CreateMap<UpdateInvoiceDto, Invoice>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.HasAnomalies, opt => opt.Ignore())
                .ForMember(dest => dest.LastInvestigatedAt, opt => opt.Ignore());

            // Invoice to Response mapping
            CreateMap<Invoice, InvoiceResponseDto>();
        }

        private void ConfigureWaybillMappings()
        {
            // Create Waybill mappings
            CreateMap<CreateWaybillDto, Waybill>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.HasAnomalies, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.LastInvestigatedAt, opt => opt.Ignore());

            // Update Waybill mappings
            CreateMap<UpdateWaybillDto, Waybill>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.HasAnomalies, opt => opt.Ignore())
                .ForMember(dest => dest.LastInvestigatedAt, opt => opt.Ignore());

            // Waybill to Response mapping
            CreateMap<Waybill, WaybillResponseDto>();
        }

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
            
            // Note: InvestigatorSummaryDto is returned directly from repository
            // No additional mapping needed for summary operations
        }
    }
}