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
/// A serializable class which implements <see cref="IReadOnlyRouteProfile"/> and provides setters.
/// </summary>
public class RouteProfile : Jsonizable, IReadOnlyRouteProfile, IValidity
{
    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Id"/> and provides a setter.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.IsRemoteOriginated"/> and provides a setter.
    /// </summary>
    public bool IsRemoteOriginated { get; }

     /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Enabled"/> and provides a setter.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Clients"/> and provides a setter.
    /// </summary>
    public string Clients { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Tags"/> and provides a setter.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.Secret"/> and provides a setter.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyRouteProfile.ThrottleRate"/> and provides a setter.
    /// </summary>
    public int ThrottleRate { get; set; }

    /// <summary>
    /// Implements <see cref="IDictionaryKey.GetKey"/>.
    /// </summary>
    public string GetKey()
    {
        return Id;
    }

    /// <summary>
    /// Implements <see cref="Jsonizable.CheckValidity(out string)"/>.
    /// </summary>
    public override bool CheckValidity(out string message)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            message = $"{nameof(Id)} empty for {nameof(RouteProfile)}";
            return false;
        }

        string suffix = $" for {nameof(RouteProfile)}.{nameof(Id)}={Id}";

        if (string.IsNullOrWhiteSpace(Clients))
        {
            message = $"{nameof(Clients)} empty";
            return false;
        }

        message = "";
        return true;
    }

    /// <summary>
    /// Implements <see cref="IEquatable{T}"/>.
    /// </summary>
    public bool Equals(IReadOnlyRouteProfile? obj)
    {
        if (obj == this)
        {
            return true;
        }

        return obj != null &&

            Id == obj.Id &&
            Enabled == obj.Enabled &&
            IsRemoteOriginated == obj.IsRemoteOriginated &&
            Clients == obj.Clients &&
            Tags == obj.Tags &&
            Secret == obj.Secret &&
            ThrottleRate == obj.ThrottleRate;
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
        return Id.ToLowerInvariant().GetHashCode();
    }
}