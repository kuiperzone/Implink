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

using KuiperZone.Implink.Routines.Api;

namespace KuiperZone.Implink.Routines.RoutingProfile;

/// <summary>
/// A serializable class which implements <see cref="IReadOnlyRouteProfile"/> and provides setters.
/// </summary>
public class SessionManager : IClientApi, IDisposable
{
    private object _syncObj = new();
    private Dictionary<string, SessionContainer> _hash = new();
    private Dictionary<string, List<SessionContainer>> _dictionary = new(StringComparer.InvariantCultureIgnoreCase);

    public void Clear()
    {
        lock (_syncObj)
        {
            ClearNoSync();
        }
    }

    public bool Exists(IReadOnlyClientProfile profile)
    {
        lock (_syncObj)
        {
            return ExistsNoSync(profile);
        }
    }

    public bool Exists(string? nameId)
    {
        lock (_syncObj)
        {
            return ExistsNoSync(nameId);
        }
    }

    public IClientApi[] Get(string? nameId)
    {
        lock (_syncObj)
        {
            return GetNoSync(nameId);
        }
    }

    public bool Add(IReadOnlyClientProfile profile)
    {
        lock (_syncObj)
        {
            return AddNoSync(profile);
        }
    }

    public int SubmitPostRequest(SubmitPost sumbit, out SubmitResponse response)
    {
        lock (_syncObj)
        {
            return GetNoSync(nameId);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        IEnumerable<SessionContainer> list;

        lock (_syncObj)
        {
            list = _hash.Values.ToArray();
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
        _hash.Clear();
        _dictionary.Clear();
    }

    private bool ExistsNoSync(string? nameId)
    {
        return !string.IsNullOrEmpty(nameId) && !_dictionary.ContainsKey(nameId);
    }

    private bool ExistsNoSync(IReadOnlyClientProfile profile)
    {
        return _hash.ContainsKey(profile.GetKey());
    }

    private IClientApi[] GetNoSync(string? nameId)
    {
        if (!string.IsNullOrEmpty(nameId) && _dictionary.TryGetValue(nameId, out List<SessionContainer>? list))
        {
            return list.ToArray();
        }

        return Array.Empty<IClientApi>();
    }

    private bool AddNoSync(IReadOnlyClientProfile profile)
    {
        if (!ExistsNoSync(profile))
        {
            var session = new SessionContainer(profile);

            _hash.Add(profile.GetKey(), session);

            if (!_dictionary.TryGetValue(profile.NameId, out List<SessionContainer>? list))
            {
                list = new();
                _dictionary.Add(profile.NameId, list);
            }

            list.Add(session);
            return true;
        }

        return false;
    }
}