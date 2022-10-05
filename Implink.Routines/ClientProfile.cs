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

namespace KuiperZone.Implink.Routines;

/// <summary>
/// A serializable class which implments <see cref="IReadOnlyClientProfile"/> and provides setters.
/// </summary>
public class ClientProfile : RouteProfile, IReadOnlyClientProfile
{
    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Category"/> and provides a setter.
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.ApiKind"/> and provides a setter.
    /// </summary>
    public string ApiKind { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.UserAgent"/> and provides setter.
    /// </summary>
    public string UserAgent { get; set; } = nameof(Implink);

    /// <summary>
    /// Overrides.
    /// </summary>
    public override void Assert()
    {
        base.Assert();

        if (string.IsNullOrWhiteSpace(Authentication))
        {
            throw new InvalidOperationException($"{Authentication} undefined");
        }

        if (string.IsNullOrWhiteSpace(ApiKind))
        {
            throw new InvalidOperationException($"{ApiKind} undefined");
        }
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public override bool Equals(IReadOnlyRouteProfile? obj)
    {
        if (obj != null && base.Equals(obj))
        {
            var temp = (IReadOnlyClientProfile)obj;
            return Category == temp.Category && ApiKind == temp.ApiKind && UserAgent == temp.UserAgent;
        }

        return false;
    }

}