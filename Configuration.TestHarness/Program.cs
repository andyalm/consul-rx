using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace ConsulRx.Configuration.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration() //is there value in this or should we just leave it out?
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
