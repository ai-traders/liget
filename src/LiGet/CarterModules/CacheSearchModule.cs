using System.Linq;
using System.Threading;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using LiGet.Cache;
using LiGet.Configuration;
using LiGet.WebModels;
using NuGet.Protocol.Core.Types;

namespace LiGet.CarterModules
{
    public class CacheSearchModule : CarterModule
    {
        const string prefix = "api/cache";

        public CacheSearchModule(ICacheService cacheService) {
            this.Get("/api/cache/v3/search", async (req, res, routeData) => {
                var query = req.Query.As<string>("q");
                int? skip = req.Query.As<int?>("skip");
                int? take = req.Query.As<int?>("take");
                bool? prerelease = req.Query.As<bool?>("prerelease");
                string semVerLevel = req.Query.As<string>("semVerLevel");
                query = query ?? string.Empty;

                var results = await cacheService.SearchAsync(query, new SearchFilter(prerelease ?? false), skip ?? 0, take ?? 0, CancellationToken.None);
                await res.AsJson(new
                {
                    TotalHits = results.Count(),
                    Data = results.Select(r => new SearchResultModel(r, r.GetVersionsAsync().Result, req, prefix))
                });
            });
        }
    }
}