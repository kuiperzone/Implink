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

using System.Globalization;
using System.Net;
using CoreTweet;

namespace KuiperZone.Implink.Routines.Api.Thirdparty;

/// <summary>
/// Concrete implementation of <see cref="ClientSession"/> for the Facebook API. The API requires
/// the following authentication key-values be provisioned: "consumer_key", "consumer_secret".
/// </summary>
public sealed class FacebookClientSession : ClientSession
{
    private readonly object _syncObj = new();
    private readonly string _key;
    private readonly string _secret;

    /// <summary>
    /// Constructor.
    /// </summary>
    public FacebookClientSession(IReadOnlyClientProfile profile)
        : base(profile)
    {
        _key = AuthDictionary["consumer_key"];
        _secret = AuthDictionary["consumer_secret"];
    }

    /// <summary>
    /// Implements <see cref="ClientSession.SubmitPostRequest(SubmitPost, out SubmitResponse)"/>.
    /// </summary>
    public override int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        response = new();

        try
        {
            lock (_syncObj)
            {
                // response.MsgId = rslt.Id.ToString(CultureInfo.InvariantCulture);
                response.ErrorInfo = "OK";
                return 200;
            }
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            response.ErrorInfo = e.InnerException.Message;
            return (int)HttpStatusCode.RequestTimeout;
        }
        catch (Exception e)
        {
            var msg = e.InnerException?.Message ?? e.Message;
            return (int)HttpStatusCode.InternalServerError;
        }
    }

}