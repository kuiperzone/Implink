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
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Api.Imp;

/// <summary>
/// Concrete implementation of <see cref="HttpClientSession"/> for the native IMP API client.
/// </summary>
public sealed class ImpClientSession : HttpClientSession
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ImpClientSession(IReadOnlyRouteProfile profile, bool remoteTerminated)
        : base(profile, new ImpSignerFactory(), remoteTerminated, "application/json")
    {
    }

    /// <summary>
    /// Implements <see cref="ClientSession.SubmitPostRequest(SubmitPost, out SubmitResponse)"/>.
    /// </summary>
    public override int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        Logger.Global.Debug(submit.ToString());
        var msg = new HttpRequestMessage(HttpMethod.Post, nameof(SubmitPost));
        msg.Content = new StringContent(submit.ToString(), Encoding.UTF8, "application/json");

        Logger.Global.Debug("Sending");
        var tuple = IsRemoteTerminated ? SignAndSend(msg) : Send(msg);

        Logger.Global.Debug(tuple.Item2);
        response = JsonSerializable.Deserialize<SubmitResponse>(tuple.Item2);
        response.ErrorReason ??= tuple.Item1.ToString();
        return (int)tuple.Item1;
    }
}