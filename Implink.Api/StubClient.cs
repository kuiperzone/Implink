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
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Api;

/// <summary>
/// Implementation of <see cref="IMessagingClient"/> for a test stub. No messages are sent.
/// </summary>
public sealed class StubClient : IMessagingClient, IDisposable
{
    private int _respCounter;

    /// <summary>
    /// Constructor.
    /// </summary>
    public StubClient(IReadOnlyClientProfile profile)
    {
        profile.AssertValidity();
        Profile = profile;
    }

    /// <summary>
    /// Implements <see cref="IMessagingClient.Profile"/>.
    /// </summary>
    public IReadOnlyClientProfile Profile { get; }

    /// <summary>
    /// Implements <see cref="IMessagingApi.PostMessage(ImpMessage)"/>. Sending a message with Text containing
    /// the string equivalent of a <see cref="HttpStatusCode"/> value will cause it to be reflected back as
    /// the status in the response.
    /// </summary>
    public ImpResponse PostMessage(ImpMessage request)
    {
        Logger.Global.Debug("Sending: " + request.ToString());

        // Follow incoming text -- allow detection on receipt
        if (!Enum.TryParse<HttpStatusCode>(request.Text, true, out HttpStatusCode status))
        {
            return new ImpResponse(status);
        }

        if (!string.IsNullOrWhiteSpace(request.MsgId))
        {
            return new ImpResponse(request.MsgId);
        }

        return new ImpResponse("Stub-" + Interlocked.Increment(ref _respCounter));
    }

    /// <summary>
    /// Implements IDisposable.
    /// </summary>
    public void Dispose()
    {
    }

}