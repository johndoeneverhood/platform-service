using Microsoft.EntityFrameworkCore;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.SyncDataServices.Grps;
using PlatformService.SyncDataServices.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("--> Using InMem db");
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMem"));
}
else
{
    Console.WriteLine("--> Using SQL Server");
    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("PlatformsConn")));
}


builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();
builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.MapControllers();
app.MapGrpcService<GrpcPlatformService>();
//app.UseHttpsRedirection();

// Для отдачи .proto-файлов (если нужно разрешить клиентам скачивать их)
// deepseek worked sugestion below
/*app.MapGet("/protos/{protoName}", (string protoName) =>
{
    string protoPath = Path.Combine(Directory.GetCurrentDirectory(), "Protos", protoName);
    if (!File.Exists(protoPath))
        return Results.NotFound();

    return Results.File(protoPath, "text/plain");
});*/

app.MapGet("/protos/platforms.proto", async (context) =>
{
    await context.Response.WriteAsync(File.ReadAllText("Protos/platforms.proto"));
});

PrepDb.PrepPopulation(app, builder.Environment.IsProduction());

Console.WriteLine($"=> Command Service Endpoint {builder.Configuration["CommandService"]}");

app.Run();
