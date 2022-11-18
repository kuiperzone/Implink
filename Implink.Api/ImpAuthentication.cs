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
/// Immutable class used in construction of IMP client and servers. It holds the IMP specific "secret" information,
/// and verifying request information, and works with <see <see cref="ImpSigner"/> to perform request signing.
/// </summary>
public class ImpAuthentication
{
    private readonly byte[] _secret;

    /// <summary>
    /// The timestamp header key name.
    /// </summary>
    public const string TIMESTAMP_KEY = "IMP_TIMESTAMP";

    /// <summary>
    /// The nonce header key name.
    /// </summary>
    public const string NONCE_KEY = "IMP_NONCE";

    /// <summary>
    /// Gets the sign header key name.
    /// </summary>
    public const string SIGN_KEY = "IMP_SIGN";

    public ImpAuthentication()
    {
        _secret = Array.Empty<byte>();
    }

    /// <summary>
    /// Constructor with secret and delta seconds. Also serves as default constructor, where authication is disabled.
    /// </summary>
    public ImpAuthentication(string? secret, int deltaSec = 30)
    {
        _secret = Encoding.UTF8.GetBytes(secret ?? "");
        AllowedDeltaSeconds = deltaSec;
    }

    /// <summary>
    /// Constructor with <see cref="IReadOnlySecretProfile"/> instance.
    /// </summary>
    public ImpAuthentication(IReadOnlySecretProfile profile, int deltaSec = 30)
        : this(GetAuth(profile, "SECRET"), deltaSec)
    {
    }

    /// <summary>
    /// Gets the maximum time delta a timetamp is allowed to differ from system time.
    /// A negative or zero value disables.
    /// </summary>
    public int AllowedDeltaSeconds {get;} = 30;

    /// <summary>
    /// Gets whether a private key string was provided.
    /// </summary>
    public bool IsAuthenticationEnabled
    {
        get { return _secret.Length != 0; }
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

                if (Math.Abs(now - uxsec) > AllowedDeltaSeconds)
                {
                    return "Timestamp delta too large";
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
            var nonce = GetHeader(headers, NONCE_KEY);

            if (nonce == null)
            {
                return $"Header {NONCE_KEY} undefined";
            }

            var timestamp = GetHeader(headers, TIMESTAMP_KEY);

            if (timestamp == null)
            {
                return $"Header {TIMESTAMP_KEY} undefined";
            }

            var sign = GetHeader(headers, SIGN_KEY);

            if (sign == null)
            {
                return $"Header {SIGN_KEY} undefined";
            }

            return Verify(sign, timestamp, nonce, body);
        }

        return null;
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
            // HMAC-SHA256(timestamp + nonce + body)
            var prehash = new StringBuilder(1024);
            prehash.Append(timestamp);
            prehash.Append(nonce);
            prehash.Append(body);

            using (var hmac = new HMACSHA256(_secret))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash.ToString())));
            }
        }

        return "";
    }

    private static string GetAuth(IReadOnlySecretProfile s, string key)
    {
        return StringParser.ToDictionary(s.Secret).GetValueOrDefault(key, "");
    }

    private static string? GetHeader(IDictionary<string, StringValues> headers, string key)
    {
        var rslt = headers[key].ToString();
        return !string.IsNullOrEmpty(rslt) ? rslt : null;
    }

}