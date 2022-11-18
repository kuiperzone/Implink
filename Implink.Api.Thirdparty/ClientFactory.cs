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

namespace KuiperZone.Implink.Api.Thirdparty;

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
    public static IMessagingClient Create(ClientKind kind, IReadOnlyClientProfile profile)
    {
        switch (kind)
        {
            case ClientKind.ImpV1:
                return new ImpClient(profile);
            case ClientKind.Twitter:
                return new TwitterClient(profile);
            default:
                throw new ArgumentException(
                    $"Unknown or invalid {nameof(ClientKind)} {kind}");

        }
    }
}