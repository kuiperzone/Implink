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

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Subclass of <see cref="ProfileConsumerDictionary{T1,T2}"/> for <see cref="MessageRouter"/> classes.
/// </summary>
public class RouterDictionary : ProfileConsumerDictionary<IReadOnlyRouteProfile, MessageRouter>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public RouterDictionary(bool waitOnForward = false)
    {
        WaitOnForward = waitOnForward;
    }

    /// <summary>
    /// Gets the client dictionary.
    /// </summary>
    public ClientDictionary Clients { get; } = new();

    /// <summary>
    /// Gets whether routes should wait on forward.
    /// </summary>
    public bool WaitOnForward { get; }

    /// <summary>
    /// Implements.
    /// </summary>
    protected override MessageRouter CreateConsumer(IReadOnlyRouteProfile profile)
    {
        return new(profile, Clients, WaitOnForward);
    }
}

