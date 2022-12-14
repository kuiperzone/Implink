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

using System.Net;
using System.Net.Mime;
using System.Text;
using KuiperZone.Implink.Api;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Stub;

/// <summary>
/// Test server.
/// </summary>
public class ImpServer : IDisposable
{
    private readonly string ServerName;
    private readonly WebApplication _app;

    /// <summary>
    /// Constructor. Does not dispose of app or database.
    /// </summary>
    public ImpServer(string url, bool isRemote, ImpAuthentication? keys = null)
    {
        IsRemote = isRemote;
        ServerUrl = url;
        Keys = keys;
        ServerName = IsRemote ? "RemoteServer" : "LocalServer";
        Logger.Global.Debug($"{nameof(ServerUrl)}={ServerUrl}");

        if (string.IsNullOrEmpty(ServerUrl))
        {
            throw new ArgumentException($"Undefined in {nameof(ServerUrl)} appsettings");
        }

        var builder = WebApplication.CreateBuilder();

        // Not using
        builder.Logging.ClearProviders();

        Logger.Global.Debug("Building application");
        _app = builder.Build();

        // https://www.koderdojo.com/blog/asp-net-core-routing-and-routebuilder-mapget-for-calculating-a-factorial
        _app.MapPost("/" + nameof(IMessagingApi.PostMessage), PostMessageHandler);

        _app.RunAsync(ServerUrl);
    }

    /// <summary>
    /// Gets whether remote or local
    /// </summary>
    public bool IsRemote { get; }

    /// <summary>
    /// Gets the server URL.
    /// </summary>
    public string ServerUrl { get; }

    /// <summary>
    /// Gets authentication keys.
    /// </summary>
    public ImpAuthentication? Keys { get; }

    /// <summary>
    /// Implements dispose.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().Wait(1000);
    }

    /// <summary>
    /// Implements dispose.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Logger.Global.Debug("Disposal");
        return _app.DisposeAsync();
    }

    private async Task<Task> PostMessageHandler(HttpContext ctx)
    {
        var resp = new ImpResponse();
        Logger.Global.Write($"{ServerName} : {nameof(IMessagingApi.PostMessage)} received on {ServerUrl}");

        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, false);
        var body = await reader.ReadToEndAsync();

        foreach (var item in ctx.Request.Headers)
        {
            if (item.Value.Count != 0)
            {
                Logger.Global.Write($"{ServerName} : HEADER: " + item.Key + "=" + item.Value[0]);
            }
        }

        Logger.Global.Write($"{ServerName} : REQ BODY: {body}");

        var authFailure = Keys?.Verify(ctx.Request.Headers, body);

        if (authFailure == null)
        {
            var msg = Jsonizable.Deserialize<ImpMessage>(body);

            if (!string.IsNullOrEmpty(msg.Text))
            {
                if (string.IsNullOrEmpty(msg.MsgId))
                {
                    resp.Status = HttpStatusCode.BadRequest;
                    resp.Content = "MsgId empty";
                }
                else
                {
                    // OK
                    resp.Content = msg.MsgId;
                }
            }
            else
            {
                resp.Status = HttpStatusCode.BadRequest;
                resp.Content = "Message text empty";
            }
        }
        else
        {
            resp.Status = HttpStatusCode.Unauthorized;
            resp.Content = authFailure;
        }

        return WriteResponseAsync(ctx.Response, resp);
    }

    private Task WriteResponseAsync(HttpResponse httpResp, ImpResponse impResp)
    {
        var body = impResp.ToString();
        Logger.Global.Write(SeverityLevel.Notice, "RESPONSE: " + body);

        httpResp.StatusCode = (int)impResp.Status;
        httpResp.ContentType = MediaTypeNames.Application.Json;
        httpResp.ContentLength = Encoding.UTF8.GetByteCount(body);

        Logger.Global.Debug($"Writing response");
        return httpResp.WriteAsync(body, new CancellationTokenSource(1500).Token);
    }

}