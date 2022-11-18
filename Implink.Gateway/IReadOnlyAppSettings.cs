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

using KuiperZone.Implink.Api;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Readonly application settings.
/// </summary>
public interface IReadOnlyAppSettings : IValidity
{
    /// <summary>
    /// Gets the logging threshold.
    /// </summary>
    SeverityLevel LoggingThreshold { get; }

    /// <summary>
    /// Gets the database technology kind.
    /// </summary>
    DatabaseKind DatabaseKind { get; }

    /// <summary>
    /// Gets the database connection. For <see cref="DatabaseKind.File"/>, this is a path to a directory.
    /// server=server;user=user;database=db;password=*****;
    /// </summary>
    string DatabaseConnection { get; }

    /// <summary>
    /// Gets the database refresh interval. A value of 0 disables refresh. Where specified in file,
    /// value is expressed in seconds.
    /// </summary>
    TimeSpan DatabaseRefresh { get; }

    /// <summary>
    /// Gets the response timeout. Where specified in file, value is expressed in milliseconds.
    /// </summary>
    TimeSpan ResponseTimeout { get; }

    /// <summary>
    /// Wait for full response leg. If false (default), the gateway responds immediately to a
    /// correctly composed request and forwards the message in a background thread. Set true
    /// for test only.
    /// </summary>
    bool WaitOnForward { get; }

    /// <summary>
    /// Gets the private server URL for remote terminated requests. This must always be on the
    /// internal LAN and may, therefore, be HTTP rather than HTTPS.
    /// </summary>
    string RemoteTerminatedUrl { get; }

    /// <summary>
    /// Gets the public server URL for remote originated requests. This will typically be exposed
    /// to the public internet and should be HTTPS.
    /// </summary>
    string RemoteOriginatedUrl { get; }

}