using System;
using Microsoft.AspNetCore.OData.Builder;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace BaGet.Core.Legacy.OData
{
    public class NuGetWebApiODataModelBuilder
    {
        private IEdmModel model;

        public IEdmModel Model
        {
            get
            {
                if (model == null)
                {
                    throw new InvalidOperationException("Must invoke Build method before accessing Model.");
                }
                return model;
            }
        }

        public void Build()
        {
            var builder = new ODataConventionModelBuilder();

            var entity = builder.EntitySet<ODataPackage>("Packages");
            entity.EntityType.HasKey(pkg => pkg.Id);
            entity.EntityType.HasKey(pkg => pkg.Version);

            var searchAction = builder.Action("Search");
            searchAction.Parameter<string>("searchTerm");
            searchAction.Parameter<string>("targetFramework");
            searchAction.Parameter<bool>("includePrerelease");
            searchAction.ReturnsCollectionFromEntitySet<ODataPackage>("Packages");

            var findPackagesAction = builder.Action("FindPackagesById");
            findPackagesAction.Parameter<string>("id");
            findPackagesAction.ReturnsCollectionFromEntitySet<ODataPackage>("Packages");

            var getUpdatesAction = builder.Action("GetUpdates");
            getUpdatesAction.Parameter<string>("packageIds");
            getUpdatesAction.Parameter<bool>("includePrerelease");
            getUpdatesAction.Parameter<bool>("includeAllVersions");
            getUpdatesAction.Parameter<string>("targetFrameworks");
            getUpdatesAction.Parameter<string>("versionConstraints");
            getUpdatesAction.ReturnsCollectionFromEntitySet<ODataPackage>("Packages");

            model = builder.GetEdmModel();
        }
    }
}
