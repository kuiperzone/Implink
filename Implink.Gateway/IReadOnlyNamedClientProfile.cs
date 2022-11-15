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

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Extends <see cref="IReadOnlyClientProfile"/>.
/// </summary>
public interface IReadOnlyNamedClientProfile : IReadOnlyClientProfile, IDictionaryKey, IValidity, IEquatable<IReadOnlyNamedClientProfile>
{
    /// <summary>
    /// Gets the mandatory unique identifier for the client. Example "LocalElgg", "Twitter1" etc.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the mandatory API technology kind. Cannot be None.
    /// </summary>
    ApiKind Api { get; }

    /// <summary>
    /// Gets whether to prefix username to message text. This is done because the "user account" provisioned
    /// with remote social media platforms may be that of the gateway and not that of the posting user.
    /// The default is true.
    /// </summary>
    bool PrefixUser { get; }

    /// <summary>
    /// Gets the maximum number of characters to permit in the post text. Messages longer than this will be truncated.
    /// A value of 0 or less does nothing.
    /// </summary>
    int MaxText { get; }

}