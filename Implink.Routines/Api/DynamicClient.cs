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
using KuiperZone.Implink.Routines.Api;
using KuiperZone.Implink.Routines.Api.Imp;
using KuiperZone.Implink.Routines.Util;

namespace KuiperZone.Implink.Routines.Gateways;

/// <summary>
/// A wrapper class for instances of <see cref="ClientSession"/>. This wrapper implements
/// <see cref="IClientApi"/>, calling on the internal session. It's purpose is twofold:
/// to allow dynamic contruction based on <see cref="IReadOnlyRouteProfile.ApiKind"/>, and
/// to add throttling to request calls.
/// </summary>
public class DynamicClient : IClientApi, IDisposable
{
    private readonly ClientSession _session;
    private readonly RateCounter _counter = new();

    /// <summary>
    /// Constructor. This instance will own the session and dispose of it.
    /// </summary>
    public DynamicClient(ClientSession session)
    {
        _session = session;
        Profile = session.Profile;
        IsRemoteTerminated = session.IsRemoteTerminated;
    }

    /// <summary>
    /// Constructor in which the session is created according to
    /// <see cref="IReadOnlyRouteProfile.ApiKind"/>. Where remoteTerminated is false, the API kind is
    /// ignored as the session is always an instance of <see cref="ImpClientSession"/>.
    /// </summary>
    public DynamicClient(IReadOnlyRouteProfile profile, bool remoteTerminated)
    {
        Profile = profile;
        IsRemoteTerminated = remoteTerminated;
        _session = remoteTerminated ? ClientFactory.Create(profile) : new ImpClientSession(profile, false);
    }

    /// <summary>
    /// Implements <see cref="IClientApi.IsRemoteTerminated"/>.
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public IReadOnlyRouteProfile Profile { get; }

    /// <summary>
    /// Implements <see cref="IClientApi.SubmitPostRequest"/>.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        if (_counter.IsThrottled(Profile.ThrottleRate))
        {
            response = new();
            response.ErrorReason = "Request throttled";
            return (int)HttpStatusCode.TooManyRequests;
        }

        try
        {
            var rslt = _session.SubmitPostRequest(submit, out response);
            _counter.Increment();
            return rslt;
        }
        catch (Exception e)
        {
            response = new();
            response.ErrorReason = e.InnerException?.Message ?? e.Message;
            return (int)HttpStatusCode.InternalServerError;
        }
    }

    /// <summary>
    /// Implements disposal.
    /// </summary>
    public void Dispose()
    {
        _session.Dispose();
    }
}