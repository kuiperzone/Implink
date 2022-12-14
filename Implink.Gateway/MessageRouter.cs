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
using System.Text;
using KuiperZone.Implink.Api;
using KuiperZone.Implink.Api.Thirdparty;
using KuiperZone.Implink.Api.Util;
using KuiperZone.Utility.Yaal;
using Microsoft.Extensions.Primitives;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Performs a one-to-many operation where <see cref="ImpMessage"/> values are sent to multiple internal clients.
/// Note that class and will not "own" the clients provided on construction and will not dispose of them.
/// </summary>
public class MessageRouter : IEquatable<IReadOnlyRouteProfile>
{
    /// <summary>
    /// Constructor with sequence of clients.
    /// </summary>
    public MessageRouter(IReadOnlyRouteProfile profile, IEnumerable<NamedClientApi> clients, bool waitOnForward)
    {
        profile.AssertValidity();

        Profile = profile;
        Clients = new List<NamedClientApi>(clients);
        TagSet = StringParser.ToSet(Profile.Tags);
        Authenticator = new(Profile);
        Counter = new(Profile.ThrottleRate);
        WaitOnForward = waitOnForward;
    }

    /// <summary>
    /// Constructor with profile and dictionary of available clients, from which the constructor selects according
    /// to the <see cref="IReadOnlyRouteProfile.Clients"/> list.
    /// </summary>
    public MessageRouter(IReadOnlyRouteProfile profile, IReadOnlyDictionary<string, NamedClientApi> clients, bool waitOnForward)
        : this(profile, SelectClients(profile, clients), waitOnForward)
    {
    }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public IReadOnlyRouteProfile Profile { get; }

    /// <summary>
    /// Gets the associated clients.
    /// </summary>
    public IReadOnlyCollection<NamedClientApi> Clients { get; }

    /// <summary>
    /// Gets the <see cref="IReadOnlyRouteProfile.Tags"/> as a set.
    /// </summary>
    public IReadOnlySet<string> TagSet { get; }

    /// <summary>
    /// Gets the rate counter.
    /// </summary>
    public RateCounter Counter { get; }

    /// <summary>
    /// Gets the route's <see cref="ImpAuthentication"/> instance which acts as authenticator for incoming
    /// remote-originated requests.
    /// </summary>
    public ImpAuthentication Authenticator;

    /// <summary>
    /// Gets whether messages are sent in an internal thread (false), or whether waits for the response to arrive.
    /// </summary>
    public bool WaitOnForward { get; }

    /// <summary>
    /// Same as <see cref="IMessagingApi.PostMessage"/>, but also authenticates incoming message.
    /// </summary>
    public ImpResponse PostMessage(IDictionary<string, StringValues> headers, string body, ImpMessage request)
    {
        try
        {
            if (!request.CheckValidity(out string? failMsg))
            {
                return new ImpResponse(HttpStatusCode.BadRequest, failMsg);
            }

            failMsg = Authenticator.Verify(headers, body);

            if (failMsg != null)
            {
                return new ImpResponse(HttpStatusCode.Unauthorized, failMsg);
            }

            if (!Profile.Enabled)
            {
                return new ImpResponse(HttpStatusCode.BadRequest, $"{nameof(Profile)} {Profile.Id} disabled");
            }

            if (TagSet.Count != 0 && !TagSet.Contains(request.Tag ?? ""))
            {
                return new ImpResponse(HttpStatusCode.BadRequest, $"Invalid {nameof(request.Tag)}, must be one of: {Profile.Tags}");
            }

            if (!Profile.Replies && !string.IsNullOrEmpty(request.ParentMsgId))
            {
                return new ImpResponse(HttpStatusCode.BadRequest, $"{nameof(request.ParentMsgId)} not supported");
            }

            if (Counter.IsThrottled(true))
            {
                return new ImpResponse(HttpStatusCode.TooManyRequests, "Requests limit reached");
            }

            if (Clients.Count == 0)
            {
                return new ImpResponse(HttpStatusCode.InternalServerError, "No valid clients on route");
            }

            if (string.IsNullOrWhiteSpace(request.MsgId))
            {
                request.MsgId = GenerateMsgId();
                Logger.Global.Debug($"Assigned msgid: {request.MsgId}");
            }

            int success = 0;
            var error = HttpStatusCode.OK;
            var info = new List<string>();

            foreach (var item in Clients)
            {
                var s = HttpStatusCode.OK;

                // Replies supported for imp only
                if (!string.IsNullOrEmpty(request.ParentMsgId) && !item.Profile.Kind.IsImp())
                {
                    s = HttpStatusCode.BadRequest;
                    info.Add($"Reply messages not supported for {item.Profile.Kind} client");
                    Logger.Global.Debug(info[^1]);
                }
                else
                if (WaitOnForward)
                {
                    Logger.Global.Debug($"Forward and wait for response for {item.Profile.Kind}");
                    var temp = PostMessageToClient(item, request);
                    s = temp.Status;

                    if (s == HttpStatusCode.OK)
                    {
                        success += 1;
                    }
                    else
                    {
                        info.Add(temp.ErrorInfo ?? s.ToString());
                    }
                }
                else
                {
                    success += 1;
                    Logger.Global.Debug($"Queue worker thread for {item.Profile.Kind}");
                    ThreadPool.QueueUserWorkItem(SubmitThread, Tuple.Create(item, request));
                }

                // Keep only first error
                if (error == HttpStatusCode.OK)
                {
                    error = s;
                }
            }

            if (error == HttpStatusCode.OK && success == 0)
            {
                // Can happen with ParentMsgId
                error = HttpStatusCode.BadRequest;
            }

            // Reponse OK if one post message succeeds,
            // but will contain ErrorInfo (if any).
            var resp = new ImpResponse(request.MsgId);

            if (success == 0)
            {
                resp.Status = HttpStatusCode.BadRequest;

                if (error != HttpStatusCode.OK)
                {
                    resp.Status = error;
                }
            }

            if (info.Count > 1)
            {
                // Combine responses
                resp.ErrorInfo = success + " of " + Clients.Count + " succeeded: " + string.Join(", ", info);
            }
            else
            if (info.Count > 0)
            {
                resp.ErrorInfo = info[0];
            }

            return resp;
        }
        catch (Exception e)
        {
            Logger.Global.Debug(e);
            return new ImpResponse(e);
        }
    }

    /// <summary>
    /// Implements <see cref="IEquatable{T}"/>.
    /// </summary>
    public bool Equals(IReadOnlyRouteProfile? obj)
    {
        return Profile.Equals(obj);
    }

    /// <summary>
    /// Override.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Profile.Equals(obj);
    }

    /// <summary>
    /// Override.
    /// </summary>
    public override int GetHashCode()
    {
        return Profile.GetHashCode();
    }

    /// <summary>
    /// Override.
    /// </summary>
    public override string ToString()
    {
        return new RouterInfo(this).ToString();
    }

    private static string GenerateMsgId(int count = 12)
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

        var buf = new StringBuilder(count);

        for (int n = 0; n < count; ++n)
        {
            buf.Append(Alphabet[Random.Shared.Next(0, Alphabet.Length)]);
        }

        return buf.ToString();
    }

    private static IEnumerable<NamedClientApi> SelectClients(IReadOnlyRouteProfile profile,
        IReadOnlyDictionary<string, NamedClientApi> clients)
    {
        Logger.Global.Debug("Select for route: " + profile.Id);

        if (!profile.Enabled)
        {
            Logger.Global.Write(SeverityLevel.Warning, $"Route {profile.Id} is disabled");
        }

        var list = new List<NamedClientApi>();

        foreach (var item in StringParser.ToSet(profile.Clients))
        {
            Logger.Global.Debug("Identify client: " + item);

            if (clients.TryGetValue(item, out NamedClientApi? api))
            {
                list.Add(api);
                Logger.Global.Debug("Add client to route OK");
            }
            else
            {
                Logger.Global.Write(SeverityLevel.Warning, $"Client {item} not provisioned for route {profile.Id}");
            }
        }

        if (list.Count == 0)
        {
            Logger.Global.Write(SeverityLevel.Warning, $"Route {profile.Id} has no clients");
        }

        return list;
    }

    private static void SubmitThread(object? obj)
    {
        var tuple = (Tuple<NamedClientApi, ImpMessage>)(obj ?? throw new ArgumentNullException());
        PostMessageToClient(tuple.Item1, tuple.Item2);
    }

    private static ImpResponse PostMessageToClient(NamedClientApi client, ImpMessage request)
    {
        var prof = client.Profile;
        var msgId = request.MsgId;
        Logger.Global.Write($"Sending to: {prof.BaseAddress}");

        var resp = client.PostMessage(request);
        Logger.Global.Write("Response: " + resp.ToString());

        if (resp.Status != HttpStatusCode.OK)
        {
            var msg = $"{nameof(PostMessage)} failed to {prof.BaseAddress}, Status {resp.Status}, {resp.Content}";
            Logger.Global.Write(SeverityLevel.Notice, msg);
        }

        return resp;
    }

    private class RouterInfo : Jsonizable
    {
        public RouterInfo(MessageRouter router)
        {
            Id = router.Profile.Id;
            Enabled = router.Profile.Enabled.ToString();
            Tags = router.Profile.Tags ?? "";
            Counters = router.Counter.ToString();

            var clients = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var item in router.Clients)
            {
                clients.TryAdd(item.Profile.Id, item.ToString());
            }

            foreach (var item in StringParser.ToSet(router.Profile.Clients))
            {
                clients.TryAdd(item, $"{item} - WARNING : Client not provisioned");
            }

            Clients = clients.Values.ToArray();
        }

        public string Id { get; }
        public string Enabled { get; }
        public string Tags { get; }
        public string Counters { get; }
        public string[] Clients { get; }
    }

}