// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using Newtonsoft.Json;
using NuGet;
using NuGet.Versioning;

namespace LiGet.NuGet.Server.Infrastructure
{
    public class SerializedServerPackages
    {
        [JsonRequired, JsonConverter(typeof(SemanticVersionJsonConverter))]
        public SemanticVersion SchemaVersion { get; set; }

        [JsonRequired]
        public IEnumerable<ServerPackage> Packages { get; set; }
    }
}