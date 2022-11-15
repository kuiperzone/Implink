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
using System.Net;
using System.Text.Json;
using KuiperZone.Implink.Api;
using KuiperZone.Implink.Gateway;
using KuiperZone.Utility.Yaal;
using KuiperZone.Utility.Yaal.Sinks;
using KuiperZone.Utility.Yaap;

namespace KuiperZone.Implink.Stub;

class Program
{
    // Make these compile time constants
    private const string Stub = "STUB : ";
    private const string RemoteUrl = "https://localhost:39668";
    private const string LocalUrl = "http://localhost:39669";
    private const string TestNameId = "TestNameId";
    private const string AuthFailNameId = "AuthFailNameId";
    private const string TestNameWithCatId = "TestNameWithCatId";
    private const string TestCategory = "TestCategory";

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
        int result = 0;
        var parser = new ArgumentParser(args);

        if (HandleArgsContinue(parser))
        {
            var fopts = new FileSinkOptions();
            fopts.RemoveLogsOnStart = true;
            Logger.Global.AddSink(new FileSink(fopts));

            var copts = new ConsoleSinkOptions();
            copts.Threshold = SeverityLevel.Info;
            Logger.Global.AddSink(new ConsoleSink(copts));

            Logger.Global.Debug($"{Stub}Main: " + parser.ToString());
            Thread? impThread = null;

            try
            {
                var conf = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json").Build();

                var settings = new AppSettings(conf);
                settings.AssertValidity();

                // Remote server verifies athentication
                WriteRoutes(true);
                using var remoteServer = new ImpServer(RemoteUrl, true, new ImpSecret(CreateImpProfile(RemoteUrl, true, false)));

                // Local on LAN, so no authentication
                WriteRoutes(false);
                using var localServer = new ImpServer(LocalUrl, false);

                Logger.Global.Write("Starting Implink");
                impThread = new(StartGateway);
                impThread.Start(parser);

                if (!SpinWait.SpinUntil(() => { return v_impStarted; }, 2000))
                {
                    throw new InvalidOperationException("Failed to start Implink");
                }

                if (!string.IsNullOrEmpty(settings.RemoteTerminatedUrl))
                {
                    // Send to gateway, which will forward to "remote server" and return response
                    Logger.Global.Write(SeverityLevel.Notice, $"{Stub}REMOTE TERMINATED TESTS");
                    RunTests(settings.RemoteTerminatedUrl, true);
                }

                if (!string.IsNullOrEmpty(settings.RemoteOriginatedUrl))
                {
                    // Send to gateway, which will forward to "remote server" and return response
                    Logger.Global.Write(SeverityLevel.Notice, $"{Stub}REMOTE ORIGINATED TESTS");
                    RunTests(settings.RemoteOriginatedUrl, false);
                }
            }
            catch (Exception e)
            {
                Logger.Global.Write(e);
                return 1;
            }
            finally
            {
                impThread?.Interrupt();
            }
        }

        return result;
    }

    private static int RunTests(string gwUrl, bool rt)
    {
        // Should be bi-directional for both RT and RO
        int result = 0;
        gwUrl = gwUrl.Replace("/*:", "/localhost:");
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}GW URL: " + gwUrl);

        string prefix = Stub + (rt ? "RT" : "RO") + " SubmitPost";

        // Create profile directed at Gateway
        var profile = CreateImpProfile(gwUrl, rt, false);
        using var client = new ImpHttpClient(profile, true);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (no Category)");
        var sub = CreateSubmit(rt, false);
        result += AssertOK(client.SubmitPostRequest(sub, out SubmitResponse resp), resp);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (with Category)");
        sub = CreateSubmit(rt, true);
        result += AssertOK(client.SubmitPostRequest(sub, out resp), resp);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (with MsgId)");
        sub = CreateSubmit(rt, false, "MSG1234567890");
        result += AssertOK(client.SubmitPostRequest(sub, out resp), resp, "MSG1234567890");

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (invalid name)");
        sub = CreateSubmit(rt, false);
        sub.GroupId = "InvalidName";
        sub.UserName = "InvalidName";
        result += AssertExpect(client.SubmitPostRequest(sub, out resp), HttpStatusCode.BadRequest, resp);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (invalid category)");
        sub = CreateSubmit(rt, true);
        sub.Category = "InvalidCategory";
        result += AssertExpect(client.SubmitPostRequest(sub, out resp), HttpStatusCode.BadRequest, resp);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (invalid authentication)");
        sub = CreateSubmit(rt, false);
        sub.GroupId = AuthFailNameId;
        sub.UserName = AuthFailNameId;
        result += AssertExpect(client.SubmitPostRequest(sub, out resp), HttpStatusCode.Unauthorized, resp);

        return result;
    }

    private static int AssertExpect(int code, HttpStatusCode exp, SubmitResponse resp)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}StatusCode: " + code + " (" + (HttpStatusCode)code + ")");

        if (code != (int)exp)
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED {exp} {(int)exp} expected, but {code} received");
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}{nameof(resp.ErrorReason)} = {resp.ErrorReason}");

            return 1;
        }

        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}OK");
        return 0;
    }

    private static int AssertOK(int code, SubmitResponse resp, string? expectId = null)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}StatusCode = " + code + " (" + (HttpStatusCode)code + ")");
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}MsgId = " + resp.MsgId);

        if (code != 200)
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - " + resp);
            return 1;
        }

        if (string.IsNullOrWhiteSpace(resp.MsgId))
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - MsgId empty");
            return 1;
        }

        if (expectId != null && expectId != resp.MsgId)
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - Expected MsgId: " + expectId);
            return 1;
        }

        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}OK");
        return 0;
    }

    private static void WriteRoutes(bool rt)
    {
        var addr = RemoteUrl;
        var fname = "./RtRoute.json";

        if (!rt)
        {
            addr = LocalUrl;
            fname = "./RoRoute.json";
        }

        var list = new List<ClientProfile>();
        list.Add(CreateImpProfile(addr, rt, false));
        list.Add(CreateImpProfile(addr, rt, true));

        var temp = CreateImpProfile(addr, rt, false);
        temp.NameId = AuthFailNameId;
        temp.Authentication = $"SECRET=123ABC";
        list.Add(temp);

        var s = JsonSerializer.Serialize(list.ToArray());
        File.WriteAllText(fname, s);
    }

    private static ClientProfile CreateImpProfile(string addr, bool rt, bool hasCategory)
    {
        var p = new ClientProfile();
        p.BaseAddress = addr;

        p.NameId = hasCategory ? TestNameWithCatId : TestNameId;
        p.Categories = hasCategory ? TestCategory : null;

        // Disable for local tests
        p.DisableSslValidation = true;

        // Unique authentication based on values
        p.Authentication = $"SECRET=123{rt.GetHashCode()}";

        p.Api = ClientFactory.ImpV1;
        p.UserAgent = "Implink";
        return p;
    }

    private static SubmitPost CreateSubmit(bool rt, bool hasCategory, string? msgId = null)
    {
        var s = new SubmitPost();
        var name = hasCategory ? TestNameWithCatId : TestNameId;;
        s.GroupId = name;
        s.UserName = name;
        s.Category = hasCategory ? TestCategory : null;
        s.MsgId = msgId;
        s.Text = "Test message";

        return s;
    }

    private static bool HandleArgsContinue(ArgumentParser parser)
    {
        if (parser.GetOrDefault("h", false) || parser.GetOrDefault("help", false))
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("    -h, --help: Help information");
            Console.WriteLine("    -v, --version: Version information");
            return false;
        }

        if (parser.GetOrDefault("v", false) || parser.GetOrDefault("version", false))
        {
            Console.WriteLine(AppInfo.Version);
            return false;
        }

        return true;
    }

    private static void StartGateway(object? _)
    {
        var info = new ProcessStartInfo
        {
            FileName = "Implink.Gateway",
            Arguments = "--forwardWait --directory=./",
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