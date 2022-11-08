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
/// Creates clients according to <see cref="IReadOnlyClientProfile.ApiKind"/>.
/// </summary>
public static class ClientFactory
{
    /// <summary>
    /// Valid <see cref="IReadOnlyClientProfile.ApiKind"/>. IMP API.
    /// </summary>
    public const string ImpV1 = "IMPv1";

    /// <summary>
    /// Valid <see cref="IReadOnlyClientProfile.ApiKind"/>. Twitter.
    /// </summary>
    public const string Twitter = "Twitter";

    /// <summary>
    /// Valid <see cref="IReadOnlyClientProfile.ApiKind"/>. Facebook.
    /// </summary>
    public const string Facebook = "Facebook";

    /// <summary>
    /// Returns true if kind is a valid api name.
    /// </summary>
    public static bool IsValidApi(string? kind)
    {
        if (ImpV1.Equals(kind, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Twitter.Equals(kind, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates new instance with given route profile.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid ApiKind</exception>
    public static ClientApi Create(IReadOnlyClientProfile profile)
    {
        if (ImpV1.Equals(profile.ApiKind, StringComparison.OrdinalIgnoreCase))
        {
            return new ImpHttpClient(profile, true);
        }

        if (Twitter.Equals(profile.ApiKind, StringComparison.OrdinalIgnoreCase))
        {
            return new TwitterClient(profile);
        }

        if (string.IsNullOrWhiteSpace(profile.ApiKind))
        {
        throw new ArgumentException(
            $"Undefined {nameof(IReadOnlyClientProfile.ApiKind)} for route {profile.NameId}");
        }

        throw new ArgumentException(
            $"Invalid unknown {nameof(IReadOnlyClientProfile.ApiKind)} {profile.ApiKind} for route {profile.NameId}");
    }
}