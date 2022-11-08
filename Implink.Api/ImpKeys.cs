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
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using KuiperZone.Implink.Api.Util;
using Microsoft.Extensions.Primitives;

namespace KuiperZone.Implink.Api;

/// <summary>
/// Immutable class used in construction of IMP client and servers.
/// </summary>
public class ImpKeys
{
    private readonly byte[] _private;

    /// <summary>
    /// The timestamp header key name.
    /// </summary>
    public const string TIMESTAMP_KEY = "IMP_TIMESTAMP";

    /// <summary>
    /// The nonce header key name.
    /// </summary>
    public const string NONCE_KEY = "IMP_NONCE";

    /// <summary>
    /// The IMP public header key name.
    /// </summary>
    public const string PUBLIC_KEY = "IMP_PUBLIC";

    /// <summary>
    /// Gets the sign header key name.
    /// </summary>
    public const string SIGN_KEY = "IMP_SIGN";

    /// <summary>
    /// Default constructor. Authentication is disabled.
    /// </summary>
    public ImpKeys()
    {
        Public = "";
        _private = Array.Empty<byte>();
    }

    /// <summary>
    /// Constructor with PUBLIC and PRIVATE values. Provided empty or null for both disables authentication.
    /// </summary>
    /// <exception cref="ArgumentException">Both PUBLIC and PRIVATE keys must be provided</exception>
    public ImpKeys(string? pub, string? priv, int deltaSec = 30)
    {
        if (string.IsNullOrEmpty(pub) != string.IsNullOrEmpty(priv))
        {
            throw new ArgumentException("Both PUBLIC and PRIVATE keys must be provided");
        }

        Public = pub ?? "";
        _private = Encoding.UTF8.GetBytes(priv ?? "");
        AllowedDeltaSeconds = deltaSec;
    }

    /// <summary>
    /// Constructor with <see cref="IReadOnlyClientProfile"/> instance.
    /// </summary>
    /// <exception cref="ArgumentException">Both PUBLIC and PRIVATE keys must be provided</exception>
    public ImpKeys(IReadOnlyClientProfile profile, int deltaSec = 30)
        : this(GetAuth(profile, out string priv), priv, deltaSec)
    {
    }

    /// <summary>
    /// Constructor with <see cref="ClientApi"/> instace.
    /// </summary>
    /// <exception cref="ArgumentException">Both PUBLIC and PRIVATE keys must be provided</exception>
    public ImpKeys(ClientApi client, int deltaSec = 30)
        : this(GetAuth(client, "PUBLIC"), GetAuth(client, "PRIVATE"), deltaSec)
    {
    }

    /// <summary>
    /// Gets the public key.
    /// </summary>
    public readonly string Public;

    /// <summary>
    /// Gets the maximum time delta a timetamp is allowed to differ from system time.
    /// A negative or zero value disables.
    /// </summary>
    public readonly int AllowedDeltaSeconds;

    /// <summary>
    /// Gets whether a private key string was provided.
    /// </summary>
    public bool IsAuthenticationEnabled
    {
        get { return _private.Length != 0; }
    }

    /// <summary>
    /// Checks that the request data authenticates against the private key. On success, the result
    /// is null. On failure, the result is an error message. The routine always returns null if
    /// <see cref="IsAuthenticationEnabled"/> is false.
    /// </summary>
    public string? Verify(string? sign, string? timestamp, string? nonce, string? body)
    {
        if (IsAuthenticationEnabled)
        {
            if (!long.TryParse(timestamp, out long uxsec))
            {
                return "Invalid timestamp";
            }

            if (AllowedDeltaSeconds > 0)
            {
                long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

                if (now - uxsec > AllowedDeltaSeconds || uxsec - now > AllowedDeltaSeconds)
                {
                    return "Timestamp difference too large";
                }
            }

            var comp = GetSignature(timestamp, nonce, body);

            if (comp != sign)
            {
                return "Authentication failed";
            }
        }

        return null;
    }

    /// <summary>
    /// Overload with request header and body data.
    /// </summary>
    public string? Verify(IDictionary<string, StringValues> headers, string body)
    {
        if (IsAuthenticationEnabled)
        {
            // Do this before HMAC
            if (GetHeader(headers, PUBLIC_KEY) != Public)
            {
                return "Authentication failed";
            }

            string timestamp = GetHeader(headers, TIMESTAMP_KEY);
            string nonce = GetHeader(headers, NONCE_KEY);
            string sign = GetHeader(headers, SIGN_KEY);
            return Verify(sign, timestamp, nonce, body);
        }

        return null;
    }

    /// <summary>
    /// Throws if authentication fails.
    /// </summary>
    /// <exception cref="ImpException">Authentication failed</exception>
    public void Assert(IDictionary<string, StringValues> headers, string body)
    {
        var msg = Verify(headers, body);

        if (msg != null)
        {
            throw new ImpException(msg, (int)HttpStatusCode.Unauthorized);
        }
    }

    /// <summary>
    /// Calculatues signature string.
    /// </summary>
    public string GetSignature(string? nonce, string? body, out string timestamp)
    {
        timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return GetSignature(timestamp, nonce, body);
    }

    /// <summary>
    /// Calculatues signature string.
    /// </summary>
    public string GetSignature(string? timestamp, string? nonce, string? body)
    {
        if (IsAuthenticationEnabled)
        {
            // HMAC-SHA256(timestamp + nonce + PUBLIC + body, PRIVATE)
            var prehash = new StringBuilder(1024);
            prehash.Append(timestamp);
            prehash.Append(nonce);
            prehash.Append(Public);
            prehash.Append(body);

            using (var hmac = new HMACSHA256(_private))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash.ToString())));
            }
        }

        return "";
    }

    private static string GetAuth(IReadOnlyClientProfile profile, out string priv)
    {
        var p = DictionaryParser.ToDictionary(profile.Authentication);
        priv = p.GetValueOrDefault("PRIVATE", "");
        return p.GetValueOrDefault("PUBLIC", "");
    }

    private static string GetAuth(ClientApi c, string key)
    {
        return c.AuthDictionary.GetValueOrDefault(key, "");
    }

    private static string GetHeader(IDictionary<string, StringValues> headers, string key)
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