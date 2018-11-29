using NuGet.Protocol.Core.Types;

namespace LiGet
{
    public interface INuGetClient
    {
        ISourceRepository GetRepository(System.Uri repositoryUrl);
    }
}