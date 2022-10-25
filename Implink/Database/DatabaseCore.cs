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

namespace KuiperZone.Implink.Database;

/// <summary>
/// Base class for database access which internally employs Dapper and supports MySql and PostgreSql.
/// Subclasses to provide specialised access.
/// </summary>
public class DatabaseCore : IDisposable
{
    /// <summary>
    /// MySQL.
    /// </summary>
    public const string MySqlKind = "mysql";

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    public const string PostgresKind = "postgres";

    /// <summary>
    /// File (connection is directory). Mainly for test use.
    /// </summary>
    public const string FileKind = "file";

    /// <summary>
    /// Test only.
    /// </summary>
    public const string TestKind = "test";

    private readonly IDbConnection? _sqlConnection;

    /// <summary>
    /// Constructor with parameters. Legal values for kind are "mysql", "postgres" or "file".
    /// </summary>
    /// <exception cref="ArgumentException">Invalid kind or connection</exception>
    /// <exception cref="DirectoryNotFoundException">Invalid directory</exception>
    public DatabaseCore(string kind, string connection)
    {
        Kind = kind.Trim().ToLowerInvariant();
        Connection = connection.Trim();

        if (Kind != TestKind)
        {
            if (string.IsNullOrEmpty(Connection))
            {
                throw new ArgumentException("Database connection string undefined");
            }

            switch (Kind)
            {
                case MySqlKind:
                    _sqlConnection = new SqlConnection(connection);
                    break;

                case PostgresKind:
                    _sqlConnection = new NpgsqlConnection(connection);
                    break;

                case FileKind:
                    var info = new FileInfo(Connection);

                    if (!Directory.Exists(info.Directory?.FullName))
                    {
                        throw new DirectoryNotFoundException("File directory not exist: " + info.Directory?.FullName);
                    }
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(Kind))
                    {
                        throw new ArgumentException("Database kind undefined");
                    }

                    throw new ArgumentException("Invalid database kind: " + kind);
            }
        }
    }

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

}
