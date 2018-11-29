using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiGet.Services;
using LiGet.Extensions;
using LiGet.WebModels;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using Newtonsoft.Json;

namespace LiGet.CarterModules
{
    public class SearchModule : CarterModule
    {
        private readonly ISearchService _searchService;

        public SearchModule(ISearchService searchService)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));

            this.Get("/v3/search", async (req, res, routeData) => {
                var query = req.Query.As<string>("q");
                query = query ?? string.Empty;

                var results = await _searchService.SearchAsync(query);
                await res.AsJson(new
                {
                    TotalHits = results.Count,
                    Data = results.Select(p => new SearchResultModel(p, req, ""))
                });
            });

            this.Get("/v3/autocomplete", async (req, res, routeData) => {
                var query = req.Query.As<string>("q");
                query = query ?? string.Empty;

                var results = await _searchService.AutocompleteAsync(query);

                await res.AsJson(new
                {
                    TotalHits = results.Count,
                    Data = results,
                });
            });
        }
    }
}