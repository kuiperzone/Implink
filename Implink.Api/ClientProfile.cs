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
public class ClientProfile : Jsonizable, IReadOnlyClientProfile, IValidity, IEquatable<IReadOnlyClientProfile>
{
    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.NameId"/> and provides a setter.
    /// </summary>
    public string NameId { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.BaseAddress"/> and provides a setter.
    /// </summary>
    public string BaseAddress { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Enpoint"/> and provides a setter.
    /// </summary>
    public EndpointKind Endpoint { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Api"/> and provides a setter.
    /// </summary>
    public ApiKind Api { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Categories"/> and provides a setter.
    /// </summary>
    public string? Categories { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Authentication"/> and provides a setter.
    /// </summary>
    public string Authentication { get; set; } = "";

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.UserAgent"/> and provides a setter.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.MaxText"/> and provides a setter.
    /// </summary>
    public int MaxText { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.ThrottleRate"/> and provides a setter.
    /// </summary>
    public int ThrottleRate { get; set; }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Timeout"/> and provides a setter.
    /// </summary>
    public int Timeout { get; set; } = 15000;

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.Enabled"/> and provides a setter.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.DisableSslValidation"/> and provides a setter.
    /// </summary>
    public bool DisableSslValidation { get; set; }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ClientProfile()
    {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public ClientProfile(IReadOnlyClientProfile other)
    {
        NameId = other.NameId;
        BaseAddress = other.BaseAddress;
        Endpoint = other.Endpoint;
        Api = other.Api;
        Categories = other.Categories;
        Authentication = other.Authentication;
        UserAgent = other.UserAgent;
        MaxText = other.MaxText;
        ThrottleRate = other.ThrottleRate;
        Timeout = other.Timeout;
        Enabled = other.Enabled;
        DisableSslValidation = other.DisableSslValidation;
    }

    /// <summary>
    /// Implements <see cref="IReadOnlyClientProfile.GetKey"/>.
    /// </summary>
    public string GetKey()
    {
        // Want this as a method, rather than property.
        return NameId.Trim().ToLowerInvariant() + "+" + BaseAddress.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Implements <see cref="Jsonizable.CheckValidity(out string)"/>.
    /// </summary>
    public override bool CheckValidity(out string message)
    {
        const string ClassName = nameof(ClientProfile);

        if (string.IsNullOrWhiteSpace(NameId))
        {
            message = $"{ClassName}.{nameof(ClientProfile.NameId)} empty";
            return false;
        }

        if (Api == ApiKind.None)
        {
            message = $"{ClassName}.{nameof(ClientProfile.Api)} invalid for {NameId}";
            return false;
        }

        if (Endpoint == EndpointKind.Remote && !BaseAddress.StartsWith("https://") && !BaseAddress.StartsWith("http://"))
        {
            message = $"{ClassName}.{nameof(ClientProfile.BaseAddress)} must start 'https://' or 'http://' for remote terminated {NameId} group";
            return false;
        }

        if (Timeout < 1)
        {
            message = $"{ClassName}.{nameof(ClientProfile.Timeout)} must be positive for {NameId}";
            return false;
        }

        message = "";
        return true;
    }

    /// <summary>
    /// Implements <see cref="IEquatable{T}"/>.
    /// </summary>
    public virtual bool Equals(IReadOnlyClientProfile? obj)
    {
        if (obj == this)
        {
            return true;
        }

        if (obj != null &&
            NameId == obj.NameId &&
            BaseAddress == obj.BaseAddress &&
            Endpoint == obj.Endpoint &&
            Api == obj.Api &&
            Categories == obj.Categories &&
            Authentication == obj.Authentication &&
            UserAgent == obj.UserAgent &&
            MaxText == obj.MaxText &&
            ThrottleRate == obj.ThrottleRate &&
            Timeout == obj.Timeout &&
            Enabled == obj.Enabled &&
            DisableSslValidation == obj.DisableSslValidation)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as IReadOnlyClientProfile);
    }

    /// <summary>
    /// Overrides.
    /// </summary>
    public sealed override int GetHashCode()
    {
        return HashCode.Combine(NameId.Trim().ToLowerInvariant(), BaseAddress.Trim().ToLowerInvariant());
    }
}