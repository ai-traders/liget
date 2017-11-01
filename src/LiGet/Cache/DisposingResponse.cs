using System;
using Nancy;

namespace LiGet.Cache
{
    public class DisposingResponse : Response
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DisposingResponse));

        private readonly IDisposable[] resources;

        public DisposingResponse(params IDisposable[] resources)  {
            this.resources = resources;
        }

        public override void Dispose()
        {
            if(resources != null) {
                foreach(var r in resources) {
                    try {
                        r.Dispose();
                    }
                    catch(Exception ex) {
                        _log.Error("Error disposing resource in response", ex);
                    }
                }
            }
        }
    }
}