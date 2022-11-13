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

using KuiperZone.Implink.Api.Thirdparty;

namespace KuiperZone.Implink.Api;

/// <summary>
/// Creates clients according to <see cref="IReadOnlyClientProfile.Api"/>.
/// </summary>
public static class ClientFactory
{
    /// <summary>
    /// Creates new remote terminated instance with given client profile. Note that only remote terminated
    /// instances can be created.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid Api, or Endpoint</exception>
    public static ClientApi Create(IReadOnlyClientProfile profile)
    {
        if (profile.Endpoint != EndpointKind.Remote)
        {
            throw new ArgumentException($"{nameof(profile.Endpoint)} cannot be remote originated");
        }

        switch (profile.Api)
        {
            case ApiKind.ImpV1:
                return new ImpHttpClient(profile);
            case ApiKind.Twitter:
                return new TwitterClient(profile);
            default:
                throw new ArgumentException(
                    $"Unknown or invalid {nameof(IReadOnlyClientProfile.Api)} {profile.Api} for route {profile.NameId}");

        }
    }
}