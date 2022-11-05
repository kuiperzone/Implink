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

            try
            {
                var conf = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json").Build();
#if DEBUG
                var fopts = new FileSinkOptions();
                fopts.RemoveLogsOnStart = true;
                Logger.Global.AddSink(new FileSink(fopts));
                Logger.Global.AddSink(new ConsoleSink());
#endif
                var settings = new AppSettings(conf);

                Logger.Global.Write(SeverityLevel.Notice, AppInfo.AppName + " starting");
                Logger.Global.Write(SeverityLevel.Info, $"args={parser}");

                Logger.Global.Write($"{nameof(settings.DatabaseKind)}={settings.DatabaseKind}");
                Logger.Global.Write($"{nameof(settings.DatabaseConnection)}={settings.DatabaseConnection}");
                Logger.Global.Write($"{nameof(settings.DatabaseRefresh)}={settings.DatabaseRefresh}");
                Logger.Global.Write($"{nameof(settings.ResponseTimeout)}={settings.ResponseTimeout}");
                Logger.Global.Write($"{nameof(settings.RemoteTerminatedUrl)}={settings.RemoteTerminatedUrl}");
                Logger.Global.Write($"{nameof(settings.RemoteOriginatedUrl)}={settings.RemoteOriginatedUrl}");

                var apps = new List<GatewayApp>();

                if (!string.IsNullOrEmpty(settings.RemoteTerminatedUrl))
                {
                    Logger.Global.Debug("Creating RemoteTerminated app");
                    apps.Add(new GatewayApp(args, settings, true));
                }


                if (!string.IsNullOrEmpty(settings.RemoteOriginatedUrl))
                {
                    Logger.Global.Debug("Creating RemoteOriginated app");
                    apps.Add(new GatewayApp(args, settings, false));
                }

                Logger.Global.Write("Running web applications");
                GatewayApp.Run(apps);
            }
            catch (Exception e)
            {
                Logger.Global.Write(e);
                return 1;
            }
        }

        return 0;
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

