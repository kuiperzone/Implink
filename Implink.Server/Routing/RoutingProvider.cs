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

using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace KuiperZone.Implink.Routing;

/// <summary>
/// Provides routing information for forward proxy.
/// </summary>
public sealed class RoutingProvider : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly Thread? _thread;
    private readonly ILogger? _logger;
    private volatile bool v_disposed;
    private volatile Dictionary<string, RoutingProfile>? v_profiles;

    /// <summary>
    /// Ensure thread stops.
    /// </summary>
    ~RoutingProvider()
    {
        v_disposed = true;
    }

    /// <summary>
    /// Constructor with WebApplication configuration and logger.
    /// </summary>
    public RoutingProvider(IConfiguration conf, ILogger? logger = null)
        : this(conf.GetValue<DbBackend>("DatabaseBackend"),
            conf["DatabaseConnection"],
            conf.GetValue<int>("DatabaseInterval"),
            logger)
    {
    }

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    public RoutingProvider(DbBackend backend, string connection, int refreshSeconds = 60, ILogger? logger = null)
    {
        Backend = backend;
        Connection = connection.Trim();
        RefreshSeconds = refreshSeconds;

        _logger = logger;
        v_profiles = Load(Backend, Connection);

        if (RefreshSeconds > 0)
        {
            _thread = new Thread(RefreshThread);
            _thread.IsBackground = true;
            _thread.Start();
        }
    }

    /// <summary>
    /// Gets the database backend.
    /// </summary>
    public readonly DbBackend Backend;

    /// <summary>
    /// Gets the connection string. For <see cref="DbBackend.File"/> this is a file path.
    /// </summary>
    public readonly string Connection;

    /// <summary>
    /// Gets the refresh seconds. A value of 0 or less implies no refresh. The value is approximate only.
    /// </summary>
    public readonly int RefreshSeconds;

    /// <summary>
    /// Gets a routing profile using name and category. If none matching, the result is null.
    /// </summary>
    public IReadOnlyRoutingProfile? Get(string nameId, string category)
    {
        if (v_profiles?.TryGetValue(Concat(nameId, category), out RoutingProfile? route) == true)
        {
            return route;
        }

        return null;
    }

    /// <summary>
    /// Implements disposal.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        v_disposed = true;
        v_profiles = null;
        _thread?.Interrupt();
    }

    private static string Concat(string nameId, string category)
    {
        return nameId.Trim().ToLowerInvariant() + '+' + category.Trim().ToLowerInvariant();
    }

    private static IDbConnection ConnectDb(DbBackend backend, string path)
    {
        switch(backend)
        {
            case DbBackend.MySql:
                return new SqlConnection(path);
            case DbBackend.PostgreSql:
                return new NpgsqlConnection(path);
            default:
                throw new ArgumentException($"Invalid {nameof(DbBackend)} value {backend}");
        }

    }

    private static Dictionary<string, RoutingProfile> LoadFromFile(string path)
    {
        var dict = new Dictionary<string, RoutingProfile>();
        var text = File.ReadAllText(path, Encoding.UTF8);
        return CreateDictionary(JsonSerializer.Deserialize<RoutingProfile[]>(text, JsonOpts));
    }

    private static Dictionary<string, RoutingProfile> LoadFromDatabase(DbBackend backend, string path)
    {
        // https://zetcode.com/csharp/dapper/
        using var con = ConnectDb(backend, path);
        con.Open();
        return CreateDictionary(con.Query<RoutingProfile>("SELECT * FROM cars"));

    }

    private static Dictionary<string, RoutingProfile> Load(DbBackend backend, string connection)
    {
        return backend == DbBackend.File ? LoadFromFile(connection) : LoadFromDatabase(backend, connection);
    }

    private static Dictionary<string, RoutingProfile> CreateDictionary(IEnumerable<RoutingProfile>? profiles)
    {
        var dictionary = new Dictionary<string, RoutingProfile>();

        if (profiles != null)
        {
            foreach (var item in profiles)
            {
                dictionary.Add(Concat(item.NameId, item.Category), item);
            }
        }

        return dictionary;
    }

    private void RefreshThread(object? _)
    {
        while (!v_disposed)
        {
            Thread.Sleep(RefreshSeconds);

            try
            {
                if (!v_disposed)
                {
                    // Lock free
                    v_profiles = Load(Backend, Connection);
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, $"{nameof(RoutingProvider)} failed to load profiles: {Connection}");
            }
        }

        v_profiles = null;
    }
}