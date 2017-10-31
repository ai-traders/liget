using System.IO;
using Autofac;
using LiGet.Cache.Proxy;
using LiGet.NuGet.Server.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Nancy.Owin;

namespace LiGet.App
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {            
            app.UseOwin(x => x.UseNancy(o => {
                o.Bootstrapper = new ProgramBootstrapper(builder => {
                    var configuration = new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .Build();
                    var config = new LiGetEnvironmentConfig(configuration);                   
                    builder.RegisterModule(new NuGetServerAutofacModule(config));
                    builder.RegisterModule(new CachingProxyAutofacModule(config));
                });
            }));
        }
    }
}