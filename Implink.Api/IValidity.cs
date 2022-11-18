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
/// Interface allowing caller of a data type to assert validity.
/// </summary>
public interface IValidity
{
    /// <summary>
    /// Checks appropriate data and returns true if populated with legal values. The result
    /// is true on success and "message" is empty. If the return value is false, the message
    /// should contain an information string. A positive result is not a guarantee.
    /// </summary>
    bool CheckValidity(out string message);

    /// <summary>
    /// Asserts appropriate data is populated with legal values. On failure, it throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Failure message</exception>
    void AssertValidity()
    {
        if (!CheckValidity(out string message))
        {
            throw new ArgumentException(message);
        }
    }

}