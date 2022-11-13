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
/// Defines supported request APIs. Values are stored in database and should be explicitly defined.
/// </summary>
public enum ApiKind
{
    /// <summary>
    /// A placeholder for an invalid value only. Not a valid protocol.
    /// </summary>
    None,

    /// <summary>
    /// IMP protocol version 1. By directional, i.e. both remote terminated and originated.
    /// </summary>
    ImpV1 = 1,

    /// <summary>
    /// Twitter third-party protocol. Remote terminated only.
    /// </summary>
    Twitter = 100,

    /// <summary>
    /// Twitter third-party protocol. Remote terminated only.
    /// </summary>
    Facebook = 101,
}

/// <summary>
/// Extension class.
/// </summary>
public static class ApiKindExt
{
    /// <summary>
    /// Returns true if kind is an IMP protocol (any version).
    /// </summary>
    public static bool IsImp(this ApiKind kind)
    {
        return kind == ApiKind.ImpV1;
    }

    /// <summary>
    /// Asserts IsImp() is true.
    /// </summary>
    /// <exception cref="ArgumentException">Not a valid IMP protocol</exception>
    public static void AssetImp(this ApiKind kind)
    {
        if (!IsImp(kind))
        {
            throw new ArgumentException("Not a valid IMP protocol");
        }
    }

}