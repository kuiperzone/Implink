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

using System.Text;

namespace KuiperZone.Implink.Api;

/// <summary>
/// Concrete implementation of <see cref="HttpClientApi"/> for the native IMP API client.
/// </summary>
public sealed class ImpHttpClient : HttpClientApi
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ImpHttpClient(IReadOnlyClientProfile profile, bool remoteTerminated = true)
        : base(profile, remoteTerminated ? new ImpSignerFactory() : null, "application/json")
    {
        IsRemoteTerminated = remoteTerminated;
    }

    /// <summary>
    /// Gets whether the client is remote terminated, i.e. requests are sent out
    /// to third-party vendors. Where false, requests are sent over local network
    /// to an internal platform module (API is always native IMP).
    /// </summary>
    bool IsRemoteTerminated { get; }

    /// <summary>
    /// Implements <see cref="HttpClientApi.ToSubmitRequest"/>.
    /// </summary>
    protected override HttpRequestMessage ToSubmitRequest(SubmitPost submit)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, nameof(SubmitPost));
        msg.Content = new StringContent(submit.ToString(), Encoding.UTF8, "application/json");
        return msg;
    }

    /// <summary>
    /// Implements <see cref="HttpClientApi.ToSubmitResponse"/>.
    /// </summary>
    protected override SubmitResponse ToSubmitResponse(string body)
    {
        return Jsonizable.Deserialize<SubmitResponse>(body);
    }
}