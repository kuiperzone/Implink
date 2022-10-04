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

namespace KuiperZone.Implink.Routines;

/// <summary>
/// Readonly outbound route data. This describes data flow originating from internal posts which
/// need to be shared with outside vendors.
/// </summary>
public interface IReadOnlyOutboundRoute
{
    /// <summary>
    /// Gets the mandatory routing name.
    /// </summary>
    string NameId { get; }

    /// <summary>
    /// Gets the optional routing category. Ignored if emtpy. If specified, it places additional
    /// matching requirement before post is handled.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the API technology kind. This is a string contain a single supported value,
    /// i.e. "Twitter" or "IMPv1".
    /// </summary>
    string ApiKind { get; }

    /// <summary>
    /// Gets the mandatory partner API base URL. I.e. "https://api.twitter.com/2/" or "https://api.telegram.org/bot<token>/".
    /// The value should be terminated with "/".
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// Gets the vendor specific authentication properties. The value is a key-value sequence seperated by comma.
    /// I.e. of form "Key1=Value1,Key2=Value2". The call should assume keys and values are case-sensitive.
    /// The value is mandatory (set to dummy string in event not used).
    /// </summary>
    string Authentication { get; }

    /// <summary>
    /// Gets optional user-agent string. Used where supported by the API.
    /// </summary>
    string UserAgent { get; }

    /// <summary>
    /// Gets the request timeout in milliseconds. Default to 15000. A value or 0 or less is invalid.
    /// </summary>
    int Timeout { get; }

}