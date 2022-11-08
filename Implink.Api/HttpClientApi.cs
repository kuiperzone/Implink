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
using System.Net.Http.Headers;
using System.Text;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Api;

/// <summary>
/// Abstract base class which extends <see cref="ClientApi"/> to implement an internal
/// <see cref="HttpClient"/> instance. It does not, however, implement the API conversion.
/// </summary>
public abstract class HttpClientApi : ClientApi, IClientApi, IDisposable
{
    private readonly HttpClient _client;

    /// <summary>
    /// Constructor. If factory is null, the instance will have no signer.
    /// </summary>
    public HttpClientApi(IReadOnlyClientProfile profile, ISignerFactory? factory, string? contentType = null)
        : base(profile)
    {
        if (profile.DisableSslValidation)
        {
            Logger.Global.Debug("SSL VALIDATION DISABLED");
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; };
            _client = HttpClientFactory.Create(handler);
        }
        else
        {
            Logger.Global.Debug("SSL validation");
            _client = HttpClientFactory.Create();
        }

        // https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working
        // You must place a slash at the end of the BaseAddress, and you must not place a slash at the beginning of your relative URI.
        _client.BaseAddress = new Uri(new Uri(Profile.BaseAddress).AbsoluteUri.Trim('/') + '/');
        _client.Timeout = TimeSpan.FromMilliseconds(Profile.Timeout);

        Signer = factory?.Create(this);

        if (!string.IsNullOrEmpty(Profile.UserAgent))
        {
            _client.DefaultRequestHeaders.Add("User-Agent", Profile.UserAgent);
        }

        if (!string.IsNullOrEmpty(contentType))
        {
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
        }
    }

    /// <summary>
    /// Gets the instance of <see cref="IHttpSigner"/>. The value may be null if none supplied on construction.
    /// </summary>
    public readonly IHttpSigner? Signer;

    /// <summary>
    /// Implements <see cref="ClientApi.SubmitPostRequest(SubmitPost, out SubmitResponse)"/>.
    /// </summary>
    public override int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        Logger.Global.Debug(submit.ToString());
        var msg = ToSubmitRequest(submit);

        Logger.Global.Debug("Sending");
        var tuple = SignAndSend(msg);

        Logger.Global.Debug("Translate response");
        Logger.Global.Debug(tuple.Item2);
        response = ToSubmitResponse(tuple.Item2);

        if (string.IsNullOrEmpty(response.ErrorReason))
        {
            response.ErrorReason ??= tuple.Item1.ToString();
        }

        return (int)tuple.Item1;
    }

    /// <summary>
    /// Must be implemented to translate <see cref="SubmitPost"/> to HttpRequestMessage.
    /// </summary>
    protected abstract HttpRequestMessage ToSubmitRequest(SubmitPost submit);

    /// <summary>
    /// Must be implemented to translate response body text to <see cref="SubmitResponse"/>.
    /// </summary>
    protected abstract SubmitResponse ToSubmitResponse(string response);

    /// <summary>
    /// Calls <see cref="IHttpSigner.Add"/> where <see cref="ClientApi.AuthDictionary"/> is not empty,
    /// and returns the result of <see cref="Send"/>. This method is provided for convenience and is called
    /// by the default implementation of <see cref="ClientApi.SubmitPostRequest"/>. It does not throw.
    /// </summary>
    protected Tuple<HttpStatusCode, string> SignAndSend(HttpRequestMessage request)
    {
        Logger.Global.Debug("Signing...");
        Signer?.Add(request);
        return Send(request);
    }

    /// <summary>
    /// Sends the request and waits <see cref="IReadOnlyClientProfile.Timeout"/> milliseconds for a response.
    /// The call does not throw, but always returns an instance of <see cref="SendTuple"/>. If the implementation of
    /// <see cref="ClientApi.SubmitPostRequest"/> does not call <see cref="SignAndSend"/>, it should call
    /// this method directly.
    /// </summary>
    protected Tuple<HttpStatusCode, string> Send(HttpRequestMessage request)
    {
        Logger.Global.Debug("Sending...");

        try
        {
            string body = "";
            var resp = _client.Send(request, HttpCompletionOption.ResponseContentRead);

            if (resp.Content != null)
            {
                Logger.Global.Debug("Reading response");
                body = new StreamReader(resp.Content.ReadAsStream(), Encoding.UTF8, false).ReadToEnd();
            }

            return Tuple.Create(resp.StatusCode, body);
        }
        catch (HttpRequestException e)
        {
            Logger.Global.Debug(e);
            var code = e.StatusCode ?? HttpStatusCode.InternalServerError;
            var resp = new ResponseMessage();
            resp.ErrorReason = e.InnerException?.Message ?? e.Message;
            return Tuple.Create(code, resp.ToString());
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            Logger.Global.Debug(e);
            var resp = new ResponseMessage();
            resp.ErrorReason = e.InnerException?.Message ?? e.Message;
            return Tuple.Create(HttpStatusCode.RequestTimeout, resp.ToString());
        }
        catch (Exception e)
        {
            Logger.Global.Debug(e);
            var resp = new ResponseMessage();
            resp.ErrorReason = e.InnerException?.Message ?? e.Message;
            return Tuple.Create(HttpStatusCode.InternalServerError, resp.ToString());
        }
    }

    /// <summary>
    /// Overriding method must call base method.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        _client.Dispose();
    }
}