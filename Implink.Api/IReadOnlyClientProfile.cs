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

namespace KuiperZone.Implink.Api;

/// <summary>
/// Readonly route profile data common to both client and server routing.
/// </summary>
public interface IReadOnlyClientProfile : IValidity, IEquatable<IReadOnlyClientProfile>
{
    /// <summary>
    /// Gets the mandatory routing name (case insensitive).
    /// </summary>
    string NameId { get; }

    /// <summary>
    /// Gets the optional routing categories. It may contain a comma separated case insensitive list of category
    /// names. If an incoming submission matches <see cref="NameId"/>, it must also match one of the values in
    /// <see cref="Categories"/> if not empty.
    /// </summary>
    string? Categories { get; }

    /// <summary>
    /// Gets the mandatory API technology kind. This is a string containing a single supported value,
    /// i.e. "Twitter" or "IMPv1".
    /// </summary>
    string ApiKind { get; }

    /// <summary>
    /// Disables SSL validation, where supported (typically only for IMP protocols). IMPORTANT. The value
    /// should invariably be set to false. Used primarily for testing.
    /// </summary>
    bool DisableSslValidation { get; }

    /// <summary>
    /// Gets the mandatory API base URL. The value must begin with "https://" or "http://". For outbound, this would
    /// be an external partner or third-party service such as : "https://api.twitter.com/2/" or
    /// "https://api.telegram.org/bot{token}/". For inbound, it should specify the listening service on the local
    /// network, i.e. "http://localhost:38669". For LAN traffic, HTTP (rather than HTTPS) may be used.
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// Gets the vendor specific authentication properties. The value is a key-value sequence seperated by comma,
    /// i.e. of form "Key1=Value1,Key2=Value2". The caller should assume keys and values are case-sensitive. For IMPv1,
    /// PRIVATE and PUBLIC key-values must be given, specifying a minimum of 12 random characters each
    /// (take care to exclude comma). Example: "PRIVATE=Fyhf$34hjfTh94,PUBLIC=KvBd73!sdL84B".
    /// </summary>
    string Authentication { get; }

    /// <summary>
    /// Gets optional user-agent string. Used where supported by the API.
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Gets the maximum number of characters post text. Messages longer than this will be truncated.
    /// A value of 0 or less does nothing.
    /// </summary>
    int MaxText { get; }

    /// <summary>
    /// Gets a maximum request rate in requests per minute. It applies per client. Requests above this rate will
    /// return a 429 error. A value of zero or less disables throttling. Advisable to specify a positive value
    /// for remote originated server side. On the remote terminated side, this will prevent gateway from flooding
    /// destination.
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

}