using System;
using System.Collections.Generic;
using System.Linq;
using BaGet.Web.Extensions;
using BaGet.Web.Models;
using Carter;
using Newtonsoft.Json;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using BaGet.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace BaGet.Web.Controllers
{
    /// <summary>
    /// The NuGet Service Index. This aids NuGet client to discover this server's services.
    /// </summary>
    public class CacheIndexModule : CarterModule
    {
        string prefix = "cache";

        private IEnumerable<ServiceResource> ServiceWithAliases(string name, string url, params string[] versions)
        {
            foreach (var version in versions)
            {
                string fullname = string.IsNullOrEmpty(version) ? name : name + "/" + version;
                yield return new ServiceResource(fullname, url);
            }
        }

        public CacheIndexModule(LiGetCompatibilityOptions compat) {
            Func<HttpRequest, HttpResponse, RouteData, Task> indexHandler = async (req, res, routeData) =>           
            {
                await res.AsJson(new
                {
                    Version = "3.0.0",
                    Resources =
                        ServiceWithAliases("PackagePublish", req.PackagePublish(prefix), "2.0.0") // api.nuget.org returns this too.
                        .Concat(ServiceWithAliases("SearchQueryService", req.PackageSearch(prefix), "", "3.0.0-beta", "3.0.0-rc")) // each version is an alias of others
                        .Concat(ServiceWithAliases("RegistrationsBaseUrl", req.RegistrationsBase(prefix), "", "3.0.0-rc", "3.0.0-beta"))
                        .Concat(ServiceWithAliases("PackageBaseAddress", req.PackageBase(prefix), "3.0.0"))
                        .Concat(ServiceWithAliases("SearchAutocompleteService", req.PackageAutocomplete(prefix), "", "3.0.0-rc", "3.0.0-beta"))
                        .ToList()
                });
            };
            this.Get(prefix + "/v3/index.json", indexHandler);
            if(compat != null && compat.Enabled) {
                this.Get("/api/" + prefix + "/v3/index.json", indexHandler);
            }
        }
    }
}
