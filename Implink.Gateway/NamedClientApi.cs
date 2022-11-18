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

using System.Text;
using KuiperZone.Implink.Api;
using KuiperZone.Implink.Api.Thirdparty;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// A class which implements <see cref="IMessagingApi"/>, where the underlying implementation is decided according to
/// the profile given to the constructor. It accepts <see cref="IReadOnlyNamedClientProfile"/> in order to extend
/// the client functionality.
/// </summary>
public class NamedClientApi : IEquatable<IReadOnlyNamedClientProfile>, IMessagingApi, IDisposable
{
    private readonly IMessagingClient _client;

    /// <summary>
    /// Constructor in which the client is created according to <see cref="ApiKind"/> value given in the profile.
    /// </summary>
    public NamedClientApi(IReadOnlyNamedClientProfile profile)
    {
        profile.AssertValidity();
        Profile = profile;

        _client = ClientFactory.Create(profile.Kind, profile);
    }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public IReadOnlyNamedClientProfile Profile { get; }

    /// <summary>
    /// Implements <see cref="IMessagingApi.PostMessage"/>.
    /// </summary>
    public ImpResponse PostMessage(ImpMessage request)
    {
        if (Profile.PrefixUser && !string.IsNullOrWhiteSpace(request.UserName))
        {
            var prefix = request.UserName + ": ";

            if (!request.Text.StartsWith(prefix))
            {
                // Make a clone
                request = new ImpMessage(request);
                request.Text = prefix + request.Text;
            }
        }

        if (Profile.MaxText > 3 && request.Text.Length > Profile.MaxText - 3)
        {
            request = new ImpMessage(request);
            request.Text = request.Text.Substring(0, Profile.MaxText - 3) + "...";
        }

        return _client.PostMessage(request);
    }

    /// <summary>
    /// Implements disposal.
    /// </summary>
    public void Dispose()
    {
        _client.Dispose();
    }

    /// <summary>
    /// Implements <see cref="IEquatable{T}"/>.
    /// </summary>
    public bool Equals(IReadOnlyNamedClientProfile? obj)
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
        var sb = new StringBuilder(256);
        sb.Append(Profile.Id);
        sb.Append(" (");
        sb.Append(Profile.Kind);
        sb.Append(") : ");
        sb.Append(Profile.BaseAddress);
        return sb.ToString();
    }
}