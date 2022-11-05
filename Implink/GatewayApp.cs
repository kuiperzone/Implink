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

using System.Net.Mime;
using System.Text;
using KuiperZone.Implink.Api;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink;

/// <summary>
/// Manages a collection of client sessions.
/// </summary>
public class GatewayApp : IDisposable, IAsyncDisposable
{
    private readonly Thread? _thread;
    private readonly WebApplication _app;
    private readonly SessionManager _manager;
    private readonly RoutingDatabase _database;
    private volatile bool v_disposed;

    /// <summary>
    /// Constructor. Does not dispose of app or database.
    /// </summary>
    public GatewayApp(string[] args, IReadOnlyAppSettings settings, bool remoteTerminated)
    {
        IsRemoteTerminated = remoteTerminated;
        Logger.Global.Debug($"{nameof(IsRemoteTerminated)}={IsRemoteTerminated}");

        Settings = settings;
        ServerUrl = IsRemoteTerminated ? Settings.RemoteTerminatedUrl : Settings.RemoteOriginatedUrl;
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
        _database = new(Settings.DatabaseKind, Settings.DatabaseConnection, IsRemoteTerminated);

        Logger.Global.Debug("Creating sessions");
        _manager = new(_database.QueryAllRoutes(), IsRemoteTerminated);

        Logger.Global.Debug("Building application");
        _app = builder.Build();

        // https://www.koderdojo.com/blog/asp-net-core-routing-and-routebuilder-mapget-for-calculating-a-factorial
        _app.MapPost("/" + nameof(SubmitPost), SubmitPostHandler);

        if (Settings.DatabaseRefresh > TimeSpan.Zero)
        {
            Logger.Global.Debug($"Starting {nameof(GatewayApp)} thread");
            _thread = new(RunThread);
            _thread.Name = nameof(GatewayApp) + "-" + (IsRemoteTerminated ? "Remote" : "Local");
            _thread.IsBackground = true;
            _thread.Start();
        }
    }

    /// <summary>
    /// Gets whether remote terminated (or local terminated).
    /// </summary>
    public bool IsRemoteTerminated { get; }

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
    public static void Run(IEnumerable<GatewayApp> apps)
    {
        Logger.Global.Debug("Running applications");
        var tasks = new List<Task>();

        try
        {
            foreach (var item in apps)
            {
                tasks.Add(item.RunAsync());
            }

            Task.WaitAny(tasks.ToArray());
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
        _manager.Dispose();
        return t;
    }

    private async Task<Task> SubmitPostHandler(HttpContext ctx)
    {
        try
        {
            Logger.Global.Write(SeverityLevel.Notice, $"{nameof(SubmitPost)} received on TBD");
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, false);

            var body = await reader.ReadToEndAsync();
            Logger.Global.Write(SeverityLevel.Info, body);

            var submit = JsonSerializable.Deserialize<SubmitPost>(body);
            var code = _manager.SubmitPostRequest(submit, out SubmitResponse response);

            Logger.Global.Write(SeverityLevel.Notice, $"Response code: {code}");

            if (!string.IsNullOrEmpty(response.ErrorReason))
            {
                Logger.Global.Write(SeverityLevel.Notice, response.ErrorReason);
            }

            return WriteResponseAsync(ctx.Response, code, response.ToString());
        }
        catch (Exception e)
        {
            Logger.Global.Write(e);
            throw;
        }
    }

    private Task WriteResponseAsync(HttpResponse resp, int code, string? body)
    {
        body ??= string.Empty;
        resp.StatusCode = code;
        resp.ContentType = MediaTypeNames.Application.Json;
        resp.ContentLength = Encoding.UTF8.GetByteCount(body);

        Logger.Global.Debug($"Writing response");
        return resp.WriteAsync(body, new CancellationTokenSource(Settings.ResponseTimeout).Token);
    }

    private void RunThread(object? _)
    {
        Logger.Global.Debug("Starting thread");

        while (!v_disposed)
        {
            try
            {
                Thread.Sleep(Settings.DatabaseRefresh);

                if (!v_disposed)
                {
                    Logger.Global.Debug($"Query all routes from database");
                    _manager.Reload(_database.QueryAllRoutes());
                }
            }
            catch (Exception e)
            {
                Logger.Global.Write(SeverityLevel.Error, "Failed to read from database");
                Logger.Global.Write(e);
            }
        }
    }

}