using NuGet.Protocol.Core.Types;

namespace BaGet.Core
{
    public interface INuGetClient
    {
        ISourceRepository GetRepository(System.Uri repositoryUrl);
    }
}