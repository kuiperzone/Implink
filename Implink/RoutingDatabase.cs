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
using KuiperZone.Implink.Api;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink;

/// <summary>
/// Extends <see cref="DatabaseCore"/> to provide route data.
/// </summary>
public class RoutingDatabase : DatabaseCore
{
    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    public RoutingDatabase(string kind, string connection)
        : base(kind, connection)
    {
        if (Kind == FileKind)
        {
            RTFilename = Path.Combine(Connection, "RTRoutes.json");
            ROFilename = Path.Combine(Connection, "RORoutes.json");
        }
    }

    /// <summary>
    /// Gets the outbound (remote terminated) filename. For test only.
    /// </summary>
    public string? RTFilename { get; }

    /// <summary>
    /// Gets the inbound (remote originated) filename. For test only.
    /// </summary>
    public string? ROFilename { get; }

    /// <summary>
    /// Queries all routes, either remote terminated or remote originated. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<IReadOnlyRouteProfile> QueryAllRoutes(bool remoteTerminated)
    {
        var fname = remoteTerminated ? RTFilename : ROFilename;

        if (fname != null)
        {
            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            try
            {
                Logger.Global.Debug("Filename: " + fname);
                var text = File.ReadAllText(fname, Encoding.UTF8);
                Logger.Global.Debug("Text: " + text);
                return JsonSerializer.Deserialize<RouteProfile[]>(text, opts) ?? Array.Empty<RouteProfile>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<RouteProfile>();
            }
        }

        return Query<RouteProfile>("SELECT STATEMENT TBD");
    }

}
