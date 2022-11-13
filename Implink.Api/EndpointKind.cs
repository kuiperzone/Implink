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

namespace KuiperZone.Implink.Api;

/// <summary>
/// Defines supported kndpoint kinds. Values are stored in database and should be explicitly defined.
/// </summary>
public enum EndpointKind
{
    /// <summary>
    /// Requests are generated locally, with the endpoint being a REMOTE TERMINATED entity,
    /// i.e. third-party service (Twitter) or other federated IMP server. All other values are
    /// to be considered REMOTE ORIGINATED.
    /// </summary>
    Remote = 0,

    /// <summary>
    /// The request are remote originated, with the endpoint being a local Elgg service.
    /// </summary>
    LocalElgg = 1,

    /// <summary>
    /// The request are remote originated, with the endpoint being a local MatterMost service.
    /// </summary>
    LocalMatterMost = 2,

}

