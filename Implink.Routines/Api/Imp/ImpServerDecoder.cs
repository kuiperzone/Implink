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
using System.Text;
using Microsoft.AspNetCore.Http;

namespace KuiperZone.Implink.Routines.Api.Imp;

/// <summary>
/// Authenticates an incoming HTTP request and reads the body content. The instance is thread-safe.
/// </summary>
public class ImpServerDecoder
{
    private readonly ImpKeys _keys;

    /// <summary>
    /// Default constructor. Authentication is disables (use only for internal LAN).
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
    /// constructor. On failure, InvalidOperationException is thrown. The routine does nothing
    /// if <see cref="IsAuthenticationEnabled"/> is false.
    /// </summary>
    /// <exception cref="InvalidOperationException">Authentication failed</exception>
    public string Assert(HttpRequest request)
    {
        var body = new StreamReader(request.Body, Encoding.UTF8, false).ReadToEnd();

        if (IsAuthenticationEnabled)
        {
            var uri = request.Path.ToString();
            string timestamp = request.Headers[TIMESTAMP_KEY];
            string nonce = request.Headers[NONCE_KEY];
            string sign = request.Headers[SIGN_KEY];
            _keys.Assert(sign, timestamp, nonce, request.Method, uri, body);
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
            _keys.Assert(sign, timestamp, nonce, request.Method.ToString(), uri, body);
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