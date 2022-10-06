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
using KuiperZone.Implink.Routines.RoutingProfile;

namespace KuiperZone.Implink.Routines.Api.Thirdparty;

/// <summary>
/// Concrete implementation of <see cref="ClientSession"/> for the Twitter API. The API requires
/// the following authentication key-values be provisioned: "consumer_key", "consumer_secret".
/// </summary>
public sealed class TwitterClientSession : ClientSession, IClientApi
{
    // We are using third-party package:
    // https://github.com/CoreTweet/CoreTweet

    // FOR FUTURE REFERENCE:
    // https://stackoverflow.com/questions/38494279/how-do-i-get-an-oauth-2-0-authentication-token-in-c-sharp
    // https://www.thatsoftwaredude.com/content/6289/how-to-post-a-tweet-using-c-for-single-user
    // https://www.codeproject.com/Articles/1185880/ASP-NET-Core-WebAPI-secured-using-OAuth-Client-Cre
    private readonly object _syncObj = new();
    private readonly string _key;
    private readonly string _secret;
    private OAuth2Token? _token;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TwitterClientSession(IReadOnlyClientProfile profile)
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

        var temp = new Dictionary<string, string>();
        temp.Add("status", submit?.Text ?? "");

        try
        {
            lock (_syncObj)
            {
                var rslt = GetTokenNoSync().Statuses.UpdateAsync(temp, new CancellationTokenSource(Profile.Timeout).Token).Result;
                response.MsgId = rslt.Id.ToString(CultureInfo.InvariantCulture);
                return 200;
            }
        }
        catch (TwitterException e)
        {
            response.ErrorReason = e.Message;
            return (int)e.Status;
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            response.ErrorReason = e.InnerException.Message;
            return (int)HttpStatusCode.RequestTimeout;
        }
        catch (Exception e)
        {
            response.ErrorReason = e.InnerException?.Message ?? e.Message;
            return (int)HttpStatusCode.InternalServerError;
        }
    }

    private OAuth2Token GetTokenNoSync()
    {
        if (_token == null)
        {
            var opts = new ConnectionOptions();

            if (!string.IsNullOrEmpty(Profile.BaseAddress))
            {
                opts.ApiUrl = Profile.BaseAddress;
            }

            if (!string.IsNullOrEmpty(Profile.UserAgent))
            {
                opts.UserAgent = Profile.UserAgent;
            }

            _token = OAuth2.GetToken(_key, _secret, opts);
        }

        return _token;
    }
}