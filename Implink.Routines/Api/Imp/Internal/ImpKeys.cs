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

using System.Security.Cryptography;
using System.Text;

namespace KuiperZone.Implink.Routines.Api.Imp;

/// <summary>
/// Class used in construction of IMP client and servers.
/// </summary>
public class ImpKeys
{
    private readonly string _public;
    private readonly byte[] _private;

    /// <summary>
    /// Default constructor. Authentication is disabled.
    /// </summary>
    public ImpKeys()
    {
        _public = "";
        _private = Array.Empty<byte>();
    }

    /// <summary>
    /// Constructor with PUBLIC and PRIVATE values. Provided empty or null for both disables authentication.
    /// </summary>
    /// <exception cref="ArgumentException">Both PUBLIC and PRIVATE keys must be provided</exception>
    public ImpKeys(string? pub, string? priv)
    {
        if (string.IsNullOrEmpty(pub) != string.IsNullOrEmpty(priv))
        {
            throw new ArgumentException("Both PUBLIC and PRIVATE keys must be provided");
        }

        _public = pub ?? "";
        _private = Encoding.UTF8.GetBytes(priv ?? "");
    }

    /// <summary>
    /// Constructor with <see cref="ClientSession"/> instace.
    /// </summary>
    /// <exception cref="ArgumentException">Both PUBLIC and PRIVATE keys must be provided</exception>
    public ImpKeys(ClientSession client)
        : this(GetAuth(client, "PUBLIC"), GetAuth(client, "PRIVATE"))
    {
    }

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
    /// <exception cref="InvalidOperationException">Authentication failed</exception>
    public void Assert(string? sign, string? timestamp, string? nonce, string? method, string? uri, string? body)
    {
        if (IsAuthenticationEnabled)
        {
            var comp = GetSignature(timestamp, nonce, method, uri, body);

            if (comp != sign)
            {
                throw new InvalidOperationException("Authentication failed");
            }
        }
    }

    /// <summary>
    /// Calculatues signature string.
    /// </summary>
    public string GetSignature(string? timestamp, string? nonce, string? method, string? uri, string? body)
    {
        if (IsAuthenticationEnabled)
        {
            // HMAC-SHA256(timestamp + nonce + PUBLIC + METHOD + /url + body, PRIVATE)
            var prehash = new StringBuilder(1024);
            prehash.Append(timestamp);
            prehash.Append(nonce);
            prehash.Append(_public);
            prehash.Append(method?.ToUpperInvariant());
            prehash.Append('/' + uri?.TrimStart('/'));
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