using ConsulRx.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddConsul(consul =>
{
    consul
        .AutoUpdate()
        .MapHttpService("service1-http", "serviceEndpoints:service1")
        .MapHttpService("service2-http", "serviceEndpoints:service2")
        .MapKeyPrefix("apps/harness", "consul")
        .MapKey("shared/feature1", "features:feature1");
});
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();

app.Run();

