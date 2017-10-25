using System;
using Autofac;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;

namespace LiGet
{
    public class ProgramBootstrapper : AutofacNancyBootstrapper
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ProgramBootstrapper));
        private Action<ContainerBuilder> additionalSetup;

        public ProgramBootstrapper()
        {            
        }
        public ProgramBootstrapper(Action<ContainerBuilder> additionalSetup)
        {            
            this.additionalSetup = additionalSetup;
        }

        protected override void ApplicationStartup(ILifetimeScope container, IPipelines pipelines)
        {
            // No registrations should be performed in here, however you may
            // resolve things that are needed during application startup.
        }

        protected override void ConfigureApplicationContainer(ILifetimeScope existingContainer)
        {
            // Perform registration that should have an application lifetime
            existingContainer.Update(builder => {
                if(additionalSetup != null)
                    additionalSetup(builder);
            });
        }
    }
}