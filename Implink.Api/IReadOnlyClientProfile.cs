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
/// Readonly client configuration.
/// </summary>
public interface IReadOnlyClientProfile : IValidity
{
    /// <summary>
    /// Gets the mandatory API base URL. i.e. "https://api.twitter.com/2/".
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// Gets the vendor specific authentication properties. The value is a key-value sequence seperated by
    /// comma, i.e. of form "Key1=Value1,Key2=Value2". The caller should assume keys and values are case-sensitive.
    /// For IMPv1, the "SECRET" value must be given, specifying a minimum of 12 random characters.
    /// Example: "SECRET=Fyhf$34hjfTh94".
    /// </summary>
    string Authentication { get; }

    /// <summary>
    /// Gets optional user-agent string. Used where supported by the API.
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Gets the request timeout in milliseconds. Defaults to 15000. A value or 0 or less is invalid.
    /// </summary>
    int Timeout { get; }

    /// <summary>
    /// Disables SSL validation, where supported (typically only for IMP protocols). IMPORTANT. The value
    /// should invariably be set to false. Used primarily for testing. The default is false.
    /// </summary>
    bool DisableSslValidation { get; }

}