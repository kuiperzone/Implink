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

using KuiperZone.Implink.Routines.Api.Imp;
using KuiperZone.Implink.Routines.Api.Thirdparty;

namespace KuiperZone.Implink.Routines.Api;

/// <summary>
/// Creates clients according to <see cref="IReadOnlyClientProfile.ApiKind"/>.
/// </summary>
public abstract class ClientFactory
{
    /// <summary>
    /// IMP API.
    /// </summary>
    public const string ImpV1 = "IMPv1";

    /// <summary>
    /// Twitter.
    /// </summary>
    public const string Twitter = "Twitter";

    /// <summary>
    /// Creates new instance with given route profile.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid ApiKind</exception>
    public ClientSession Create(IReadOnlyClientProfile profile)
    {
        switch (profile.ApiKind.ToLowerInvariant())
        {
            case ImpV1: return new ImpClientSession(profile);
            case Twitter: return new TwitterClientSession(profile);
            default: throw new ArgumentException($"Invalid or unknown {nameof(IReadOnlyClientProfile.ApiKind)} {profile.ApiKind}");
        }
    }
}