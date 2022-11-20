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
using KuiperZone.Implink.Api.Thirdparty;
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
    private const string TestId = "TestId";
    private const string StubId = "StubId";
    private const string TagId = "TagId";

    private const string AuthFailId = "AuthFailId";
    private const string TestTag = "TestTag";

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

                IReadOnlyAppSettings settings = new AppSettings(conf);
                settings.AssertValidity();

                // Remote server verifies athentication
                WriteRoutesAndClients();
                using var remoteServer = new ImpServer(RemoteUrl, true, new ImpAuthentication(CreateClientProfile("id", true)));

                // Local on LAN, so no authentication
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
                    RunPostMessages(settings.RemoteTerminatedUrl, false);
                }

                if (!string.IsNullOrEmpty(settings.RemoteOriginatedUrl))
                {
                    // Send to gateway, which will forward to "remote server" and return response
                    Logger.Global.Write(SeverityLevel.Notice, $"{Stub}REMOTE ORIGINATED TESTS");
                    RunPostMessages(settings.RemoteOriginatedUrl, true);
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

    private static int RunPostMessages(string gwUrl, bool ro)
    {
        // Should be bi-directional for both RT and RO
        int result = 0;
        gwUrl = gwUrl.Replace("/*:", "/localhost:");
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}GW URL: " + gwUrl);

        string id = (ro ? "RO " : "RT ");
        string prefix = Stub + id + nameof(IMessagingApi.PostMessage);

        // Create profile directed at Gateway
        var profile = CreateClientProfile(id, gwUrl, ro);
        using var client = new ImpClient(profile);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (no Tag)");
        var msg = CreateMessage(TestId, ro);
        result += AssertOK(client.PostMessage(msg));

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (with Tag)");
        msg = CreateMessage(TagId, ro);
        msg.Tag = TagId;
        result += AssertOK(client.PostMessage(msg));

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (with MsgId)");
        msg = CreateMessage(TestId, ro, "MSG1234567890");
        result += AssertOK(client.PostMessage(msg), "MSG1234567890");

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (invalid name)");
        msg = CreateMessage(TestId, ro);
        msg.GroupId = "InvalidName";
        msg.GatewayId = "InvalidName";
        msg.UserName = "InvalidName";
        result += AssertExpect(client.PostMessage(msg), HttpStatusCode.BadRequest);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (invalid tag)");
        msg = CreateMessage(TagId, ro);
        msg.Tag = "InvalidTag";
        result += AssertExpect(client.PostMessage(msg), HttpStatusCode.BadRequest);

        Logger.Global.Write(SeverityLevel.Notice, $"{prefix} (invalid authentication)");
        msg = CreateMessage(AuthFailId, ro);
        result += AssertExpect(client.PostMessage(msg), HttpStatusCode.Unauthorized);

        return result;
    }

    private static int AssertExpect(ImpResponse resp, HttpStatusCode exp)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}StatusCode: " + resp.Status);

        if (resp.Status != exp)
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - {exp} expected, but {resp.Status} received");
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}{nameof(resp.Content)} = {resp.Content}");

            return 1;
        }

        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}OK");
        return 0;
    }

    private static int AssertOK(ImpResponse resp, string? expectId = null)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}Status = " + resp.Status);
        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}Content = " + resp.Content);

        if (resp.Status != HttpStatusCode.OK)
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - " + resp);
            return 1;
        }

        if (string.IsNullOrWhiteSpace(resp.Content))
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - Content empty");
            return 1;
        }

        if (expectId != null && expectId != resp.Content)
        {
            Logger.Global.Write(SeverityLevel.Error, $"{Stub}FAILED - Expected MsgId: " + expectId);
            return 1;
        }

        Logger.Global.Write(SeverityLevel.Notice, $"{Stub}OK");
        return 0;
    }

    private static string GetId(string id, bool ro)
    {
        return id + (ro ? "-ro" : "-rt");
    }

    private static string GetId(string id, bool ro, out string addr)
    {
        if (ro)
        {
            addr = LocalUrl;
            return id + "-ro";
        }

        addr = RemoteUrl;
        return id + "-rt";
    }

    private static void WriteRoutesAndClients()
    {
        var clist = new List<NamedClientProfile>();

        var c = CreateClientProfile(TestId, false);
        clist.Add(c);

        c = CreateClientProfile(TagId, false);
        clist.Add(c);

        c = CreateClientProfile(StubId, false);
        c.Kind = ClientKind.Stub;
        clist.Add(c);

        c = CreateClientProfile(TestId, true);
        clist.Add(c);

        c = CreateClientProfile(TagId, true);
        clist.Add(c);

        c = CreateClientProfile(StubId, true);
        c.Kind = ClientKind.Stub;
        clist.Add(c);

        var s = JsonSerializer.Serialize(clist.ToArray());
        File.WriteAllText("./" + ProfileDatabase.ClientTable + ".json", s);


        var rlist = new List<RouteProfile>();

        var r = CreateRouteProfile(false, false);
        rlist.Add(r);

        r = CreateRouteProfile(false, true);
        rlist.Add(r);

        r = CreateRouteProfile(true, false);
        rlist.Add(r);

        r = CreateRouteProfile(true, true);
        rlist.Add(r);

        r = new RouteProfile();
        r.IsRemoteOriginated = true;
        r.Id = GetId(AuthFailId, r.IsRemoteOriginated);
        r.Secret = $"SECRET=InvalidHdueeet";
        r.Clients = GetId(TestId, r.IsRemoteOriginated);
        rlist.Add(r);

        r = new RouteProfile();
        r.IsRemoteOriginated = false;
        r.Id = GetId(AuthFailId, r.IsRemoteOriginated);
        r.Secret = $"SECRET=InvalidHdueeet";
        r.Clients = GetId(TestId, r.IsRemoteOriginated);
        rlist.Add(r);

        s = JsonSerializer.Serialize(rlist.ToArray());
        File.WriteAllText("./" + ProfileDatabase.RouteTable + ".json", s);
    }

    private static NamedClientProfile CreateClientProfile(string id, string addr, bool ro)
    {
        var p = CreateClientProfile(id, ro);
        p.BaseAddress = addr;
        return p;
    }

    private static NamedClientProfile CreateClientProfile(string id, bool ro)
    {
        var p = new NamedClientProfile();
        p.Id = GetId(TestId, false, out string addr);
        p.BaseAddress = addr;

        // Disable for local tests
        p.DisableSslValidation = true;

        // Unique authentication based on values
        p.Secret = $"SECRET=123{ro.GetHashCode()}";

        p.Kind = ClientKind.ImpV1;
        p.UserAgent = "Implink";
        return p;
    }

    private static RouteProfile CreateRouteProfile(bool ro, bool hasTag)
    {
        var p = new RouteProfile();
        p.IsRemoteOriginated = ro;
        p.Secret = $"SECRET=123{ro.GetHashCode()}";

        if (hasTag)
        {
            var id = GetId(TagId, ro);
            p.Id = id;
            p.Clients = id;
            p.Tags = "T1," + id + ",T3";
        }
        else
        {
            var id = GetId(TestId, ro);

            p.Id = id;
            p.Clients = id + "," + GetId(StubId, ro) + ",InvalidClient";
        }

        return p;
    }

    private static ImpMessage CreateMessage(string id, bool ro, string? msgId = null)
    {
        var msg = new ImpMessage();
        msg.GroupId = id;
        msg.GatewayId = id;
        msg.UserName = id;
        msg.MsgId = msgId;
        msg.Text = "Test message";

        return msg;
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