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

using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Text;
using KuiperZone.Implink.Api;
using KuiperZone.Implink.Api.Util;
using KuiperZone.Utility.Yaal;
using Microsoft.Extensions.Primitives;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Wraps a server and collection of a clients into an "application class".
/// </summary>
public class GatewayApp : IDisposable, IAsyncDisposable
{
    private readonly object _syncObj = new();
    private readonly ProfileDatabase _database;
    private readonly RouterDictionary _routers;

    private readonly Thread? _thread;
    private readonly WebApplication _app;


    private volatile bool v_disposed;

    /// <summary>
    /// Constructor.
    /// </summary>
    public GatewayApp(string[] args, IReadOnlyAppSettings settings, bool remoteOriginated)
    {
        Settings = settings;
        IsRemoteOriginated = remoteOriginated;

        if (IsRemoteOriginated)
        {
            DirectionName = "RemoteOriginated";
            ServerUrl = Settings.RemoteOriginatedUrl;
        }
        else
        {
            DirectionName = "RemoteTerminated";
            ServerUrl = Settings.RemoteTerminatedUrl;
        }

        Logger.Global.Debug($"{nameof(DirectionName)}={DirectionName}");
        Logger.Global.Debug($"{nameof(ServerUrl)}={ServerUrl}");

        if (string.IsNullOrEmpty(ServerUrl))
        {
            throw new ArgumentException($"Undefined in {nameof(ServerUrl)} appsettings");
        }

        var builder = WebApplication.CreateBuilder(args);

        // Not using
        builder.Logging.ClearProviders();

        builder.Host.UseSystemd();
        Logger.Global.Debug("Creating database");
        _database = new(Settings.DatabaseKind, Settings.DatabaseConnection);

        Logger.Global.Debug("Creating routes");
        _routers = new(Settings.WaitOnForward);
        RefreshRoutesNoSync();

        Logger.Global.Debug("Building application");
        _app = builder.Build();

        // https://www.koderdojo.com/blog/asp-net-core-routing-and-routebuilder-mapget-for-calculating-a-factorial
        _app.MapPost("/" + nameof(IMessagingApi.PostMessage), PostMessageHandler);
        _app.MapGet("/GetTime", GetTimeHandler);

        if (!IsRemoteOriginated)
        {
            // Local only
            _app.MapGet("/GetRoutingInfo", GetRoutingInfoHandler);
        }

        if (Settings.DatabaseRefresh > TimeSpan.Zero)
        {
            Logger.Global.Debug($"Starting {nameof(GatewayApp)} thread");
            _thread = new(DatabaseThread);
            _thread.Name = "Database" + (IsRemoteOriginated ? "Local" : "Remote");
            _thread.IsBackground = true;
            _thread.Start();
       }
    }

    /// <summary>
    /// Gets whether gateway is remote-originated.
    /// </summary>
    public bool IsRemoteOriginated { get; }

    /// <summary>
    /// Gets the direction name, i.e. "RemoteOriginated" or "RemoteTerminated".
    /// </summary>
    public string DirectionName { get; }

    /// <summary>
    /// Gets the routing information refresh interval.
    /// </summary>
    public IReadOnlyAppSettings Settings { get; }

    /// <summary>
    /// Gets the server URL.
    /// </summary>
    public string ServerUrl { get; }

    /// <summary>
    /// Runs all applications and disposes all instances on return.
    /// </summary>
    public static void RunAll(IEnumerable<GatewayApp> apps)
    {
        Logger.Global.Debug("Running applications");
        var tasks = new List<Task>();

        try
        {
            foreach (var item in apps)
            {
                tasks.Add(item.RunAsync());
            }

            if (tasks.Count != 0)
            {
                Task.WaitAny(tasks.ToArray());
            }
            else
            {
                Logger.Global.Write(SeverityLevel.Warning, "Nothing to run");
            }
        }
        finally
        {
            tasks.Clear();

            foreach (var item in apps)
            {
                tasks.Add(item.DisposeAsync().AsTask());
            }

            Task.WaitAll(tasks.ToArray(), 5000);
        }
    }

    /// <summary>
    /// Runs the application.
    /// </summary>
    public void Run()
    {
        _app.Run(ServerUrl);
    }

    /// <summary>
    /// Runs the application asynchronously.
    /// </summary>
    public Task RunAsync()
    {
        return _app.RunAsync(ServerUrl);
    }

    /// <summary>
    /// Implements dispose.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().Wait(Settings.ResponseTimeout);
    }

    /// <summary>
    /// Implements dispose.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Logger.Global.Debug("Disposal");

        v_disposed = true;
        _thread?.Interrupt();

        var t = _app.DisposeAsync();
        _database.Dispose();

        return t;
    }

    private MessageRouter? GetRoute(string? id)
    {
        lock (_syncObj)
        {
            if (id != null && _routers.TryGetValue(id, out MessageRouter? route))
            {
                return route;
            }

            return null;
        }
    }

    private string GetRoutingInfo(bool refresh, bool indent = true)
    {
        lock (_syncObj)
        {
            if (refresh)
            {
                try
                {
                    RefreshRoutesNoSync();
                }
                catch (Exception e)
                {
                    var sb = new StringBuilder();

                    sb.Append($"Failed to refresh routes from database: {_database.Connection}");
                    Logger.Global.Write(SeverityLevel.Error, sb.ToString());

                    sb.AppendLine();
                    sb.Append(e.Message);

                    Logger.Global.Write(e);
                    return sb.ToString();
                }
            }

            return GetRoutingInfoNoSync(indent);
        }
    }

    private void RefreshRoutes()
    {
        lock (_syncObj)
        {
            try
            {
                RefreshRoutesNoSync();
            }
            catch
            {
            }
        }
    }

    private string RefreshRoutesNoSync()
    {
        try
        {
            var sb = new StringBuilder();

            Logger.Global.Debug("Query data for clients");
            var clients = _database.QueryClients();

            foreach (var item in clients)
            {

            }


            var oldClients = _routers.Clients.UpsertMany();

            foreach (var item in oldClients)
            {
                // We could dispose of clients here,
                // but existing routes may be using them.
                // GOING TO LEAVE THEM TO GARBAGE COLLECTOR
                Logger.Global.Write(SeverityLevel.Notice, $"Deprovisioned client {item.Profile.Id}");
            }

            Logger.Global.Debug("Query data for routes");
            var oldRoutes = _routers.UpsertMany(_database.QueryRoutes(IsRemoteOriginated));

            foreach (var item in oldRoutes)
            {
                Logger.Global.Write(SeverityLevel.Notice, $"Deprovisioned route {item.Profile.Id}");
            }


            foreach (var route in _routers.Values)
            {
                var clients = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (var item in route.Clients)
                {
                    clients.Add(item.Profile.Id);
                }

                foreach (var item in StringParser.ToSet(route.Profile.Clients))
                {
                    if (clients.Add(item))
                    {
                        Logger.Global.Write(SeverityLevel.Warning, $"{item} - WARNING : Client not provisioned");
                    }
                }
            }
        }
        catch (Exception e)
        {
            var sb = new StringBuilder($"Failed to read from database: {_database.Connection}");
            Logger.Global.Write(SeverityLevel.Error, sb.ToString());
            Logger.Global.Write(e);

            sb.AppendLine();
            sb.Append(e.Message);

            return sb.ToString();
        }
    }

    private string GetRoutingInfoNoSync(bool indent)
    {
        var sb = new StringBuilder();

        foreach (var items in _routers)
        {
            if (sb.Length != 0)
            {
                sb.AppendLine(",");
            }

            sb.Append(items.ToString());
        }

        return sb.ToString();
    }

    private Task GetTimeHandler(HttpContext ctx)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"RECEIVED: {DirectionName}.GetTime on {ServerUrl}");

        var resp = new ImpResponse(DateTime.UtcNow.ToString("O"));
        return WriteResponseAsync(ctx.Response, resp);
    }

    private Task GetRoutingInfoHandler(HttpContext ctx)
    {
        var resp = new ImpResponse();
        Logger.Global.Write(SeverityLevel.Notice, $"RECEIVED: {DirectionName}.GetRouteInfo on {ServerUrl}");

        try
        {
            var temp = ctx.Request.Query["refresh"].ToString();

            Logger.Global.Debug("Refresh: {temp}");
            resp.Content = GetRoutingInfo(temp.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Failed to refresh routes from database: {_database.Connection}");
            sb.Append(e.Message);
            resp.Content = sb.ToString();
        }

        return WriteResponseAsync(ctx.Response, resp);
    }

    private async Task<Task> PostMessageHandler(HttpContext ctx)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"RECEIVED: {DirectionName}.{nameof(IMessagingApi.PostMessage)} on {ServerUrl}");

        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, false);

        var body = await reader.ReadToEndAsync();
        Logger.Global.Write(SeverityLevel.Info, body);

        // Must deserialize before we can route and authenticate
        var request = Jsonizable.Deserialize<ImpMessage>(body);

        if (IsRemoteOriginated && string.IsNullOrWhiteSpace(request.GatewayId))
        {
            return WriteResponseAsync(ctx.Response, new ImpResponse(HttpStatusCode.BadRequest, $"{nameof(request.GatewayId)} mandatory for {DirectionName}"));
        }

        var id = (IsRemoteOriginated ? request.GatewayId : request.GroupId) ?? "";
        Logger.Global.Write(SeverityLevel.Info, $"Routing ID: {id}");

        var route = GetRoute(id);

        if (route == null)
        {
            return WriteResponseAsync(ctx.Response, new ImpResponse(HttpStatusCode.BadRequest, $"No route for {id}"));
        }

        return WriteResponseAsync(route, ctx.Response, route.PostMessage(ctx.Request.Headers, body, request));
    }

    private Task WriteResponseAsync(HttpResponse httpResp, ImpResponse impResp)
    {
        var body = impResp.ToString();
        Logger.Global.Write(SeverityLevel.Notice, "RESPONSE: " + body);

        httpResp.StatusCode = (int)impResp.Status;
        httpResp.ContentType = MediaTypeNames.Application.Json;
        httpResp.ContentLength = Encoding.UTF8.GetByteCount(body);

        Logger.Global.Debug($"Writing response");
        return httpResp.WriteAsync(body, new CancellationTokenSource(Settings.ResponseTimeout).Token);
    }

    private Task WriteResponseAsync(MessageRouter route, HttpResponse httpResp, ImpResponse impResp)
    {
        var rate = route.Counter.Rate;
        var limit = route.Counter.MaxRate;
        httpResp.Headers.Add("CURRENT_RATE", rate.ToString(CultureInfo.InvariantCulture));
        httpResp.Headers.Add("RATE_LIMIT", rate.ToString(CultureInfo.InvariantCulture));
        return WriteResponseAsync(httpResp, impResp);
    }

    private void DatabaseThread(object? _)
    {
        Logger.Global.Debug("Starting thread");

        while (!v_disposed)
        {
            try
            {
                Thread.Sleep(Settings.DatabaseRefresh);

                if (!v_disposed)
                {
                    lock (_syncObj)
                    {
                        RefreshRoutesNoSync();
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
                Logger.Global.Write(e.Message);
            }
            catch (Exception e)
            {
                Logger.Global.Write(SeverityLevel.Error, "Failed to read from database");
                Logger.Global.Write(e.ToString());
            }
        }
    }

}