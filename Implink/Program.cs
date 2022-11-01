// -----------------------------------------------------------------------------
// PROJECT   : Implink
// COPYRIGHT : Andy Thomas (C) 2022
// LICENSE   : AGPL-3.0-or-later
// HOMEPAGE  : https://github.com/kuiperzone/Implink
//
// This file is part of Implink.
//
// Implink is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// Implink is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for
// more details.
//
// You should have received a copy of the GNU Affero General Public License along with Implink.
// If not, see <https://www.gnu.org/licenses/>.
// -----------------------------------------------------------------------------

using KuiperZone.Utility.Yaal;
using KuiperZone.Utility.Yaal.Sinks;
using KuiperZone.Utility.Yaap;

namespace KuiperZone.Implink;

class Program
{
    private static bool RemoteTerminated;

    // REFERENCE
    // ASP WebApp-application:
    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0
    // https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-6.0&tabs=visual-studio-code
    // https://andrewlock.net/exploring-dotnet-6-part-2-comparing-webapplicationbuilder-to-the-generic-host/
    // https://dotnettutorials.net/lesson/asp-net-core-launchsettings-json-file/
    // https://www.adamrussell.com/appsettings-json-in-a-net-core-console-application

    // WebApplication vs HttpListener:
    // https://github.com/dotnet/runtime/issues/63941

    /// <summary>
    /// Traditional and proper Main() function.
    /// </summary>
    public static int Main(string[] args)
    {
        var parser = new ArgumentParser(args);

        if (HandleArgsContinue(parser))
        {
            Thread.CurrentThread.Name = "MAINTHREAD";
#if DEBUG
            var fopts = new FileSinkOptions();
            fopts.RemoveLogsOnStart = true;
            Logger.Global.AddSink(new FileSink(fopts));
            Logger.Global.AddSink(new ConsoleSink());
#endif
            Logger.Global.Write(SeverityLevel.Notice, AppInfo.AppName + " starting");
            Logger.Global.Write(SeverityLevel.Notice, $"args={parser}");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Not using
                builder.Logging.ClearProviders();

                builder.Host.UseSystemd();
                var conf = builder.Configuration;

                var url = GetUrl(conf);
                Logger.Global.Write(SeverityLevel.Notice, $"RemoteTerminated={RemoteTerminated}");
                Logger.Global.Write(SeverityLevel.Notice, $"Url={url}");

                using var database = new RoutingDatabase(conf["DatabaseKind"], conf["DatabaseConnection"]);
                using var app = builder.Build();
                using var gway = new GatewayApp(app, database, RemoteTerminated);

                app.Run(url);
            }
            catch (Exception e)
            {
                Logger.Global.Write(e);
                return 1;
            }
        }

        return 0;
    }

    private static string GetUrl(IConfiguration conf)
    {
        var key = RemoteTerminated ? "RemoteTerminatedUrl" : "RemoteOriginatedUrl";

        var url = conf[key];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException($"{key} undefined in appsettings");
        }

        return url;
    }

    private static bool HandleArgsContinue(ArgumentParser parser)
    {
        if (parser.GetOrDefault("h", false) || parser.GetOrDefault("help", false))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("    -h, --help: Help information");
            Console.WriteLine("    -v, --version: Version information");
            Console.WriteLine("    -o, --remoteOrig: Remote originated");
            return false;
        }

        if (parser.GetOrDefault("v", false) || parser.GetOrDefault("version", false))
        {
            Console.WriteLine(AppInfo.Version);
            return false;
        }

        bool remoteOrig = parser.GetOrDefault("o", false) || parser.GetOrDefault("remoteOrig", false);
        RemoteTerminated = !remoteOrig;

        return true;
    }

}

