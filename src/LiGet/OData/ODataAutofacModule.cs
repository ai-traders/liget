using Autofac;
using Microsoft.OData.Edm;

namespace LiGet.OData
{
    public class ODataAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder) {
            var odataModelBuilder = new NuGetWebApiODataModelBuilder();
            odataModelBuilder.Build();
            builder.RegisterInstance(odataModelBuilder.Model).As<IEdmModel>();
        }
    }
}