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
using Microsoft.Extensions.Primitives;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Wraps a server and collection of a clients into an "application class".
/// </summary>
public class GatewayApp : IDisposable, IAsyncDisposable
{
    private readonly Thread? _thread;
    private readonly WebApplication _app;
    private readonly ClientDictionary _clients;
    private readonly RouteDatabase _database;
    private volatile bool v_disposed;

    /// <summary>
    /// Constructor.
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

        Logger.Global.Debug("Creating clients");
        _clients = new(_database.QueryAllRoutes(), IsRemoteTerminated);

        Logger.Global.Debug("Building application");
        _app = builder.Build();

        // https://www.koderdojo.com/blog/asp-net-core-routing-and-routebuilder-mapget-for-calculating-a-factorial
        _app.MapPost("/" + nameof(SubmitPost), SubmitPostHandler);

        if (Settings.DatabaseRefresh > TimeSpan.Zero)
        {
            Logger.Global.Debug($"Starting {nameof(GatewayApp)} thread");
            _thread = new(DatabaseThread);
            _thread.Name = "Database" + (IsRemoteTerminated ? "Remote" : "Local");
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
        _clients.Dispose();
        return t;
    }

    private static string GenerateMsgId(int count = 10)
    {
        const string Alphabet = "abcdefghijklmnopqurstuvwxyz0123456789";

        var buf = new StringBuilder(count);

        for (int n = 0; n < count; ++n)
        {
            buf.Append(Alphabet[Random.Shared.Next(0, Alphabet.Length)]);
        }

        return buf.ToString();
    }

    private static int SubmitPostToClient(ClientApi client, SubmitPost submit, out SubmitResponse response)
    {
        try
        {
            var prof = client.Profile;

            Logger.Global.Write($"Sending: {prof.ApiKind}, {prof.BaseAddress}");
            var code = client.SubmitPostRequest(submit, out response);

            Logger.Global.Write("Response code: " + code);
            Logger.Global.Write(response.ToString());

            if (code != (int)HttpStatusCode.OK)
            {
                var msg = $"Failed to submit request on internal thread: {prof.ApiKind}, {prof.BaseAddress}. " +
                    $"Status code {code}, {response.ErrorReason}";
                Logger.Global.Write(SeverityLevel.Notice, msg);
            }

            return code;
        }
        catch (Exception e)
        {
            Logger.Global.Write(e);

            response = new();
            response.ErrorReason = e.Message;
            response.MsgId = submit.MsgId;
            return (int)HttpStatusCode.InternalServerError;
        }
    }

    private async Task<Task> SubmitPostHandler(HttpContext ctx)
    {
        Logger.Global.Write(SeverityLevel.Notice, $"{nameof(SubmitPost)} received on {ServerUrl}");
        using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, false);

        var body = await reader.ReadToEndAsync();
        Logger.Global.Write(SeverityLevel.Info, body);

        var submit = JsonSerializable.Deserialize<SubmitPost>(body);

        if (string.IsNullOrEmpty(submit.MsgId))
        {
            submit.MsgId = GenerateMsgId();
        }

        int code = SubmitPostToMultiClients(ctx.Request.Headers, body, submit, out SubmitResponse response);
        Logger.Global.Write(SeverityLevel.Notice, $"Response code: {code}");

        if (!string.IsNullOrEmpty(response.ErrorReason))
        {
            Logger.Global.Write(SeverityLevel.Notice, response.ErrorReason);
        }

        return WriteResponseAsync(ctx.Response, code, response.ToString());
    }

    private int SubmitPostToMultiClients(IDictionary<string, StringValues> headers, string body,
        SubmitPost submit, out SubmitResponse response)
    {
        response = new();
        response.MsgId = submit.MsgId;

        try
        {
            if (string.IsNullOrWhiteSpace(submit.NameId))
            {
                response.ErrorReason = $"{nameof(SubmitPost)}.{nameof(SubmitPost.NameId)} undefined";
                return (int)HttpStatusCode.BadRequest;
            }

            if (string.IsNullOrWhiteSpace(submit.Text))
            {
                response.ErrorReason = $"{nameof(SubmitPost)}.{nameof(SubmitPost.Text)} undefined";
                return (int)HttpStatusCode.BadRequest;
            }

            var clients = _clients.Get(submit.NameId, submit.Category);

            if (clients.Length == 0)
            {
                response.ErrorReason = "No client for " + submit.NameId;
                return (int)HttpStatusCode.BadRequest;
            }

            var code = (int)HttpStatusCode.OK;
            response.ErrorReason = "OK";

            var errors = new List<string>();

            foreach (var item in clients)
            {
                Logger.Global.Debug(item.Client.Profile.BaseAddress);

                var authFailure = item.AuthenticationKeys?.Verify(headers, body);

                if (authFailure != null)
                {
                    errors.Add(authFailure);
                    Logger.Global.Debug(errors[^1]);

                    if (code == (int)HttpStatusCode.OK)
                    {
                        code = (int)HttpStatusCode.Unauthorized;
                    }
                }
                else
                if (item.Counter.IsThrottled(true))
                {
                    errors.Add(item.Client.Profile.ApiKind + ' ' + HttpStatusCode.TooManyRequests);
                    Logger.Global.Debug(errors[^1]);

                    if (code == (int)HttpStatusCode.OK)
                    {
                        code = (int)HttpStatusCode.TooManyRequests;
                    }
                }
                else
                if (Settings.ForwardWait)
                {
                    Logger.Global.Debug("Forward and wait for response");
                    var tc = SubmitPostToClient(item.Client, submit, out SubmitResponse tr);

                    if (tc != (int)HttpStatusCode.OK)
                    {
                        if (!string.IsNullOrEmpty(tr.ErrorReason))
                        {
                            errors.Add(tr.ErrorReason);
                        }

                        if (code == (int)HttpStatusCode.OK)
                        {
                            code = tc;
                        }
                    }
                }
                else
                {
                    Logger.Global.Debug("Queue for worker thread");
                    ThreadPool.QueueUserWorkItem(SubmitThread, Tuple.Create(item.Client, submit));
                }
            }

            if (code != (int)HttpStatusCode.OK)
            {
                if (clients.Length == 1)
                {
                    response.ErrorReason = string.Join(", ", errors);
                }
                else
                {
                    // Combine responses
                    response.ErrorReason = (clients.Length - errors.Count) + " of " + clients.Length + " succeeded: " + string.Join(", ", errors);
                }
            }

            return code;
        }
        catch (ImpException e)
        {
            Logger.Global.Debug(e);
            response.ErrorReason = e.Message;
            return e.StatusCode;
        }
        catch (Exception e)
        {
            Logger.Global.Debug(e);
            response.ErrorReason = e.Message;
            return (int)HttpStatusCode.InternalServerError;
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

    private static void SubmitThread(object? obj)
    {
        var tuple = (Tuple<ClientApi, SubmitPost>)(obj ?? throw new ArgumentNullException());
        SubmitPostToClient(tuple.Item1, tuple.Item2, out SubmitResponse _);
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
                    Logger.Global.Debug($"Query all routes from database");
                    _clients.Reload(_database.QueryAllRoutes());
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