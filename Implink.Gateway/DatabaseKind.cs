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

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Database technology kind.
/// </summary>
public enum DatabaseKind
{
    /// <summary>
    /// Not a valid kind.
    /// </summary>
    None = 0,

    /// <summary>
    /// MySQL compatible database (i.e. MariaDB).
    /// </summary>
    MySQL,

    /// <summary>
    /// Postgres compatible database.
    /// </summary>
    Postgres,

    /// <summary>
    /// Json file (testing only).
    /// </summary>
    File
}