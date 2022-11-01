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

using System.Net;
using KuiperZone.Implink.Api;
using KuiperZone.Implink.Api.Imp;

namespace KuiperZone.Implink;

/// <summary>
/// A container class for an instance of <see cref="ClientSession"/>, which also provides rate limiting.
/// </summary>
public class SessionContainer : IDisposable
{
    /// <summary>
    /// Constructor. This instance will own the session and dispose of it.
    /// </summary>
    public SessionContainer(ClientSession session)
    {
        Client = session;
        Counter = new(session.Profile.ThrottleRate);
    }

    /// <summary>
    /// Constructor in which the session is created according to
    /// <see cref="IReadOnlyRouteProfile.ApiKind"/>. Where remoteTerminated is false, the API kind is
    /// ignored as the session is always an instance of <see cref="ImpClientSession"/>.
    /// </summary>
    public SessionContainer(IReadOnlyRouteProfile profile, bool remoteTerminated)
        : this(remoteTerminated ? ClientFactory.Create(profile) : new ImpClientSession(profile, false))
    {

    }

    /// <summary>
    /// Gets a rate counter.
    /// </summary>
    public RateCounter Counter { get; }

    /// <summary>
    /// Gets the client.
    /// </summary>
    public ClientSession Client { get; }

    /// <summary>
    /// Calls <see cref="IClientApi.SubmitPostRequest"/> and truncates message if too long.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        var prof = Client.Profile;

        if (Counter.IsThrottled())
        {
            response = new();
            response.ErrorReason = "Request throttled";
            return (int)HttpStatusCode.TooManyRequests;
        }

        if (prof.MaxText > 3 && submit.Text.Length > prof.MaxText - 3)
        {
            // Make a clone
            submit = new SubmitPost(submit);
            submit.Text = submit.Text.Substring(0, prof.MaxText - 3) + "...";
        }

        return Client.SubmitPostRequest(submit, out response);
    }

    /// <summary>
    /// Implements disposal.
    /// </summary>
    public void Dispose()
    {
        Client.Dispose();
    }
}