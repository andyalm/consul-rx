using ConsulRx.Configuration;
using Microsoft.AspNetCore.Builder;

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
var app = builder.Build();
app.UseEndpoints(e =>
{
    e.MapControllers();
});

app.Run();

