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
using KuiperZone.Implink.Routines.Api;

namespace KuiperZone.Implink.Routines.Gateways;

/// <summary>
/// Manages a collection of client sessions.
/// </summary>
public class SessionManager : IClientApi, IDisposable
{
    private readonly object _syncObj = new();
    private readonly Dictionary<string, SessionContainer> _keyed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<SessionContainer>> _named = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SessionManager(bool remoteTerminated = true)
    {
        IsRemoteTerminated = remoteTerminated;
    }

    /// <summary>
    /// Constructor with initial sequence of client profiles.
    /// </summary>
    public SessionManager(IEnumerable<IReadOnlyRouteProfile> profiles, bool remoteTerminated)
    {
        IsRemoteTerminated = remoteTerminated;
        Reload(profiles);
    }

    /// <summary>
    /// Implements <see cref="IClientApi.IsRemoteTerminated"/>.
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    /// Gets the number of sessions.
    /// </summary>
    public int Count
    {
        get { return _keyed.Count; }
    }

    /// <summary>
    /// Clears all.
    /// </summary>
    public void Clear()
    {
        lock (_syncObj)
        {
            ClearNoSync();
        }
    }

    /// <summary>
    /// Returns true if one or more clients exist with the given name.
    /// </summary>
    public bool Exists(string? nameId)
    {
        lock (_syncObj)
        {
            return ExistsNoSync(nameId);
        }
    }

    /// <summary>
    /// Gets the clients with matching name.
    /// </summary>
    public IClientApi[] Get(string? nameId)
    {
        lock (_syncObj)
        {
            return GetNoSync(nameId);
        }
    }

    /// <summary>
    /// Adds a new session with given profile. The result is true on success, or false if already exists.
    /// </summary>
    public bool Add(IReadOnlyRouteProfile profile)
    {
        lock (_syncObj)
        {
            return AddNoSync(profile);
        }
    }

    /// <summary>
    /// Removes one or more sessions with given name. The result is the number of sessions removed.
    /// </summary>
    public int Remove(string? nameId)
    {
        lock (_syncObj)
        {
            return RemoveNoSync(nameId);
        }
    }

    /// <summary>
    /// Loads sessions for given profiles. Existing sessions with unchanged profile are maintained.
    /// The result is true if session list is modified.
    /// </summary>
    public bool Reload(IEnumerable<IReadOnlyRouteProfile> profiles, out string? info)
    {
        info = null;
        var newDictionary = new Dictionary<string, IReadOnlyRouteProfile>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var item in profiles)
        {
            if (!string.IsNullOrWhiteSpace(item.NameId))
            {
                if (!newDictionary.TryAdd(item.GetKey(), item))
                {
                    info ??= $"{nameof(RouteProfile.NameId)} and {nameof(RouteProfile.BaseAddress)} combination not unique for {item.NameId}";
                }
            }
            else
            {
                info ??= $"{nameof(RouteProfile)}.{nameof(RouteProfile.NameId)} is mandatory";
            }
        }

        bool modified = false;

        lock (_syncObj)
        {
            // Remove items no longer in given profiles
            foreach (var oldContainer in _keyed.Values.ToArray())
            {
                var key = oldContainer.Profile.GetKey();

                if (!newDictionary.TryGetValue(key, out IReadOnlyRouteProfile? newProfile) || !newProfile.Equals(oldContainer.Profile))
                {
                    oldContainer.Dispose();
                    _keyed.Remove(key);
                    _named.Remove(oldContainer.Profile.NameId);
                    modified = true;
                }
            }

            // Add new ones
            foreach (var item in newDictionary.Values)
            {
                try
                {
                    modified |= AddNoSync(item);
                }
                catch (Exception e)
                {
                    // Log only
                    info = e.Message;
                }
            }
        }

        return modified;
    }

    /// <summary>
    /// Implements <see cref="IClientApi.SubmitPostRequest"/> and sends post to all clients with matching
    /// name. Sending is performed in other threads and the call does not wait, therefore, for the response.
    /// The result indicates success if there is a client with a matching name.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        response = new();

        try
        {
            if (string.IsNullOrWhiteSpace(submit.NameId) || string.IsNullOrWhiteSpace(submit.Text))
            {
                response.ErrorReason = "Name or text undefined";
                return (int)HttpStatusCode.BadRequest;
            }

            var clients = Get(submit.NameId);

            if (clients.Length != 0)
            {
                foreach (var item in clients)
                {
                    var tuple = Tuple.Create(item, submit);
                    ThreadPool.QueueUserWorkItem(SubmitThread, tuple);
                }

                return (int)HttpStatusCode.OK;
            }

            response.ErrorReason = "No client for " + submit.NameId;
            return (int)HttpStatusCode.BadRequest;
        }
        catch (Exception e)
        {
            response.ErrorReason = e.Message;
            return (int)HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Disposes.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        IEnumerable<SessionContainer> list;

        lock (_syncObj)
        {
            list = _keyed.Values.ToArray();
            ClearNoSync();
        }

        foreach (var item in list)
        {
            item.Dispose();
        }
    }

    private static void SubmitThread(object? obj)
    {
        try
        {
            var tuple = (Tuple<SessionContainer, SubmitPost>)(obj ?? throw new ArgumentNullException());
            var code = tuple.Item1.SubmitPostRequest(tuple.Item2, out SubmitResponse resp);
        }
        catch (Exception e)
        {
        }
    }

    private void ClearNoSync()
    {
        _keyed.Clear();
        _named.Clear();
    }

    private bool ExistsNoSync(string? nameId)
    {
        return !string.IsNullOrEmpty(nameId) && _named.ContainsKey(nameId);
    }

    private IClientApi[] GetNoSync(string? nameId)
    {
        if (!string.IsNullOrEmpty(nameId) && _named.TryGetValue(nameId, out List<SessionContainer>? list))
        {
            return list.ToArray();
        }

        return Array.Empty<IClientApi>();
    }

    private bool AddNoSync(IReadOnlyRouteProfile profile)
    {
        if (!_keyed.ContainsKey(profile.GetKey()))
        {
            var session = new SessionContainer(profile, IsRemoteTerminated);

            _keyed.Add(profile.GetKey(), session);

            if (!_named.TryGetValue(profile.NameId, out List<SessionContainer>? list))
            {
                list = new();
                _named.Add(profile.NameId, list);
            }

            list.Add(session);
            return true;
        }

        return false;
    }

    private int RemoveNoSync(string? nameId)
    {
        int count = 0;

        if (!string.IsNullOrEmpty(nameId) && _named.TryGetValue(nameId, out List<SessionContainer>? list))
        {
            count += 1;
            _named.Remove(nameId);

            foreach (var item in list)
            {
                _keyed.Remove(item.Profile.GetKey());
            }
        }

        return count;
    }

}