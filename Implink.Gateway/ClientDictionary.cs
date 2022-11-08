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

using KuiperZone.Implink.Api;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Manages a collection of client sessions. Thread safe.
/// </summary>
public class ClientDictionary : IDisposable
{
    private readonly object _syncObj = new();
    private readonly Dictionary<string, ClientContainer> _keyed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ClientContainer>> _named = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ClientDictionary(bool remoteTerminated = true)
    {
        IsRemoteTerminated = remoteTerminated;
    }

    /// <summary>
    /// Constructor with initial sequence of client profiles.
    /// </summary>
    public ClientDictionary(IEnumerable<IReadOnlyClientProfile> profiles, bool remoteTerminated)
    {
        IsRemoteTerminated = remoteTerminated;
        Reload(profiles);
    }

    /// <summary>
    /// Implements <see cref="IClientApi.IsRemoteTerminated"/>.
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    /// Gets the number of clients.
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
    /// Gets the clients with matching name. If the client has categories, then "category" must be one of those.
    /// </summary>
    public ClientContainer[] Get(string? nameId, string? category)
    {
        lock (_syncObj)
        {
            return GetNoSync(nameId, category);
        }
    }

    /// <summary>
    /// Adds a new client with given profile. The client implementation is created according to
    /// <see cref="IReadOnlyClientProfile.ApiKind"/>. The result is true on success, or false if already exists.
    /// </summary>
    public bool Add(IReadOnlyClientProfile profile)
    {
        lock (_syncObj)
        {
            return AddNoSync(profile);
        }
    }

    /// <summary>
    /// Removes one or more clients with the given name. The result is the number of clients removed.
    /// </summary>
    public int Remove(string? nameId)
    {
        lock (_syncObj)
        {
            return RemoveNoSync(nameId);
        }
    }

    /// <summary>
    /// Loads clients for given profiles. Existing clients with unchanged profile are maintained.
    /// The result is true if client list is modified.
    /// </summary>
    public bool Reload(IEnumerable<IReadOnlyClientProfile> profiles)
    {
        var newDictionary = new Dictionary<string, IReadOnlyClientProfile>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var item in profiles)
        {
            if (!string.IsNullOrWhiteSpace(item.NameId))
            {
                if (!newDictionary.TryAdd(item.GetKey(), item))
                {
                    Logger.Global.Write(SeverityLevel.Warning, $"{nameof(ClientProfile.NameId)} and {nameof(ClientProfile.BaseAddress)} combination not unique for {item.NameId}");
                }
            }
            else
            {
                Logger.Global.Write(SeverityLevel.Warning, $"{nameof(ClientProfile)}.{nameof(ClientProfile.NameId)} is mandatory (ignored)");
            }
        }

        bool modified = false;

        lock (_syncObj)
        {
            // Remove items no longer in given profiles
            foreach (var oldContainer in _keyed.Values.ToArray())
            {
                var oldProfile = oldContainer.Client.Profile;
                var key = oldProfile.GetKey();

                if (!newDictionary.TryGetValue(key, out IReadOnlyClientProfile? newProfile) || !newProfile.Equals(oldProfile))
                {
                    oldContainer.Dispose();

                    _keyed.Remove(key);
                    _named.Remove(oldProfile.NameId);

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
                    Logger.Global.Write(e);
                }
            }
        }

        return modified;
    }

    /// <summary>
    /// Disposes.
    /// </summary>
    public void Dispose()
    {
        IEnumerable<ClientContainer> list;

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

    private void ClearNoSync()
    {
        _keyed.Clear();
        _named.Clear();
    }

    private bool ExistsNoSync(string? nameId)
    {
        return !string.IsNullOrEmpty(nameId) && _named.ContainsKey(nameId);
    }

    private ClientContainer[] GetNoSync(string? nameId, string? category)
    {
        Logger.Global.Debug("Find: " + nameId + " / " + category);

        if (!string.IsNullOrEmpty(nameId) && _named.TryGetValue(nameId, out List<ClientContainer>? temp1))
        {
            Logger.Global.Debug($"Found {temp1.Count}: " + nameId);

            var temp2 = new List<ClientContainer>(temp1.Count);

            foreach (var item in temp1)
            {
                Logger.Global.Debug(item.Client.Profile.ToString());

                if (item.Client.Categories.Count == 0 || (item.Client.Categories.Contains(category ?? "")))
                {
                    Logger.Global.Debug($"Accept");
                    temp2.Add(item);
                }
            }

            return temp2.ToArray();
        }

        return Array.Empty<ClientContainer>();
    }

    private bool AddNoSync(IReadOnlyClientProfile profile)
    {
        Logger.Global.Debug(profile.GetKey());

        if (!_keyed.ContainsKey(profile.GetKey()))
        {
            var client = new ClientContainer(profile, IsRemoteTerminated);

            Logger.Global.Debug("Add to keyed");
            _keyed.Add(profile.GetKey(), client);

            if (!_named.TryGetValue(profile.NameId, out List<ClientContainer>? list))
            {
                Logger.Global.Debug("Add new list for " + profile.NameId);
                list = new();
                _named.Add(profile.NameId, list);
            }

            Logger.Global.Debug("Add client to list");
            list.Add(client);
            return true;
        }

        return false;
    }

    private int RemoveNoSync(string? nameId)
    {
        int count = 0;

        if (!string.IsNullOrEmpty(nameId) && _named.TryGetValue(nameId, out List<ClientContainer>? list))
        {
            count += 1;
            _named.Remove(nameId);

            foreach (var item in list)
            {
                _keyed.Remove(item.Client.Profile.GetKey());
            }
        }

        return count;
    }

}