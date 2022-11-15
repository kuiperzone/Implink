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
/// Readonly route data.
/// </summary>
public interface IReadOnlyRouteProfile : IDictionaryKey, IValidity, IEquatable<IReadOnlyRouteProfile>
{
    /// <summary>
    /// Gets the mandatory unique identifier. For remote-terminated routes, this is a group name and corresponds to
    /// <see cref="SubmitPost.GroupId"/> on an incoming message generated locally. For remote-originated routes,
    /// this is a gateway name and corresponds to <see cref="SubmitPost.GatewayId"/> on an incoming message
    /// received from an external partner.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets whether the route is remote-originated, in other words is intended to route incoming traffic from
    /// external partner gateways to local modules, such as Elgg or Mattermost. If false (default), the route is
    /// remote-terminated and routes outgoing to traffic to third-party services and external partners.
    /// </summary>
    bool IsRemoteOriginated { get; }

    /// <summary>
    /// Gets whether this route is enabled. This setting allows for profile to be disabled without deletion.
    /// Default is true (enabled).
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Gets a mandatory comma separated list of <see cref="IReadOnlyNamedClientProfile.Id"/> values.
    /// </summary>
    string Clients { get; }

    /// <summary>
    /// Gets the optional tags (or categories). This may contain a comma separated case insensitive list of tag
    /// or category names. If an incoming message matches the profile Id, it must also match one of these values,
    /// otherwise the request will be rejected. If <see cref="Tag"/> is empty or null, it does nothing.
    /// Example: "cats,dogs,animals".
    /// </summary>
    string? Tags { get; }

    /// <summary>
    /// Gets the mandatory API secret used for authentication of incoming remote-originated requests.
    /// It should not be populated for remote-terminated routes. Note that each <see cref="GatewayId"/> should
    /// have a unique secret value comprising a minimum of 12 random characters (excluding comma and space), to be
    /// shared with the remote party.
    /// </summary>
    string? Secret { get; }

    /// <summary>
    /// Gets a maximum request rate in terms of requests per minute. Incoming requests which exceed this rate will
    /// result in a 429 error response. A value of zero or less disables throttling. It is advisable to set this
    /// to set a positive non-zero value for remote-originated routes, and leave at 0 for remote-termined.
    /// Example: 10.
    /// </summary>
    int ThrottleRate { get; }
}