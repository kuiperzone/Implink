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
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Npgsql;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Database access which internally employs Dapper and supports MySql and PostgreSql.
/// Additionally, allows for "file database" for testing purposes.
/// </summary>
public sealed class ProfileDatabase : IDisposable
{
    /// <summary>
    /// Define consistent naming of table.
    /// </summary>
    public const string ClientTable = nameof(ClientProfile);

    /// <summary>
    /// Define consistent naming of table.
    /// </summary>
    public const string RouteTable = nameof(RouteProfile);

    private readonly string? _directory;
    private readonly IDbConnection? _sqlConnection;

    /// <summary>
    /// Constructor with parameters. If kind is file, connection is directory.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid kind or connection</exception>
    /// <exception cref="DirectoryNotFoundException">Invalid directory</exception>
    public ProfileDatabase(DatabaseKind kind, string connection)
    {
        Kind = kind;
        Connection = connection;

        if (string.IsNullOrEmpty(Connection))
        {
            throw new ArgumentException($"Undefined {nameof(Connection)}");
        }

        if (Kind == DatabaseKind.File)
        {
            var info = new FileInfo(Connection);

            if (!Directory.Exists(info.Directory?.FullName))
            {
                throw new DirectoryNotFoundException("File directory not exist: " + info.Directory?.FullName);
            }

            _directory = info.Directory.FullName;
        }
        else
        if (Kind == DatabaseKind.MySQL)
        {
            _sqlConnection = new SqlConnection(connection);
        }
        else
        if (Kind == DatabaseKind.Postgres)
        {
            _sqlConnection = new NpgsqlConnection(connection);
        }
        else
        {
            throw new ArgumentException("Invalid or unknown database kind: " + kind);
        }
    }

    /// <summary>
    /// Gets the database technology kind.
    /// </summary>
    public DatabaseKind Kind { get; }

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string Connection { get; }

    /// <summary>
    /// Gets or sets whether the connection is open. The connection is opened either by explicitly
    /// setting true, or automatically on first access. Setting the value may block.
    /// </summary>
    public bool IsOpen
    {
        get
        {
            return _sqlConnection == null ||
                _sqlConnection.State == ConnectionState.Open ||
                _sqlConnection.State == ConnectionState.Executing ||
                    _sqlConnection.State == ConnectionState.Fetching;
        }

        set
        {
            if (_sqlConnection != null && value != IsOpen)
            {
                if (!value || _sqlConnection.State == ConnectionState.Broken)
                {
                    _sqlConnection.Close();
                }

                if (value && _sqlConnection.State == ConnectionState.Closed)
                {
                    _sqlConnection.Open();
                }
            }
        }
    }

    /// <summary>
    /// Queries all clients. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<NamedClientProfile> QueryClients()
    {
        if (_directory != null)
        {
            return LoadFile<NamedClientProfile>(ClientTable);
        }

        return Query<NamedClientProfile>("SELECT STATEMENT TBD");
    }

    /// <summary>
    /// Queries all remote-terminated rotes. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<RouteProfile> QueryRoutes(bool remoteOriginated)
    {
        if (_directory != null)
        {
            var temp = new List<RouteProfile>();

            foreach (var item in LoadFile<RouteProfile>(RouteTable))
            {
                if (item.IsRemoteOriginated == remoteOriginated)
                {
                    temp.Add(item);
                }
            }

            return temp;
        }

        return Query<RouteProfile>("SELECT STATEMENT TBD");
    }

    /// <summary>
    /// Disposal pattern.
    /// </summary>
    public void Dispose()
    {
        _sqlConnection?.Dispose();
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public override string ToString()
    {
        return Kind + ": " + Connection;
    }

    private IEnumerable<T> LoadFile<T>(string fname)
    {
        if (_directory != null)
        {
            var path = Path.Combine(_directory, fname);
            Logger.Global.Debug("File path: " + path);

            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<T[]>(text, opts) ?? Array.Empty<T>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<T>();
            }
        }

        throw new InvalidOperationException($"{nameof(LoadFile)} operation not supported for ${Kind}");
    }

    private IEnumerable<T> Query<T>(string sql)
    {
        if (_sqlConnection != null)
        {
            // "SELECT * FROM cars"
            IsOpen = true;
            return _sqlConnection.Query<T>(sql);
        }

        throw new InvalidOperationException($"{nameof(Query)} operation not supported for ${Kind}");
    }

}
