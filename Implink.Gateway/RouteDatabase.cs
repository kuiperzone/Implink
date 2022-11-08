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
public sealed class RouteDatabase : IDisposable
{
    private readonly string? _filename;
    private readonly IDbConnection? _sqlConnection;

    /// <summary>
    /// Constructor with parameters. If kind is file, connection is directory.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid kind or connection</exception>
    /// <exception cref="DirectoryNotFoundException">Invalid directory</exception>
    public RouteDatabase(DatabaseKind kind, string connection, bool remoteTerminated)
    {
        Kind = kind;
        Connection = connection;
        IsRemoteTerminated = remoteTerminated;
        TableName = IsRemoteTerminated ? "RTRoutes" : "RORoutes";

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

            _filename = Path.Combine(info.Directory.FullName, TableName + ".json");
        }
        else
        if (Kind == DatabaseKind.File)
        {
            _sqlConnection = new SqlConnection(connection);
        }
        else
        if (Kind == DatabaseKind.File)
        {
            _sqlConnection = new NpgsqlConnection(connection);
        }
        else
        {
            throw new ArgumentException("Invalid or unknown database kind: " + kind);
        }
    }

    /// <summary>
    /// Gets whether remote terminated.
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    /// Gets the database technology kind.
    /// </summary>
    public DatabaseKind Kind { get; }

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string Connection { get; }

    /// <summary>
    /// Gets the applicable table name.
    /// </summary>
    public string TableName { get; }

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

    /// <summary>
    /// Queries all routes, either remote terminated or remote originated. The result is a new instance on each call.
    /// </summary>
    public IEnumerable<IReadOnlyClientProfile> QueryAllRoutes()
    {
        if (_filename != null)
        {
            var opts = new JsonSerializerOptions();
            opts.PropertyNameCaseInsensitive = true;

            try
            {
                Logger.Global.Debug("Filename: " + _filename);
                var text = File.ReadAllText(_filename, Encoding.UTF8);
                Logger.Global.Debug("Text: " + text);
                return JsonSerializer.Deserialize<ClientProfile[]>(text, opts) ?? Array.Empty<ClientProfile>();
            }
            catch (FileNotFoundException)
            {
                return Array.Empty<ClientProfile>();
            }
        }

        return Query<ClientProfile>("SELECT STATEMENT TBD");
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
