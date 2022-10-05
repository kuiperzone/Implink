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

namespace KuiperZone.Implink.Routines.Api;
/// <summary>
/// Abstract base class which extends <see cref="ClientSession"/> to implement an internal
/// <see cref="HttpClient"/> instance. It does not, however, implement the API conversion.
/// </summary>
public abstract class HttpClientSession : ClientSession
{
    private readonly HttpClient _client = HttpClientFactory.Create();
    private int _sendCounter;
    private int _disposeCounter;

    /// <summary>
    /// Constructor. If factory is null, the instance will have no signer.
    /// </summary>
    public HttpClientSession(IReadOnlyClientProfile profile, ISignerFactory? factory, string? contentType = null)
        : base(profile)
    {
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
            _client.DefaultRequestHeaders.Add("Content-Type", contentType);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
        }
    }

    /// <summary>
    /// Gets the instance of <see cref="IHttpSigner"/>. The value may be null if none supplied on construction.
    /// </summary>
    public readonly IHttpSigner? Signer;

    /// <summary>
    /// Overriding method must call base method.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Interlocked.Increment(ref _disposeCounter) == 1)
            {
                ThreadPool.QueueUserWorkItem(DisposeProc);
            }
        }
        else
        {
            _client.Dispose();
        }
    }

    /// <summary>
    /// Calls <see cref="IHttpSigner.Add"/> where <see cref="Authentication"/> is not null, and returns
    /// the result of <see cref="Send"/>. This method is provided for convenience and is expected to be
    /// called by the implementation of <see cref="ClientSession.SubmitPostRequest(SubmitPost)"/> with an
    /// instance of HttpRequestMessage. It does not throw.
    /// </summary>
    protected Tuple<int, string, string> SignAndSend(HttpRequestMessage request)
    {
        Signer?.Add(request);
        return Send(request);
    }

    /// <summary>
    /// Sends the request and waits <see cref="IReadOnlyClientRoute.Timeout"/> milliseconds for a response.
    /// The call does not throw, but returns a Tuple, where Item1 = StatusCode, Item2 = error string,
    /// Item3 = response body text. If the implementation of <see cref="ClientSession.SubmitPostRequest(SubmitPost)"/>
    /// does not call <see cref="SignAndSend"/>, it should call this method directly.
    /// </summary>
    protected Tuple<int, string, string> Send(HttpRequestMessage request)
    {
        try
        {
            string body = "";
            Interlocked.Increment(ref _sendCounter);
            var resp = _client.Send(request, HttpCompletionOption.ResponseContentRead);

            if (resp.Content != null)
            {
                body = new StreamReader(resp.Content.ReadAsStream(), Encoding.UTF8, false).ReadToEnd();
            }

            return Tuple.Create((int)resp.StatusCode, resp.StatusCode.ToString(), body);
        }
        catch (HttpRequestException e)
        {
            var code = e.StatusCode ?? HttpStatusCode.InternalServerError;
            return Tuple.Create((int)code, code.ToString(), "");
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            return Tuple.Create((int)HttpStatusCode.RequestTimeout, HttpStatusCode.RequestTimeout.ToString(), "");
        }
        catch (Exception e)
        {
            var msg = e.InnerException?.Message ?? e.Message;
            return Tuple.Create((int)HttpStatusCode.InternalServerError, msg, "");
        }
        finally
        {
            Interlocked.Decrement(ref _sendCounter);
        }
    }

    private void DisposeProc(object? _)
    {
        // Wait until finished sending or timeout
        SpinWait.SpinUntil( () => { return Interlocked.Or(ref _sendCounter, 0) == 0; }, Profile.Timeout);
        _client.Dispose();
    }
}