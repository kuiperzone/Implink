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
/// Implments <see cref="IReadOnlyRoutingProfile"/>.
/// </summary>
public class RoutingProfile : IReadOnlyRoutingProfile
{
    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.NameId"/> and provides setter.
    /// </summary>
    public string NameId { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.Category"/> and provides setter.
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.Api"/> and provides setter.
    /// </summary>
    public string Api { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.BaseAddress"/> and provides setter.
    /// </summary>
    public string BaseAddress { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.Authentication"/> and provides setter.
    /// </summary>
    public string Authentication { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.UserAgent"/> and provides setter.
    /// </summary>
    public string UserAgent { get; set; } = nameof(Implink);

    /// <summary>
    /// Implements <see cref="IReadOnlyRoutingProfile.Timeout"/> and provides setter.
    /// </summary>
    public int Timeout { get; set; } = 15000;

    /// <summary>
    /// Asserts that properties are (or at least appear) valid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Invalid RoutingProfile</exception>
    public void Assert()
    {
        if (!Validate(out string msg))
        {
            throw new InvalidOperationException($"Invalid {nameof(RoutingProfile)} {NameId} - {msg}");
        }
    }

    /// <summary>
    /// Validates the properties and returns true on success. If false, message will be set to an error string.
    /// </summary>
    public bool Validate(out string message)
    {
        if (string.IsNullOrWhiteSpace(NameId))
        {
            message = $"{NameId} undefined";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Api))
        {
            message = $"{Api} undefined";
            return false;
        }

        if (BaseAddress.Length <= "https://".Length)
        {
            message = $"{BaseAddress} undefined";
            return false;
        }

        if (!BaseAddress.StartsWith("https://") && !BaseAddress.StartsWith("http://"))
        {
            message = $"{BaseAddress} must start with https:// or http://";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Authentication))
        {
            message = $"{Authentication} undefined";
            return false;
        }

        if (Timeout < 1)
        {
            message = $"{Timeout} is zero or less";
            return false;
        }

        message = "";
        return true;
    }

}