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
using KuiperZone.Implink.Routing;

namespace KuiperZone.Implink.Proxies;

/// <summary>
/// Provides routing information for forward proxy.
/// </summary>
public sealed class ForwardProxy : IDisposable
{
    private readonly ILogger? _logger;
    private volatile bool v_disposed;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ForwardProxy(WebApplication app)
    {
        _logger = app.Logger;
        Routing = new RoutingProvider(app.Configuration, _logger);

        // https://www.koderdojo.com/blog/asp-net-core-routing-and-routebuilder-mapget-for-calculating-a-factorial
        app.MapPost("/publish", PublishHandler);
    }

    /// <summary>
    /// Gets the routing provider.
    /// </summary>
    public readonly RoutingProvider Routing;

    /// <summary>
    /// Implements disposable.
    /// </summary>
    public void Dispose()
    {
        Routing.Dispose();
        v_disposed = true;
    }

    private Task PublishHandler(HttpContext ctx)
    {
        return ctx.Response.WriteJsonAsync(HttpStatusCode.OK, "Hello World");
    }

}