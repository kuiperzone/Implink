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

namespace KuiperZone.Implink.Routines.RoutingProfile;

/// <summary>
/// Readonly route data common to both client and server routing.
/// </summary>
public interface IReadOnlyRouteProfile : IEquatable<IReadOnlyRouteProfile>
{
    /// <summary>
    /// Gets the mandatory routing name. This is to be a "group name", although may in principle
    /// be a user name.
    /// </summary>
    string NameId { get; }

    /// <summary>
    /// Gets the mandatory API base URL. The value must begin with "https://" or "http://". For outbound, this would
    /// be an external partner or third-party service such as : "https://api.twitter.com/2/" or
    /// "https://api.telegram.org/bot<token>/". For inbound, it should specify the listening service on the local
    /// network, i.e. "http://localhost:38669". For LAN traffic, HTTP over HTTPS may be used.
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// Gets the vendor specific authentication properties. The value is a mandatory a key-value sequence seperated
    /// by comma. I.e. of form "Key1=Value1,Key2=Value2". The call should assume keys and values are case-sensitive.
    /// For IMPv1 on the public API, PRIVATE and PUBLIC key-values must be given, specifying a minimum of 12
    /// random characters each (take care to exclude comma). Example: "PRIVATE=Fyhf$34hjfTh94,PUBLIC=KvBd73!sdL84B".
    /// </summary>
    string Authentication { get; }

    /// <summary>
    /// Gets a maximum request rate in requests per minute. It applies per client. Requests above this rate will
    /// return a 429 error. A value of zero or less disables throttling. Advisable to specify a positive value
    /// for server side.
    /// </summary>
    int ThrottleRate { get; }

    /// <summary>
    /// Gets the request timeout in milliseconds. Defaults to 15000. A value or 0 or less is invalid.
    /// </summary>
    int Timeout { get; }

    /// <summary>
    /// Returns a unique key for the instance, formed from <see cref="NameId"/> and <see cref="BaseAddress"/>.
    /// </summary>
    string GetKey();

    /// <summary>
    /// Asserts that properties are (or at least appear) valid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid RoutingProfile</exception>
    void Assert();

}