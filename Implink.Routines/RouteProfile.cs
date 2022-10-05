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
/// A serializable class which implements <see cref="IReadOnlyRouteProfile"/> and provides setters.
/// </summary>
public class RouteProfile : JsonSerializable, IReadOnlyRouteProfile, IEquatable<IReadOnlyRouteProfile>
{
    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.NameId"/> and provides a setter.
    /// </summary>
    public string NameId { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.BaseAddress"/> and provides a setter.
    /// </summary>
    public string BaseAddress { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Authentication"/> and provides a setter.
    /// </summary>
    public string Authentication { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Timeout"/> and provides a setter.
    /// </summary>
    public int Timeout { get; set; } = 15000;

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.GetKey"/>.
    /// </summary>
    public string GetKey()
    {
        // Want this as a method, rather than property.
        return NameId + "+" + BaseAddress;
    }

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Assert"/>.
    /// </summary>
    public virtual void Assert()
    {
        if (string.IsNullOrWhiteSpace(NameId))
        {
            throw new InvalidOperationException($"{NameId} undefined");
        }

        if (!BaseAddress.StartsWith("https://") && !BaseAddress.StartsWith("http://"))
        {
            throw new InvalidOperationException($"{BaseAddress} must start with https:// or http://");
        }

        if (BaseAddress.Length <= "https://".Length)
        {
            throw new InvalidOperationException($"{BaseAddress} undefined");
        }

        if (Timeout < 1)
        {
            throw new InvalidOperationException($"{Timeout} is zero or less");
        }
    }

    /// <summary>
    /// Implements <see cref="IEquatable{T}"/>.
    /// </summary>
    public virtual bool Equals(IReadOnlyRouteProfile? obj)
    {
        if (obj == this)
        {
            return true;
        }

        if (obj != null && NameId == obj.NameId && BaseAddress == obj.BaseAddress &&
            Authentication == obj.Authentication && Timeout == obj.Timeout)
        {
            return GetType() == obj.GetType();
        }

        return false;
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as IReadOnlyRouteProfile);
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(NameId, BaseAddress);
    }
}