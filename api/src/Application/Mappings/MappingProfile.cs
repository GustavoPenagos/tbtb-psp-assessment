using AutoMapper;
using Application.DTOs;
using Domain.Entities;

namespace Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientResponseDto>();
        CreateMap<PatientRegisterRequestDto, Patient>();

        CreateMap<Contact, ContactResponseDto>();
        CreateMap<ContactCreateRequestDto, Contact>();

        CreateMap<RegistrationLink, RegistrationLinkResponseDto>();

        CreateMap<ContactAuditLog, AuditLogResponseDto>()
            .ForMember(dest => dest.EntityId, opt => opt.MapFrom(src => src.ContactId));

        CreateMap<PatientAuditLog, AuditLogResponseDto>()
            .ForMember(dest => dest.EntityId, opt => opt.MapFrom(src => src.PatientId));
    }
}