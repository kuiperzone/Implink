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
using KuiperZone.Implink.Api.Util;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// A class which implements <see cref="IClientApi"/>, but actually submits a message to many internal clients.
/// This class will not dispose of the client instances.
/// </summary>
public class RouteClient : IEquatable<IReadOnlyRouteProfile>, IClientApi
{
    /// <summary>
    /// Constructor with profile and dictionary of available clients.
    /// </summary>
    public RouteClient(IReadOnlyRouteProfile profile, Dictionary<string, IClientApi> clients)
    {
        profile.AssertValidity();
        Profile = profile;

        var list = new List<IClientApi>();

        foreach (var item in DictionaryParser.ToSet(profile.Clients))
        {
            if (clients.TryGetValue(item, out IClientApi? api))
            {
                list.Add(api);
            }
        }

        Clients = list;
    }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public IReadOnlyRouteProfile Profile { get; }

    /// <summary>
    /// Gets the associated clients.
    /// </summary>
    public IReadOnlyCollection<IClientApi> Clients { get; }

    /// <summary>
    /// Gets whether to dispose on clients
    /// </summary>
    public bool ShallDisposeOfClients { get; }

    /// <summary>
    /// Implements <see cref="IClientApi.SubmitPostRequest(SubmitPost, out SubmitResponse)"/>.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
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

}