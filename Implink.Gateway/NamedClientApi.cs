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

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// A class which implements <see cref="IClientApi"/>, where the underlying implementation is decided according to
/// the profile given to the constructor. It accepts <see cref="IReadOnlyNamedClientProfile"/> in order to extend
/// the client functionality.
/// </summary>
public class NamedClientApi : IEquatable<IReadOnlyNamedClientProfile>, IClientApi, IDisposable
{
    private readonly ClientApi _client;

    /// <summary>
    /// Constructor in which the client is created according to <see cref="ApiKind"/> value given in the profile.
    /// </summary>
    public NamedClientApi(IReadOnlyNamedClientProfile profile)
    {
        profile.AssertValidity();
        Profile = profile;
        _client = ClientFactory.Create(profile.Api, profile);
    }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public IReadOnlyNamedClientProfile Profile { get; }

    /// <summary>
    /// Implements <see cref="IClientApi.SubmitPostRequest(SubmitPost, out SubmitResponse)"/>.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        if (Profile.PrefixUser && !string.IsNullOrWhiteSpace(submit.UserName))
        {
            var prefix = submit.UserName + ": ";

            if (!submit.Text.StartsWith(prefix))
            {
                // Make a clone
                submit = new SubmitPost(submit);
                submit.Text = prefix + submit.Text;
            }
        }

        if (Profile.MaxText > 3 && submit.Text.Length > Profile.MaxText - 3)
        {
            submit = new SubmitPost(submit);
            submit.Text = submit.Text.Substring(0, Profile.MaxText - 3) + "...";
        }

        return _client.SubmitPostRequest(submit, out response);
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

}