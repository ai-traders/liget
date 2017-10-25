using System.Collections.Generic;
using System.Linq;
using Autofac;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Xunit;

namespace LiGet.Tests
{
    public class TestBootstrapper : ProgramBootstrapper
    {
        //PATCH: Force nancy modules to register, autodiscovery seems to fail when testing
        public TestBootstrapper()
            :base(b => {
                b.RegisterType<PackagesNancyModule>().As<INancyModule>();
            })
        {            
        }
    }
}