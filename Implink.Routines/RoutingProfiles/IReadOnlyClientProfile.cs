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

namespace KuiperZone.Implink.Routines.RoutingProfile;

/// <summary>
/// Extends <see cref="IReadOnlyRouteProfile"/> to provide additional fields for client routing.
/// </summary>
public interface IReadOnlyClientProfile : IReadOnlyRouteProfile
{
    /// <summary>
    /// Gets the optional routing category. Ignored if emtpy. If specified, it places additional
    /// matching requirement before post is handled.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the API technology kind. This is a string contain a single supported value,
    /// i.e. "Twitter" or "IMPv1".
    /// </summary>
    string ApiKind { get; }

    /// <summary>
    /// Gets optional user-agent string. Used where supported by the API.
    /// </summary>
    string UserAgent { get; }

}