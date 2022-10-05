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
    private readonly IEnumerable<IReadOnlyServerProfile>? _servers;
    private readonly IEnumerable<IReadOnlyClientProfile>? _clients;

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    public RoutingDatabase(string storageId, string connection)
        : base(storageId, connection)
    {
        if (Kind == FileKind)
        {
            ServerFilename = Path.Combine(Connection, "ServerProfiles.json");
            ClientFilename = Path.Combine(Connection, "ClientProfiles.json");
        }
    }

    /// <summary>
    /// Gets the inbound server filename. For test only.
    /// </summary>
    public readonly string? ServerFilename;

    /// <summary>
    /// Gets the outbound client filename. For test only.
    /// </summary>
    public readonly string? ClientFilename;

    /// <summary>
    /// Queries all server routes. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<IReadOnlyServerProfile> QueryAllServerRoutes()
    {
        if (ServerFilename != null)
        {
            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            try
            {
                var text = File.ReadAllText(ServerFilename, Encoding.UTF8);
                return JsonSerializer.Deserialize<ServerProfile[]>(text, opts) ?? Array.Empty<ServerProfile>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<ServerProfile>();
            }
        }

        return Query<ServerProfile>("SELECT STATEMENT TBD");
    }

    /// <summary>
    /// Queries all client routes. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<IReadOnlyClientProfile> QueryAllClientRoutes()
    {
        if (ClientFilename != null)
        {
            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            try
            {
                var text = File.ReadAllText(ClientFilename, Encoding.UTF8);
                return JsonSerializer.Deserialize<ClientProfile[]>(text, opts) ?? Array.Empty<ClientProfile>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<ClientProfile>();
            }
        }

        return Query<ClientProfile>("SELECT STATEMENT TBD");
    }

}
