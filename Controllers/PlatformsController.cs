using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Models;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.SyncDataServices.Http;

namespace PlatformService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlatformsController : ControllerBase
{
    private readonly IPlatformRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICommandDataClient _commandDataClient;
    private readonly IMessageBusClient _messageBusClient;

    public PlatformsController(IPlatformRepo repository, IMapper mapper, ICommandDataClient commandDataClient, IMessageBusClient messageBusClient)
    {
        _repository = repository;
        _mapper = mapper;
        _commandDataClient = commandDataClient;
        _messageBusClient = messageBusClient;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Platform>> GetPlatforms()
    {
        Console.WriteLine("=> Getting platforms ... ({0})", DateTime.Now);

        var platformItems = _repository.GetAllPlatforms();

        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
        var platformItem = _repository.GetPlatformById(id);

        if (platformItem != null)
        {
            return Ok(_mapper.Map<PlatformReadDto>(platformItem));
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto platformCreateDto)
    {
        var platformModel = _mapper.Map<Platform>(platformCreateDto);

        _repository.CreatePlatform(platformModel);
        _repository.SaveChanges();

        var platformReadDto = _mapper.Map<PlatformReadDto>(platformModel);

        // send sync message

        try
        {
            await _commandDataClient.SendPlatformToCommand(platformReadDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> Couldn't send sync request: {ex.Message}");
        }

        // send async message

        try
        {
            var platformPublishedDto = _mapper.Map<PlatformPublishedDto>(platformReadDto);
            platformPublishedDto.Event = "Platform_Published";
            await _messageBusClient.InitAsync();
            await _messageBusClient.PublishNewPlatformAsync(platformPublishedDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"==> Couldn't send async request: {ex.Message}");
        }

        return CreatedAtRoute(nameof(GetPlatformById), new { id = platformReadDto.Id }, platformReadDto);
    }
}