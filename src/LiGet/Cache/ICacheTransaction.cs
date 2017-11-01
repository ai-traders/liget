using System;
using System.Threading.Tasks;

namespace LiGet.Cache
{
    public interface ICacheTransaction : IDisposable
    {
         byte[] TryGet(string package, string version);

         void Insert(string package, string version, byte[] value);
    }
}