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
/// Implements <see cref="IHttpSigner"/> for the native IMP API.
/// </summary>
public class ImpClientSigner : IHttpSigner
{
    private readonly ImpKeys _keys;

    /// <summary>
    /// Constructor with key instance.
    /// </summary>
    public ImpClientSigner(ImpKeys keys)
    {
        _keys = keys;
    }

    /// <summary>
    /// Implements <see cref="IHttpSigner.Add(HttpRequestMessage request)"/>.
    /// </summary>
    public void Add(HttpRequestMessage request)
    {
        string nonce = GetNonce();
        // CallLogger.Debug("Nonce: {0}", nonce);

        // Future proof - a version ID
        request.Headers.Add("IMP_API", ClientFactory.ImpV1);

        string? body = null;

        if (request.Content != null)
        {
            body = new StreamReader(request.Content.ReadAsStream(), Encoding.UTF8, false).ReadToEnd();
        }

        var sign = _keys.GetSignature(nonce, body, out string timestamp);
        request.Headers.Add(ImpServerDecoder.PUBLIC_KEY, _keys.Public);
        request.Headers.Add(ImpServerDecoder.NONCE_KEY, nonce);
        request.Headers.Add(ImpServerDecoder.SIGN_KEY, sign);
        request.Headers.Add(ImpServerDecoder.TIMESTAMP_KEY, timestamp);
    }

    private static string GetNonce(int count = 16)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
    }
}
