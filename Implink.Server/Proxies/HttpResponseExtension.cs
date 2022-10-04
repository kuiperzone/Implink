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
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace KuiperZone.Implink.Proxies;

/// <summary>
/// Provides routing information for forward proxy.
/// </summary>
public static class HttpResponseExtension
{
    /// <summary>
    /// Writes the body to the HttpResponse asynchronsly and assigns the status code. The ContentType is "application/json".
    /// </summary>
    public static Task WriteJsonAsync(this HttpResponse resp, HttpStatusCode code, string? body = null, CancellationToken cancel = default)
    {
        body ??= string.Empty;
        resp.StatusCode = (int)HttpStatusCode.OK;
        resp.ContentType = MediaTypeNames.Application.Json;
        resp.ContentLength = Encoding.UTF8.GetByteCount(body);
        return resp.WriteAsync(body, cancel);
    }

    /// <summary>
    /// Overload. The body is serialized to Json.
    /// </summary>
    public static Task WriteJsonAsync<T>(this HttpResponse resp, HttpStatusCode code, T? body, CancellationToken cancel = default)
        where T : class
    {
        return WriteJsonAsync(resp, code, JsonSerializer.Serialize(body), cancel);
    }

}