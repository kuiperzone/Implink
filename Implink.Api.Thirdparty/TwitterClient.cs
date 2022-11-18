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
using KuiperZone.Implink.Api.Util;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Api.Thirdparty;

/// <summary>
/// Implementation of <see cref="IMessagingClient"/> for the Twitter API. The API requires
/// the following authentication key-values be provisioned: "consumer_key", "consumer_secret".
/// </summary>
public sealed class TwitterClient : IMessagingClient, IDisposable
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
    public TwitterClient(IReadOnlyClientProfile profile)
    {
        profile.AssertValidity();
        Profile = profile;

        var temp = StringParser.ToDictionary(Profile.Secret);

        // Will throw if not configured
        _key = temp["consumer_key"];
        _secret = temp["consumer_secret"];
    }

    /// <summary>
    /// Implements <see cref="IMessagingClient.Profile"/>.
    /// </summary>
    public IReadOnlyClientProfile Profile { get; }

    /// <summary>
    /// Implements <see cref="IMessagingApi.PostMessage(ImpMessage)"/>.
    /// </summary>
    public ImpResponse PostMessage(ImpMessage request)
    {
        Logger.Global.Debug("Sending: " + request.ToString());
        ((IValidity)request).AssertValidity();

        var temp = new Dictionary<string, string>();
        temp.Add("status", request.Text ?? "");

        if (!string.IsNullOrEmpty(request.ParentMsgId))
        {
            if (!long.TryParse(request.ParentMsgId, out long _))
            {
                return new ImpResponse(HttpStatusCode.BadRequest, $"{nameof(request.ParentMsgId)} not a Twitter id)");
            }

            temp.Add("in_reply_to_status_id", request.ParentMsgId);
        }

        try
        {
            lock (_syncObj)
            {
                var rslt = GetTokenNoSync().Statuses.UpdateAsync(temp, new CancellationTokenSource(Profile.Timeout).Token).Result;
                return new ImpResponse(HttpStatusCode.OK, rslt.Id.ToString(CultureInfo.InvariantCulture));
            }
        }
        catch (TwitterException e)
        {
            return new ImpResponse(e.Status, e.Message);
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            return new ImpResponse(HttpStatusCode.RequestTimeout, e.InnerException.Message);
        }
        catch (Exception e)
        {
            return new ImpResponse(HttpStatusCode.InternalServerError, e.InnerException?.Message ?? e.Message);
        }
    }

    /// <summary>
    /// Implements IDisposable.
    /// </summary>
    public void Dispose()
    {
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