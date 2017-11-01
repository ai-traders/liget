using System;

namespace LiGet.Cache
{
    public interface INupkgCacheProvider 
    {
         ICacheTransaction OpenTransaction();
    }
}