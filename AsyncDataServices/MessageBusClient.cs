using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlatformService.AsyncDataServices;

public class MessageBusClient : IMessageBusClient
{
    private readonly IConfiguration _config;
    private IChannel? _channel;
    private IConnection? _conn;
    private readonly ConnectionFactory _factory;
    
    public MessageBusClient(IConfiguration configuration)
    {
        _config = configuration;
        _factory = new ConnectionFactory() { HostName = _config["RabbitMQHost"]!, Port = int.Parse(_config["RabbitMQPort"]!) };
    }
    public async Task InitAsync()
    {
        try
        {
            _conn = await _factory.CreateConnectionAsync();
            _channel = await _conn.CreateChannelAsync();
            
            await _channel.ExchangeDeclareAsync("trigger", type: ExchangeType.Fanout, durable: false);
            _conn.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdownAsync;

            Console.WriteLine("--> Connected to message bus");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Couldn't connect to message bus. Error {ex.Message}");
        }
    }
    private async Task RabbitMQ_ConnectionShutdownAsync(object sender, ShutdownEventArgs shutdownEventArgs)
    {
        await Task.Delay(1);
        Console.WriteLine("--> RabbitMQ Connection shutdown");
    }

    private async Task SendMessageAsync(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties(); // конкретная реализация вместо интерфейса

        if (_channel is not null)
        {
            await _channel.BasicPublishAsync(
                        exchange: "trigger",
                        routingKey: "",
                        mandatory: false,
                        basicProperties: properties,
                        body: body
                    );

            Console.WriteLine($"--> We have sent a message: {message}");
        }
        else
        {
            Console.WriteLine($"--> Channel is null!");
        }
    }
    public async Task PublishNewPlatformAsync(PlatformPublishedDto platformPublishDto)
    {
        var message = JsonSerializer.Serialize(platformPublishDto);

        if (_conn is not null && _conn.IsOpen)
        {
            await SendMessageAsync(message);
            Console.WriteLine("--> RabbitMQ connection is opened. Sending message ...");
        }
        else
        {
            Console.WriteLine("--> Rabbit connection is closed. Message is not sent!");
        }
    }
    public async Task Dispose()
    {
        Console.WriteLine("--> Message bus is disposed");

        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.CloseAsync();
            await _conn?.CloseAsync()!; 
        }
    }
}
