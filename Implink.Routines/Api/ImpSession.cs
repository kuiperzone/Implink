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

namespace KuiperZone.Implink.Routines.Api;

/// <summary>
/// Concrete implementation of <see cref="HttpSession"/> for the native IMP API client.
/// </summary>
public sealed class ImpSession : HttpSession
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public ImpSession(IReadOnlyOutboundRoute profile)
        : base(profile, new ImpSignerFactory(), "application/json")
    {
    }

    /// <summary>
    /// Implements <see cref="ClientSession.SubmitPostRequest(SubmitPost, out SubmitResponse)"/>.
    /// </summary>
    public override int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, nameof(SubmitPost));
        msg.Content = new StringContent(submit.ToString(), Encoding.UTF8, "application/json");

        var tuple = SignAndSend(msg);
        response = JsonBody.Deserialize<SubmitResponse>(tuple.Item3);
        response.ErrorInfo ??= tuple.Item2;
        return tuple.Item1;
    }
}