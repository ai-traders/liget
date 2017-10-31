using System.Collections.Generic;
using System.IO;

namespace LiGet.Cache.Proxy
{
    public interface IV3JsonInterceptor
    {
        void Intercept(Dictionary<string, string> valueReplacements, Stream input, Stream output);
    }
}