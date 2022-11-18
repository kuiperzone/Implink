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
using KuiperZone.Implink.Api.Thirdparty;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Implements <see cref="IReadOnlyNamedClientProfile"/> and extends <see cref="ClientProfile"/>.
/// </summary>
public class NamedClientProfile : ClientProfile, IReadOnlyNamedClientProfile, IDictionaryKey, IValidity, IEquatable<IReadOnlyNamedClientProfile>
{
    /// <summary>
    /// Implements <see cref="IReadOnlyNamedClientProfile.Id"/> and provides a setter.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyNamedClientProfile.Kind"/> and provides a setter.
    /// </summary>
    public ClientKind Kind { get; set; } = ClientKind.None;

    /// <summary>
    /// Implements <see cref="IReadOnlyNamedClientProfile.PrefixUser"/> and provides a setter.
    /// </summary>
    public bool PrefixUser { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyNamedClientProfile.MaxText"/> and provides a setter.
    /// </summary>
    public int MaxText { get; set; }

    /// <summary>
    /// Overrides.
    /// </summary>
    public override bool CheckValidity(out string message)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            message = $"{nameof(Id)} empty for {nameof(ClientProfile)}";
            return false;
        }

        string suffix = $" for {nameof(Id)}={Id}";

        if (Kind == ClientKind.None)
        {
            message = $"{nameof(Kind)} cannot be {nameof(ClientKind.None)} {suffix}";
            return false;
        }

        if (!base.CheckValidity(out message))
        {
            message += suffix;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Implements <see cref="IDictionaryKey.GetKey"/>.
    /// </summary>
    public string GetKey()
    {
        return Id;
    }

    /// <summary>
    /// Implements <see cref="IEquatable{T}"/>.
    /// </summary>
    public bool Equals(IReadOnlyNamedClientProfile? obj)
    {
        if (obj == this)
        {
            return true;
        }

        return obj != null &&
            BaseAddress == obj.BaseAddress &&
            Secret == obj.Secret &&
            UserAgent == obj.UserAgent &&
            Timeout == obj.Timeout &&
            DisableSslValidation == obj.DisableSslValidation &&

            Id == obj.Id &&
            Kind == obj.Kind &&
            MaxText == obj.MaxText;
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as IReadOnlyNamedClientProfile);
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public sealed override int GetHashCode()
    {
        return Id.ToLowerInvariant().GetHashCode();
    }

}