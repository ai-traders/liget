using Microsoft.AspNetCore.Builder;
using Nancy.Owin;

namespace LiGet.App
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseOwin(x => x.UseNancy(o => o.Bootstrapper = new ProgramBootstrapper()));
        }
    }
}