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
using Dapper;
using Npgsql;

namespace KuiperZone.Implink.Routines.Database;

/// <summary>
/// Base class for database access which internally employs Dapper and supports MySql and PostgreSql.
/// Subclasses to provide specialised access. Additionally, file data may be supported by subclasses (mainly for
/// test and development use). For file data, the connection string provides the file path.
/// </summary>
public class DatabaseCore : IDisposable
{
    private readonly IDbConnection? _sqlConnection;

    /// <summary>
    /// Constructor with parameters. Legal values for kind are "mysql", "postgresql" or "file".
    /// </summary>
    /// <exception cref="ArgumentException">Invalid kind or connection</exception>
    /// <exception cref="DirectoryNotFoundException">Invalid directory</exception>
    public DatabaseCore(string kind, string connection)
    {
        Kind = AssertKind(kind);
        Connection = connection.Trim();

        if (string.IsNullOrEmpty(Connection))
        {
            throw new ArgumentException("Database connection string undefined");
        }

        _sqlConnection = CreateConnection(Kind, Connection);

        if (_sqlConnection == null)
        {
            // Allow file not exist (it may be written later), but directory must exist.
            var info = new FileInfo(Connection);

            if (!info.Exists && !Directory.Exists(info.Directory?.FullName))
            {
                throw new DirectoryNotFoundException("File directory not exist: " + info.Directory?.FullName);
            }
        }
    }

    /// <summary>
    /// Json file. Connection string to specify file path.
    /// </summary>
    public const string FileKind = "file";

    /// <summary>
    /// MySQL.
    /// </summary>
    public const string MySqlKind = "mysql";

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    public const string PostgreSqlKind = "postgresql";

    /// <summary>
    /// Gets the database backend kind.
    /// </summary>
    public readonly string Kind;

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public readonly string Connection;

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
    /// Runs a SQL query. Throws InvalidOperationException for "file" kind.
    /// </summary>
    public IEnumerable<T> Query<T>(string sql)
    {
        if (_sqlConnection != null)
        {
            // "SELECT * FROM cars"
            IsOpen = true;
            return _sqlConnection.Query<T>(sql);
        }

        throw new InvalidOperationException($"{nameof(Query)} operation not supported for ${FileKind}");
    }

    /// <summary>
    /// Disposal pattern.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(false);
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public override string ToString()
    {
        return Kind + ": " + Connection;
    }

    /// <summary>
    /// Disposal pattern.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        _sqlConnection?.Dispose();
    }

    private static string AssertKind(string? id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Database kind undefined");
        }

        id = id.Trim().ToLowerInvariant();

        if (id != FileKind && id != MySqlKind && id != PostgreSqlKind)
        {
            throw new ArgumentException("Invalid database kind: " + id);
        }

        return id;
    }

    private static IDbConnection? CreateConnection(string id, string connection)
    {
        if (id == MySqlKind)
        {
            return new SqlConnection(connection);
        }

        if (id == PostgreSqlKind)
        {
            return new NpgsqlConnection(connection);
        }

        return null;
    }

}
