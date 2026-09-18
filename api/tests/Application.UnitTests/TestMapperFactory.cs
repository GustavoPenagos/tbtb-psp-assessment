using Application.Mappings;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests;

public static class TestMapperFactory
{
    private static readonly IMapper _mapper;

    static TestMapperFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    public static IMapper Create() => _mapper;
}
