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
using KuiperZone.Implink.Api.Util;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Api;

/// <summary>
/// Abstract base class which extends <see cref="ClientApi"/> to implement an internal
/// <see cref="HttpClient"/> instance. It does not, however, implement the API conversion.
/// </summary>
public abstract class HttpMessagingClient : IMessagingClient, IDisposable
{
    private readonly object _synLock = new();
    private readonly string? _contentType;
    private volatile HttpClient? v_client;

    /// <summary>
    /// Constructor. If factory is null, the instance will not be able to sign messages.
    /// </summary>
    public HttpMessagingClient(IReadOnlyClientProfile profile, ISignerFactory? factory, string? contentType = null)
    {
        profile.AssertValidity();

        Profile = profile;
        Signer = factory?.Create(this);
        _contentType = contentType;
    }

    /// <summary>
    /// Implements <see cref="IMessagingClient.Profile"/>.
    /// </summary>
    public IReadOnlyClientProfile Profile { get; }

    /// <summary>
    /// Gets the instance of <see cref="IHttpSigner"/>. The value may be null if no factory was
    /// supplied on construction.
    /// </summary>
    public readonly IHttpSigner? Signer;

    /// <summary>
    /// Implements <see cref="IMessagingApi.PostMessage(ImpMessage)"/>.
    /// </summary>
    public ImpResponse PostMessage(ImpMessage request)
    {
        Logger.Global.Debug("Sending: " + request.ToString());
        var msg = TranslateRequest(request);

        var tuple = SignAndSend(msg);
        Logger.Global.Debug($"Tuple {tuple}");

        Logger.Global.Debug($"Translating response");
        return TranslateResponse(tuple.Item1, tuple.Item2);
    }

    /// <summary>
    /// Implements the disposal pattern.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>
    /// Disposes of client.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        v_client?.Dispose();
    }

    /// <summary>
    /// Must be implemented to translate <see cref="ImpMessage"/> to HttpRequestMessage.
    /// </summary>
    protected abstract HttpRequestMessage TranslateRequest(ImpMessage request);

    /// <summary>
    /// Must be implemented to translate the incoming response body to an instance of <see cref="ImpResponse"/>.
    /// If status is not 200 OK, the body string will contain an "error reason" rather than the body (if any).
    /// </summary>
    protected abstract ImpResponse TranslateResponse(HttpStatusCode status, string body);

    private Tuple<HttpStatusCode, string> SignAndSend(HttpRequestMessage request)
    {
        // Calls <see cref="IHttpSigner.Add"/> where <see cref="Signer"/> is not null, and returns the result of
        // <see cref="Send"/>. This method is provided for convenience and is called by <see cref="PostMessage"/>.
        // It does not throw.
        Logger.Global.Debug("Signing...");
        Signer?.AddHeaders(request);
        return Send(request);
    }

    private Tuple<HttpStatusCode, string> Send(HttpRequestMessage request)
    {
        // Sends the request and waits <see cref="IReadOnlyClientProfile.Timeout"/> milliseconds for a response.
        // The call does not throw, but always returns a tuple. On 200 success, the tuple string contains the response
        // body text (if any). On failure, the string contain an error string.
        Logger.Global.Debug("Sending...");

        try
        {
            string body = "";
            var resp = GetClientOnDemaned().Send(request, HttpCompletionOption.ResponseContentRead);

            Logger.Global.Debug("Reading response");

            if (resp.Content != null)
            {
                body = new StreamReader(resp.Content.ReadAsStream(), Encoding.UTF8, false).ReadToEnd();
            }

            return Tuple.Create(resp.StatusCode, body);
        }
        catch (HttpRequestException e)
        {
            Logger.Global.Debug(e);
            var code = e.StatusCode ?? HttpStatusCode.InternalServerError;
            var reason = e.InnerException?.Message ?? e.Message;
            reason ??= code.ToString();
            return Tuple.Create(code, reason);
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            Logger.Global.Debug(e);
            var reason = e.InnerException?.Message ?? e.Message;
            reason ??= HttpStatusCode.RequestTimeout.ToString();
            return Tuple.Create(HttpStatusCode.RequestTimeout, reason);
        }
        catch (Exception e)
        {
            Logger.Global.Debug(e);
            var reason = e.InnerException?.Message ?? e.Message;
            reason ??= HttpStatusCode.InternalServerError.ToString();
            return Tuple.Create(HttpStatusCode.InternalServerError, reason);
        }
    }

    private HttpClient GetClientOnDemaned()
    {
        var client = v_client;

        if (client != null)
        {
            return client;
        }

        lock (_synLock)
        {
            client = v_client;

            if (client != null)
            {
                return client;
            }

            Logger.Global.Debug("Create underlying client");

            if (Profile.DisableSslValidation)
            {
                Logger.Global.Debug("SSL VALIDATION DISABLED");
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; };
                client = HttpClientFactory.Create(handler);
            }
            else
            {
                Logger.Global.Debug("SSL validation");
                client = HttpClientFactory.Create();
            }

            // https://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working
            // You must place a slash at the end of the BaseAddress, and you must not place a slash at the beginning of your relative URI.
            client.BaseAddress = new Uri(new Uri(Profile.BaseAddress).AbsoluteUri.Trim('/') + '/');
            client.Timeout = TimeSpan.FromMilliseconds(Profile.Timeout);


            if (!string.IsNullOrEmpty(Profile.UserAgent))
            {
                client.DefaultRequestHeaders.Add("User-Agent", Profile.UserAgent);
            }

            if (!string.IsNullOrEmpty(_contentType))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_contentType));
            }

            v_client = client;
        }

        return client;
    }
}