using AutoMapper;
using Grpc.Core;
using PlatformService.Data;

namespace PlatformService.SyncDataServices.Grps;

public class GrpcPlatformService : GrpcPlatform.GrpcPlatformBase
{
    private readonly IPlatformRepo _repo;
    private readonly IMapper _mapper;
    public GrpcPlatformService(IPlatformRepo repository, IMapper mapper)
    {
        _repo = repository;
        _mapper = mapper;
    }

    public override Task<PlatformResponse> GetAllPlatforms(GetAllRequest request, ServerCallContext context)
    {
        var response = new PlatformResponse();
        var platforms = _repo.GetAllPlatforms();

        foreach (var plat in platforms)
        {
            response.Platform.Add(_mapper.Map<GrpcPlatformModel>(plat));
        }

        return Task.FromResult(response);
    }
}