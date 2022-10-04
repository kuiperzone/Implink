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

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace KuiperZone.Implink.Routines.Api;

/// <summary>
/// Authenticates an incoming HTTP request and reads the body content. The instance is thread-safe.
/// </summary>
public class ImpDecoder
{
    private readonly byte[] _private;

    /// <summary>
    /// Constructor with private key string. A value of empty or null disables authentication.
    /// </summary>
    public ImpDecoder(string? priv = null)
    {
        _private = Encoding.UTF8.GetBytes(priv ?? "");
    }

    /// <summary>
    /// Constructor with <see cref="ClientSession"/>.
    /// </summary>
    public ImpDecoder(ClientSession client)
    {
        _private = Encoding.UTF8.GetBytes(client.AuthDictionary.GetValueOrDefault("PRIVATE", ""));
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
    /// Gets the sign header key name.
    /// </summary>
    public readonly static string SIGN_KEY = "IMP_SIGN";

    /// <summary>
    /// Gets whether a private key string was provided.
    /// </summary>
    public bool IsAuthenticationEnabled
    {
        get { return _private.Length != 0; }
    }

    /// <summary>
    /// Asserts where the request data authenticates agains the private key provided on
    /// constructor. On failure, InvalidOperationException is thrown. The routine does nothing
    /// if <see cref="IsAuthenticationEnabled"/> is false.
    /// </summary>
    public void Assert(string? timestamp, string? nonce, string? sign, string? method, string? uri, string? body)
    {
        if (IsAuthenticationEnabled)
        {
            // HMAC-SHA256(timestamp + nonce + METHOD + /url + body, PRIVATE)
            uri = '/' + uri?.TrimStart('/');

            // CallLogger.Debug("Timestamp: {0}", timestamp);
            // CallLogger.Debug("Nonce: {0}", nonce);
            // CallLogger.Debug("Sign: {0}", sign);
            // CallLogger.Debug("Method: {0}", method);
            // CallLogger.Debug("Uri: {0}", uri);
            // CallLogger.Debug("Body: {0}", body);

            var prehash = new StringBuilder(1024);
            prehash.Append(timestamp);
            prehash.Append(nonce);
            prehash.Append(method?.ToUpperInvariant());
            prehash.Append(uri);
            prehash.Append(body);

            using (var hmac = new HMACSHA256(_private))
            {
                if (sign != Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash.ToString()))))
                {
                    throw new InvalidOperationException("Authentication failure");
                }
            }
        }
    }

    /// <summary>
    /// Overload with instance of <see cref="HttpRequest"/>. On success, the return value is the request
    /// body string decoded as UTF8.
    /// </summary>
    public string Assert(HttpRequest request)
    {
        var body = new StreamReader(request.Body, Encoding.UTF8, false).ReadToEnd();

        if (IsAuthenticationEnabled)
        {
            var uri = request.Path.ToString();
            string timestamp = request.Headers[TIMESTAMP_KEY];
            string nonce = request.Headers[NONCE_KEY];
            string sign = request.Headers[SIGN_KEY];
            Assert(timestamp, nonce, sign, request.Method, uri, body);
        }

        return body;
    }

    /// <summary>
    /// Overload with instance of <see cref="HttpRequestMessage"/>. On success, the return value is the request
    /// body string decoded as UTF8.
    /// </summary>
    public string Assert(HttpRequestMessage request)
    {
        string body = "";

        if (request.Content != null)
        {
            body = new StreamReader(request.Content.ReadAsStream(), Encoding.UTF8, false).ReadToEnd();
        }

        if (IsAuthenticationEnabled)
        {
            var uri = request.RequestUri?.ToString();
            string timestamp = GetHeader(request.Headers, TIMESTAMP_KEY);
            string nonce = GetHeader(request.Headers, NONCE_KEY);
            string sign = GetHeader(request.Headers, SIGN_KEY);
            Assert(timestamp, nonce, sign, request.Method.ToString(), uri, body);
        }

        return body;
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

        throw new InvalidOperationException($"HTTP {key} header value not provided");
    }

}
