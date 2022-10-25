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

namespace KuiperZone.Implink.Api.Imp;

/// <summary>
/// Authenticates an incoming HTTP request and reads the body content. The instance is thread-safe.
/// </summary>
public class ImpServerDecoder
{
    private readonly ImpKeys _keys;

    /// <summary>
    /// Default constructor. Authentication is disabled (use only for internal LAN).
    /// </summary>
    public ImpServerDecoder()
    {
        _keys = new();
    }

    /// <summary>
    /// Constructor with key instance.
    /// </summary>
    public ImpServerDecoder(ImpKeys keys)
    {
        _keys = keys;
    }

    /// <summary>
    /// Gets the timestamp header key name.
    /// </summary>
    public readonly static string TIMESTAMP_KEY = "IMP_TIMESTAMP";

    /// <summary>
    /// Gets the nonce header key name.
    /// </summary>
    public readonly static string NONCE_KEY = "IMP_NONCE";

    /// <summary>
    /// Gets the nonce header key name.
    /// </summary>
    public readonly static string PUBLIC_KEY = "IMP_PUBLIC";

    /// <summary>
    /// Gets the sign header key name.
    /// </summary>
    public readonly static string SIGN_KEY = "IMP_SIGN";

    /// <summary>
    /// Gets whether a private key string was provided.
    /// </summary>
    public bool IsAuthenticationEnabled
    {
        get { return _keys.IsAuthenticationEnabled; }
    }

    /// <summary>
    /// Asserts where the request data authenticates agains the private key provided on
    /// constructor. The routine does nothing if <see cref="IsAuthenticationEnabled"/> is false.
    /// On success, the result is the response body text.
    /// </summary>
    /// <exception cref="ImpException">Authentication failed</exception>
    public string ReadAuthenticated(HttpRequest request)
    {
        var body = new StreamReader(request.Body, Encoding.UTF8, false).ReadToEnd();

        if (IsAuthenticationEnabled)
        {
            // Do this before HMAC
            if (_keys.Public != request.Headers[PUBLIC_KEY])
            {
                throw new ImpException("Authentication failed", HttpStatusCode.Unauthorized);
            }

            string timestamp = GetHeader(request.Headers, TIMESTAMP_KEY);
            string nonce = GetHeader(request.Headers, NONCE_KEY);
            string sign = GetHeader(request.Headers, SIGN_KEY);
            _keys.Assert(sign, timestamp, nonce, body);
        }

        return body;
    }

    /// <summary>
    /// Overload with instance of <see cref="HttpRequestMessage"/>.
    /// On success, the result is the response body text.
    /// </summary>
    /// <exception cref="ImpException">Authentication failed</exception>
    public string ReadAuthenticated(HttpRequestMessage request)
    {
        string body = "";

        if (request.Content != null)
        {
            body = new StreamReader(request.Content.ReadAsStream(), Encoding.UTF8, false).ReadToEnd();
        }

        if (IsAuthenticationEnabled)
        {
            string timestamp = GetHeader(request.Headers, TIMESTAMP_KEY);
            string nonce = GetHeader(request.Headers, NONCE_KEY);
            string sign = GetHeader(request.Headers, SIGN_KEY);
            _keys.Assert(sign, timestamp, nonce, body);
        }

        return body;
    }

    private static string GetHeader(IHeaderDictionary headers, string key)
    {
        var rslt = headers[key].ToString();

        if (rslt.Length != 0)
        {
            return rslt;
        }

        throw new ImpException($"Header {key} undefined");
    }

    private static string GetHeader(HttpRequestHeaders headers, string key)
    {
        if (headers.TryGetValues(key, out IEnumerable<string>? values))
        {
            var rslt = values.FirstOrDefault("");

            if (rslt.Length != 0)
            {
                return rslt;
            }
        }

        throw new ImpException($"Header {key} undefined");
    }
}
