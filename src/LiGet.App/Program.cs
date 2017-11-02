using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Hosting;

namespace LiGet.App
{
  public class Program
  {
      private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(typeof(Program));

      private static CancellationTokenSource cts = new CancellationTokenSource();

      static void Console_CancelKeyPress (object sender, ConsoleCancelEventArgs e)
      {
        if (!cts.IsCancellationRequested) {
          cts.Cancel ();
          e.Cancel = true;
        }
      }
      public static void Main(string[] args)
      {
          Console.CancelKeyPress += Console_CancelKeyPress;

          var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
          FileInfo config = null;
          if(File.Exists("/etc/liget/log4net.xml"))
            config = new FileInfo("/etc/liget/log4net.xml");
          else if(File.Exists("debug.log4net.xml"))
            config = new FileInfo("debug.log4net.xml");
          else {
              Console.WriteLine("There is no log4net configuration file. Tried /etc/liget/log4net.xml and debug.log4net.xml");
              Environment.Exit(1);
          }
          XmlConfigurator.Configure(logRepository, config);
          

          try {
            var builder = new WebHostBuilder();
            string threadCount = Environment.GetEnvironmentVariable("LIGET_LIBUV_THREAD_COUNT");
            if(!string.IsNullOrEmpty(threadCount))
              builder.UseLibuv(opts => opts.ThreadCount = int.Parse(threadCount));


            log.InfoFormat("Starting http kestrel host. ServerGC={0}, ThreadCount={1}", GCSettings.IsServerGC, threadCount);
            using(var host = builder
                .UseSetting("detailedErrors", "true")
                .UseUrls("http://0.0.0.0:9011")
                .UseContentRoot(Directory.GetCurrentDirectory())                
                .UseKestrel(options => {
                  // options.ApplicationSchedulingMode = SchedulingMode.ThreadPool;
                  // options.Limits.MaxConcurrentConnections = 8;
                  // options.Limits.MaxConcurrentUpgradedConnections = 8;
                  options.Limits.MaxResponseBufferSize = null;
                  options.Limits.MinRequestBodyDataRate = null;
                  options.Limits.MinResponseDataRate = null;
                  options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
                  
                })
                .UseStartup<Startup>()
                .Build()) {
              host.RunAsync(cts.Token).Wait();
            }
          }
          catch(Exception ex) {
            log.Fatal("Application crashed",ex);
            Console.WriteLine("Application crashed. Exception:\n{0}",ex);
          }
      }
  }
}