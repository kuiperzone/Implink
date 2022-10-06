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
using System.Security.Cryptography;
using System.Text;

namespace KuiperZone.Implink.Routines.Api.Imp;

/// <summary>
/// Class used in construction of IMP client and servers.
/// </summary>
public class ImpKeys
{
    private readonly byte[] _private;

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
    /// Constructor with <see cref="ClientSession"/> instace.
    /// </summary>
    /// <exception cref="ArgumentException">Both PUBLIC and PRIVATE keys must be provided</exception>
    public ImpKeys(ClientSession client, int deltaSec = 30)
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
    /// Asserts where the request data authenticates agains the private key provided on
    /// constructor. The routine does nothing if <see cref="IsAuthenticationEnabled"/> is false.
    /// </summary>
    /// <exception cref="ImpException">Authentication failed</exception>
    public void Assert(string? sign, string? timestamp, string? nonce, string? body)
    {
        if (IsAuthenticationEnabled)
        {
            if (!long.TryParse(timestamp, out long uxsec))
            {
                throw new ImpException("Invalid timestamp", 401);
            }

            if (AllowedDeltaSeconds > 0)
            {
                long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

                if (now - uxsec > AllowedDeltaSeconds || uxsec - now > AllowedDeltaSeconds)
                {
                    throw new ImpException("Timestamp difference too large", 401);
                }
            }

            var comp = GetSignature(timestamp, nonce, body);

            if (comp != sign)
            {
                throw new ImpException("Authentication failed", 401);
            }
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

    private static string GetAuth(ClientSession c, string key)
    {
        return c.AuthDictionary.GetValueOrDefault(key, "");
    }
}