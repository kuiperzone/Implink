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
/// Readonly route profile data common to both client and server routing.
/// </summary>
public interface IReadOnlyClientProfile : IValidity, IEquatable<IReadOnlyClientProfile>
{
    /// <summary>
    /// Gets the mandatory routing name (case insensitive). For remote terminated (RT) profiles, it corresponds to
    /// a <see cref="SubmitPost.GroupName"/>. For remote originated (RO) profiles, it corresponds to
    /// <see cref="SubmitPost.UserName"/>.
    /// </summary>
    string NameId { get; }

    /// <summary>
    /// Gets the end-point kind. This specifies whether requests are to be remote terminated (RT) or originated (RO).
    /// If remote originated, the values implicitly identifies the local destination address.
    /// </summary>
    EndpointKind Endpoint { get; }

    /// <summary>
    /// Gets the API base URL. It is mandatory where <see cref="Endpoint"/> is <see cref="EndpointKind.Remote"/>
    /// and should, in this case, specify the base address of the external partner or third-party API service,
    /// i.e. "https://api.twitter.com/2/". It is not used (and should be left blank) for remote originated requests,
    /// as the local end-point can be deduced from <see cref="Endpoint"/>.
    /// </summary>
    string BaseAddress { get; }

    /// <summary>
    /// Gets the mandatory API technology kind.
    /// </summary>
    ApiKind Api { get; }

    /// <summary>
    /// Gets the optional routing categories. It may contain a comma separated case insensitive list of category
    /// names. If an incoming submission matches <see cref="NameId"/>, it must also match one of the values in
    /// <see cref="Categories"/> if not empty.
    /// </summary>
    string? Categories { get; }

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
    /// Gets the maximum number of characters to permit in the post text. Messages longer than this will be truncated.
    /// A value of 0 or less does nothing.
    /// </summary>
    int MaxText { get; }

    /// <summary>
    /// Gets a maximum request rate in terms of requests per minute. It applies per client. Requests above this rate
    /// will return a 429 error. A value of zero or less disables throttling. Advisable to specify a positive value
    /// for remote originated server side. On the remote terminated side, this will prevent gateway from flooding
    /// destination.
    /// </summary>
    int ThrottleRate { get; }

    /// <summary>
    /// Gets the request timeout in milliseconds. Defaults to 15000. A value or 0 or less is invalid.
    /// </summary>
    int Timeout { get; }

    /// <summary>
    /// Gets whether this profile is enabled. This setting allows for profile to be disabled without deletion.
    /// Default is true (enabled).
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Disables SSL validation, where supported (typically only for IMP protocols). IMPORTANT. The value
    /// should invariably be set to false. Used primarily for testing. The default is false.
    /// </summary>
    bool DisableSslValidation { get; }

    /// <summary>
    /// Returns a unique Id for the instance, formed from <see cref="NameId"/> and <see cref="BaseAddress"/> in
    /// lowercase (invariant culture) and separated by "+". Ie. "nameid+baseaddress".
    /// </summary>
    string GetKey();

}