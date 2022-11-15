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
/// A serializable class which implements <see cref="IReadOnlyClientProfile"/> and provides setters.
/// </summary>
public class ClientProfile : Jsonizable, IReadOnlyClientProfile, IValidity
{
    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.BaseAddress"/> and provides a setter.
    /// </summary>
    public string BaseAddress { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Authentication"/> and provides a setter.
    /// </summary>
    public string Authentication { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.UserAgent"/> and provides a setter.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Timeout"/> and provides a setter.
    /// </summary>
    public int Timeout { get; set; } = 15000;

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.DisableSslValidation"/> and provides a setter.
    /// </summary>
    public bool DisableSslValidation { get; set; }

    /// <summary>
    /// Implements <see cref="Jsonizable.CheckValidity(out string)"/>.
    /// </summary>
    public override bool CheckValidity(out string message)
    {
        if (string.IsNullOrWhiteSpace(BaseAddress))
        {
            message = $"{nameof(BaseAddress)} empty";
            return false;
        }

        if (!Uri.IsWellFormedUriString(BaseAddress, UriKind.Absolute))
        {
            message = $"{nameof(BaseAddress)} not a valid URI";
            return false;
        }

        if (Timeout < 1)
        {
            message = $"{nameof(Timeout)} must be a non-zero positive value";
            return false;
        }

        message = "";
        return true;
    }

}