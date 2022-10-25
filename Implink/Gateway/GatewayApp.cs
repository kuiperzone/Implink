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
using KuiperZone.Implink.Database;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Manages a collection of client sessions.
/// </summary>
public class GatewayApp : IDisposable
{
    private readonly Thread? _thread;
    private readonly SessionManager _manager;
    private readonly RoutingDatabase _database;
    private volatile bool v_disposed;

    /// <summary>
    /// Constructor. Intervalue is Does not dispose of app or database.
    /// </summary>
    public GatewayApp(WebApplication app, RoutingDatabase database, bool remoteTerminated)
    {
        IsRemoteTerminated = remoteTerminated;
        Logger.Global.Debug($"{nameof(IsRemoteTerminated)}={IsRemoteTerminated}");

        var conf = app.Configuration;
        RefreshInterval = TimeSpan.FromSeconds(conf.GetValue<int>(nameof(RefreshInterval), 60));
        Logger.Global.Debug($"{nameof(RefreshInterval)}={RefreshInterval}");

        ResponseTimeout = TimeSpan.FromMilliseconds(conf.GetValue<int>(nameof(ResponseTimeout), 15000));
        Logger.Global.Debug($"{nameof(ResponseTimeout)}={ResponseTimeout}");

        if (IsRemoteTerminated)
        {
            Url = conf["RemoteTerminatedUrl"];

            if (string.IsNullOrEmpty(Url))
            {
                throw new ArgumentException("RemoteTerminatedUrl undefined in appsettings");
            }
        }
        else
        {
            Url = conf["RemoteOriginatedUrl"];

            if (string.IsNullOrEmpty(Url))
            {
                throw new ArgumentException("RemoteOriginatedUrl undefined in appsettings");
            }
        }





        _database = database;
        _manager = new(_database.QueryAllRoutes(IsRemoteTerminated), IsRemoteTerminated);

        if (RefreshInterval > TimeSpan.Zero)
        {
            Logger.Global.Debug("Starting thread");
            _thread = new(RunThread);
            _thread.Name = nameof(GatewayApp) + "-" + (IsRemoteTerminated ? "Remote" : "Local");
            _thread.IsBackground = true;
            _thread.Start();
        }

        // https://www.koderdojo.com/blog/asp-net-core-routing-and-routebuilder-mapget-for-calculating-a-factorial
        app.MapPost("/" + nameof(SubmitPost), SubmitPostHandler);
    }

    /// <summary>
    /// Gets whether remote terminated (or local terminated).
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    ///
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the routing information refresh interval.
    /// </summary>
    public TimeSpan RefreshInterval { get; }

    /// <summary>
    /// Gets the response timeout.
    /// </summary>
    public TimeSpan ResponseTimeout { get; }

    /// <summary>
    /// Implements dispose.
    /// </summary>
    public void Dispose()
    {
        Logger.Global.Debug("Disposal");
        v_disposed = true;
        _thread?.Interrupt();
        _manager.Dispose();
    }

    private Task SubmitPostHandler(HttpContext ctx)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"{nameof(SubmitPost)} received on TBD");

        var body = new StreamReader(ctx.Request.Body, Encoding.UTF8, false).ReadToEnd();
        Logger.Global.Write(SeverityLevel.Info, body);

        var submit = JsonSerializable.Deserialize<SubmitPost>(body);
        var code = _manager.SubmitPostRequest(submit, out SubmitResponse response);
        Logger.Global.Debug($"Response code: {code}");

        return WriteResponseAsync(ctx.Response, code, response.ToString());
    }

    private Task WriteResponseAsync(HttpResponse resp, int code, string? body)
    {
        body ??= string.Empty;
        resp.StatusCode = code;
        resp.ContentType = MediaTypeNames.Application.Json;
        resp.ContentLength = Encoding.UTF8.GetByteCount(body);

        Logger.Global.Debug($"Writing response");
        return resp.WriteAsync(body, new CancellationTokenSource(ResponseTimeout).Token);
    }

    private void RunThread(object? _)
    {
        Logger.Global.Debug("Starting thread");

        while (!v_disposed)
        {
            try
            {
                Thread.Sleep(RefreshInterval);

                if (!v_disposed)
                {
                    Logger.Global.Debug($"Query all routes from database");
                    _manager.Reload(_database.QueryAllRoutes(IsRemoteTerminated));
                }
            }
            catch (Exception e)
            {
                Logger.Global.Write(e);
            }
        }
    }
}