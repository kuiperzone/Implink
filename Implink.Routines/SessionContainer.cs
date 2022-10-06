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
using KuiperZone.Implink.Routines.RoutingProfile;

namespace KuiperZone.Implink.Routines;

/// <summary>
/// Interface which provide client request calls.
/// </summary>
public class SessionContainer : IClientApi, IDisposable
{
    private object _syncObj = new();
    private DateTime _epoch = DateTime.UtcNow;
    private long _counter;
    private ClientSession _session;

    public SessionContainer(IReadOnlyClientProfile profile)
    {
        Profile = profile;
        _session = ClientFactory.Create(profile);
    }

    /// <summary>
    /// Implements <see cref="IClientApi.Profile"/>.
    /// </summary>
    public IReadOnlyClientProfile Profile { get; }

    /// <summary>
    /// Gets requests per second.
    /// </summary>
    public double GetRate()
    {
        lock (_syncObj)
        {
            var c = Interlocked.Or(ref _counter, 0);
            var now = DateTime.UtcNow;
            var sec = (now - _epoch).TotalSeconds;

            if (sec < 0 || sec > 60)
            {
                sec = 60;
                _epoch = now;
                _counter = 0;
            }

            return c / sec;
        }
    }

    /// <summary>
    /// Implements <see cref="IClientApi.SubmitPostRequest"/>.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        if (Profile.ThrottleRate > 0 && GetRate() >= Profile.ThrottleRate)
        {
            response = new();
            response.ErrorReason = "Request throttled";
            return (int)HttpStatusCode.TooManyRequests;
        }

        try
        {
            var rslt = _session.SubmitPostRequest(submit, out response);
            Interlocked.Increment(ref _counter);
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