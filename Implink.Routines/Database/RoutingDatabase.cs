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
using System.Text.Json;

namespace KuiperZone.Implink.Routines.Database;

/// <summary>
/// Extends <see cref="DatabaseCore"/> to provide route data.
/// </summary>
public class RoutingDatabase : DatabaseCore
{
    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    public RoutingDatabase(string storageId, string connection)
        : base(storageId, connection)
    {
    }

    /// <summary>
    /// Loads all outbound routes. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<IReadOnlyOutboundRoute> QueryOutboundRoutes()
    {
        if (Kind == FileKind)
        {
            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            var text = File.ReadAllText(Connection, Encoding.UTF8);
            return JsonSerializer.Deserialize<OutboundRoute[]>(text, opts) ?? Array.Empty<OutboundRoute>();
        }

        return Query<OutboundRoute>("SELECT STATEMENT TBD");
    }

}
