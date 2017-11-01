using System;

namespace LiGet.Cache
{
    public interface INupkgCacheProvider 
    {
         INupkgCacheTransaction OpenTransaction();
    }
}