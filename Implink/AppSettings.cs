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

using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink;

/// <summary>
/// Implements <see cref="IReadOnlyAppSettings"/>.
/// </summary>
public class AppSettings : IReadOnlyAppSettings
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public AppSettings()
    {
    }

    /// <summary>
    /// Constructor with <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> instance.
    /// </summary>
    public AppSettings(IConfiguration conf)
    {
        DatabaseKind = Enum.Parse<DatabaseKind>(conf[nameof(DatabaseKind)], true);
        DatabaseConnection = conf[nameof(DatabaseKind)].Trim();
        DatabaseRefresh = TimeSpan.FromSeconds(conf.GetValue<int>(nameof(DatabaseRefresh), 60));
        ResponseTimeout = TimeSpan.FromSeconds(conf.GetValue<int>(nameof(ResponseTimeout), 5000));
        RemoteTerminatedUrl = conf[nameof(RemoteTerminatedUrl)].Trim();
        RemoteOriginatedUrl = conf[nameof(RemoteOriginatedUrl)].Trim();
    }

    /// <summary>
    /// Implements <see cref="IReadOnlyAppSettings.DatabaseKind"/> and provides a setter.
    /// </summary>
    public DatabaseKind DatabaseKind { get; set; } = DatabaseKind.None;

    /// <summary>
    /// Implements <see cref="IReadOnlyAppSettings.DatabaseConnection"/> and provides a setter.
    /// </summary>
    public string DatabaseConnection { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyAppSettings.DatabaseRefresh"/> and provides a setter.
    /// </summary>
    public TimeSpan DatabaseRefresh { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Implements <see cref="IReadOnlyAppSettings.ResponseTimeout"/> and provides a setter.
    /// </summary>
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromMilliseconds(5000);

    /// <summary>
    /// Implements <see cref="IReadOnlyAppSettings.RemoteOriginatedUrl"/> and provides a setter.
    /// </summary>
    public string RemoteTerminatedUrl { get; set; } = "http://localhost:38668";

    /// <summary>
    /// Implements <see cref="IReadOnlyAppSettings.RemoteOriginatedUrl"/> and provides a setter.
    /// </summary>
    public string RemoteOriginatedUrl { get; set; } = "https://*:38669";

}