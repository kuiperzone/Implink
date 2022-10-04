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

namespace KuiperZone.Implink.Routing;

/// <summary>
/// Routing profile for forward proxy. Interface is readonly.
/// </summary>
public interface IReadOnlyRoutingProfile
{
    /// <summary>
    /// Gets the mandatory routing name.
    /// </summary>
    string NameId { get; }

    /// <summary>
    /// Gets the mandatory routing category.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the API technology kind, i.e. "twitter" or "imp1".
    /// </summary>
    string Api { get; }

    /// <summary>
    /// Gets the partner API base URL. I.e. "https://api.twitter.com/2/" or "https://api.telegram.org/bot<token>/".
    /// The value should be terminated with "/".
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// Gets the vendor specific authentication properties. The value is a key-value sequence seperated by comma.
    /// I.e. of form "Key1=Value1,Key2=Value2". The call should assume keys and values are case-sensitive.
    /// </summary>
    string Authentication { get; }

    /// <summary>
    /// Gets optional user-agent string.
    /// </summary>
    string UserAgent { get; }

    /// <summary>
    /// Gets the request timeout in milliseconds. Default to 15000. A value or 0 or less is invalid.
    /// </summary>
    int Timeout { get; }

}