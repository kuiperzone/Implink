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

namespace KuiperZone.Implink.Routines.Api;

/// <summary>
/// Implements <see cref="IHttpSigner"/> for the native IMP API.
/// </summary>
public class ImpSigner : IHttpSigner
{
    private readonly byte[] _private;

    public ImpSigner(string? priv)
    {
        _private = Encoding.UTF8.GetBytes(priv ?? "");
    }

    public ImpSigner(ClientSession client)
    {
        _private = Encoding.UTF8.GetBytes(client.AuthDictionary.GetValueOrDefault("PRIVATE", ""));
    }

    /// <summary>
    /// Implements <see cref="IHttpSigner.Add(HttpRequestMessage request)"/>.
    /// </summary>
    public void Add(HttpRequestMessage request)
    {
        // Need to prefix separator

        string uri = '/' + request.RequestUri?.AbsoluteUri.TrimStart('/');
        // CallLogger.Debug("uri: {0}", uri);

        string timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        // CallLogger.Debug("Timestamp: {0}", timestamp);

        string nonce = GetNonce();
        // CallLogger.Debug("Nonce: {0}", nonce);

        // Future proof - a version ID
        request.Headers.Add("IMP_AUTHSCHEME", "0");

        request.Headers.Add(ImpDecoder.TIMESTAMP_KEY, timestamp);
        request.Headers.Add(ImpDecoder.NONCE_KEY, nonce);

        // HMAC-SHA256(timestamp + nonce + METHOD + /url + body, PRIVATE)
        var prehash = new StringBuilder(1024);
        prehash.Append(timestamp);
        prehash.Append(nonce);
        prehash.Append(request.Method.ToString().ToUpperInvariant());
        prehash.Append(uri);
        prehash.Append(request.Content);
        // CallLogger.Debug("Prehash: {0}", prehash.ToString());

        using (var hmac = new HMACSHA256(_private))
        {
            var sign = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash.ToString())));
            // CallLogger.Debug("Sign: {0}", sign);

            request.Headers.Add(ImpDecoder.SIGN_KEY, sign);
        }
    }

    private static string GetNonce(int count = 16)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
    }
}
