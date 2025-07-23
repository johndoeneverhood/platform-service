using PlatformService.Dtos;

namespace PlatformService.AsyncDataServices;

public interface IMessageBusClient
{
    Task PublishNewPlatformAsync(PlatformPublishedDto platformPublishDto);
    Task InitAsync();
}