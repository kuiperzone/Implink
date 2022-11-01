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

using System.Diagnostics;
using System.Text.Json;
using KuiperZone.Implink.Api;
using KuiperZone.Implink.Api.Imp;
using KuiperZone.Utility.Yaal;
using KuiperZone.Utility.Yaal.Sinks;
using KuiperZone.Utility.Yaap;
using Microsoft.Extensions.Configuration;

namespace KuiperZone.Implink.Stub;

class Program
{
    private const string NameId = "TestNameId";
    private const string RemoteUrl = "https://localhost:39668";
    private static readonly SubmitPost Submit = new();

    private static bool StartImplink;
    private static bool RemoteOriginated;

    private static volatile bool v_impStarted;

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
            var fopts = new FileSinkOptions();
            fopts.RemoveLogsOnStart = true;
            Logger.Global.AddSink(new FileSink(fopts));
            Logger.Global.AddSink(new ConsoleSink());

            Logger.Global.Debug("START STUB: " + parser.ToString());
            Thread? impThread = null;

            try
            {
                var conf = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json").Build();

                var impUrl = GetUrl(conf);
                WriteRTRoutes();

                if (StartImplink)
                {
                    Logger.Global.Debug("Starting Implink");
                    impThread = new(ImpStart);
                    impThread.Start(parser);

                    if (!SpinWait.SpinUntil(() => { return v_impStarted; }, 2000))
                    {
                        throw new InvalidOperationException("Failed to start Implink");
                    }
                }

                var profile = new RouteProfile();
                profile.NameId = NameId;
                profile.ApiKind = ClientFactory.ImpV1;
                profile.BaseAddress = impUrl;

                Logger.Global.Debug("Sending SubmitPost to: " + impUrl);
                var client = new ImpClientSession(profile, true);
                int code = client.SubmitPostRequest(Submit, out SubmitResponse resp);

                Logger.Global.Debug("HTTP Code: " + code);
                Logger.Global.Debug(resp.ToString());


                Thread.Sleep(500);
                return code == 200 ? 0 : 1;
            }
            catch (Exception e)
            {
                Logger.Global.Debug(e);
                return 1;
            }
            finally
            {
                impThread?.Interrupt();
            }
        }

        return 0;
    }

    private static RouteProfile WriteRTRoutes()
    {
        var imp = new RouteProfile();
        imp.NameId = NameId;
        imp.ApiKind = ClientFactory.ImpV1;
        imp.BaseAddress = RemoteUrl;
        imp.Authentication = "PRIVATE=Fyhf$34hjfTh94,PUBLIC=KvBd73!sdL84B";
        imp.UserAgent = "Implink";

        var arr = new RouteProfile[] { imp };
        var s = JsonSerializer.Serialize(arr);

        File.WriteAllText("./RTRoutes.json", s);

        return imp;
    }

    private static string GetUrl(IConfiguration conf)
    {
        var key = RemoteOriginated ? "RemoteOriginatedUrl" : "RemoteTerminatedUrl";

        var url = conf[key];

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException($"{key} undefined in appsettings");
        }

        return url.Replace("/*:", "/localhost:");
    }

    private static bool HandleArgsContinue(ArgumentParser parser)
    {
        if (parser.GetOrDefault("h", false) || parser.GetOrDefault("help", false))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("    -h, --help: Help information");
            Console.WriteLine("    -v, --version: Version information");
            Console.WriteLine("    -o, --remoteOrig: Remote originated");
            Console.WriteLine("    --start: Start Implink platform");
            return false;
        }

        if (parser.GetOrDefault("v", false) || parser.GetOrDefault("version", false))
        {
            Console.WriteLine(AppInfo.Version);
            return false;
        }

        RemoteOriginated = parser.GetOrDefault("o", false) || parser.GetOrDefault("remoteOrig", false);
        StartImplink = parser.GetOrDefault("start", false);

        Submit.NameId = NameId;
        Submit.UserName = "TestUser";
        Submit.Category = "Category";
        Submit.MsgId = "msgid123";
        Submit.Text = "Test message";

        return true;
    }

    private static void ImpStart(object? _)
    {
        var args = RemoteOriginated ? "-o" : "";

        var info = new ProcessStartInfo
        {
            FileName = nameof(Implink),
            Arguments = args,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(info);

        try
        {
            Thread.Sleep(500);
            v_impStarted = true;
            Thread.Sleep(-1);
        }
        catch
        {
        }

        proc?.Kill();
    }

}