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

namespace KuiperZone.Implink.Database;

/// <summary>
/// Extends <see cref="DatabaseCore"/> to provide route data.
/// </summary>
public class RoutingDatabase : DatabaseCore
{
    private readonly IEnumerable<IReadOnlyRouteProfile>? _remotes;
    private readonly IEnumerable<IReadOnlyRouteProfile>? _locals;

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    public RoutingDatabase(string kind, string connection)
        : base(kind, connection)
    {
        if (Kind == FileKind)
        {
            LocalFilename = Path.Combine(Connection, "LocalRoutes.json");
            RemoteFilename = Path.Combine(Connection, "RemoteRoutes.json");
        }
    }

    /// <summary>
    /// For use in testing only.
    /// </summary>
    public RoutingDatabase(IEnumerable<IReadOnlyRouteProfile> remotes, IEnumerable<IReadOnlyRouteProfile>? locals = null)
        : base(TestKind, "")
    {
        _remotes = remotes;
        _locals = locals ?? Array.Empty<IReadOnlyRouteProfile>();
    }

    /// <summary>
    /// Gets the inbound (remote originated) filename. For test only.
    /// </summary>
    public string? LocalFilename { get; }

    /// <summary>
    /// Gets the outbound (remote terminated) filename. For test only.
    /// </summary>
    public string? RemoteFilename { get; }

    /// <summary>
    /// Queries all routes, either remote terminated or remote originated. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<IReadOnlyRouteProfile> QueryAllRoutes(bool remoteTerminated)
    {
        var fname = remoteTerminated ? RemoteFilename : LocalFilename;

        if (fname != null)
        {
            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            try
            {
                var text = File.ReadAllText(fname, Encoding.UTF8);
                return JsonSerializer.Deserialize<RouteProfile[]>(text, opts) ?? Array.Empty<RouteProfile>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<RouteProfile>();
            }
        }

        if (_remotes != null && remoteTerminated)
        {
            return _remotes;
        }

        if (_locals != null && !remoteTerminated)
        {
            return _locals;
        }

        return Query<RouteProfile>("SELECT STATEMENT TBD");
    }

}
