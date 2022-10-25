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
/// A base class for response body content.
/// </summary>
public class ResponseMessage : JsonSerializable, IValidity
{
    /// <summary>
    /// Gets or sets an optional error reason message.
    /// </summary>
    public string? ErrorReason { get; set; }

    /// <summary>
    /// Implements <see cref="JsonSerializable.CheckValidity(out string)"/> but always returns true.
    /// </summary>
    public override bool CheckValidity(out string message)
    {
        message = "";
        return true;
    }
}
