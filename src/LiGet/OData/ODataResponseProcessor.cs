using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiGet.Models;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Nancy;
using Nancy.Responses.Negotiation;

namespace LiGet.OData
{
    public class ODataResponseProcessor : IResponseProcessor
    {
        static readonly MediaRange atomXmlContentType = new MediaRange("application/atom+xml");
        private readonly IODataPackageSerializer serializer;

        public ODataResponseProcessor(IODataPackageSerializer serializer)
        {
            this.serializer = serializer;
        }

        public IEnumerable<Tuple<string, MediaRange>> ExtensionMappings
        {
            get
            {
                return Enumerable.Empty<Tuple<string, MediaRange>>();
            }
        }

        public ProcessorMatch CanProcess(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            var match = new ProcessorMatch();

            var odataPackage = model as ODataResponse<PackageWithUrls>;
            if (odataPackage != null)
            {
                match.ModelResult = MatchResult.ExactMatch;
            }
            var odataPackageCollection = model as ODataResponse<IEnumerable<PackageWithUrls>>;
            if (odataPackageCollection != null)
            {
                match.ModelResult = MatchResult.ExactMatch;
            }

            match.RequestedContentTypeResult = MatchResult.ExactMatch;// requestedMediaRange.Matches(atomXmlContentType) ? MatchResult.ExactMatch : MatchResult.NonExactMatch;
            return match;
        }

        public Response Process(MediaRange requestedMediaRange, dynamic model, NancyContext context)
        {
            var odataPackage = model as ODataResponse<PackageWithUrls>;
            if (odataPackage != null)
            {
                var response = context.Response;
                if (response == null)
                    context.Response = response = new Response();

                response.ContentType = atomXmlContentType;
                response.StatusCode = HttpStatusCode.OK;
                response.Contents = netStream =>
                {
                    serializer.Serialize(netStream, odataPackage.Entity.Pkg, 
                        odataPackage.ServiceBaseUrl, odataPackage.Entity.ResourceIdUrl, odataPackage.Entity.PackageContentUrl);
                };
                return response;
            }
            var odataPackageCollection = model as ODataResponse<IEnumerable<PackageWithUrls>>;
            if (odataPackageCollection != null)
            {
                var response = context.Response;
                if (response == null)
                    context.Response = response = new Response();

                response.ContentType = atomXmlContentType;
                response.StatusCode = HttpStatusCode.OK;
                response.Contents = netStream =>
                {
                    serializer.Serialize(netStream, odataPackageCollection.Entity, odataPackageCollection.ServiceBaseUrl);
                };
                return response;
            }
            throw new InvalidOperationException("invalid model type");
        }
    }
}