using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using LiGet.Cache.Proxy;
using log4net;
using log4net.Config;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Xunit;

namespace LiGet.Tests
{
    public class TestBootstrapper : ProgramBootstrapper
    {
        public static void ConfigureLogging()
        {
            // enough to call it, static constructor will be called exactly once
        }
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        static TestBootstrapper()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            string configText =
@"<log4net>
  <appender name=""ConsoleAppender"" type=""log4net.Appender.ConsoleAppender"">
    <layout type=""log4net.Layout.PatternLayout"">
      <conversionPattern value=""%date [%thread] %-5level %logger - %message%newline"" />
    </layout>
  </appender>
  <appender name=""FileAppender"" type=""log4net.Appender.FileAppender"">
    <file value=""LiGet.Tests.debug.log"" />
    <appendToFile value=""false"" />
    <layout type=""log4net.Layout.PatternLayout"">
        <conversionPattern value=""%date [%thread] %-5level %logger - %message%newline"" />
    </layout>
  </appender>
  <root>
    <level value=""DEBUG"" />
    <appender-ref ref=""FileAppender"" />
    <appender-ref ref=""ConsoleAppender"" />
  </root>
</log4net>";
            using (Stream s = GenerateStreamFromString(configText))
            {
                XmlConfigurator.Configure(logRepository, s);
            }
        }
        private readonly Type nancyModule;

        public TestBootstrapper(Type nancyModule, Action<ContainerBuilder> additionalSetup = null)
            : base(b =>
            {                
                b.RegisterType(nancyModule).As<INancyModule>().As(nancyModule);
                if (additionalSetup != null)
                    additionalSetup(b);
            })
        {
            this.nancyModule = nancyModule;
        }

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
        }

        // in tests we want to test just one module at a time
        protected override IEnumerable<INancyModule> GetAllModules(ILifetimeScope container)
        {
            return new INancyModule[] { (INancyModule)container.Resolve(nancyModule) };
        }
    }
}