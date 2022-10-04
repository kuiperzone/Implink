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

namespace KuiperZone.Implink.Routines.Api;

/// <summary>
/// Interface for routines which adds authentication signiture header values to a HTTP request.
/// Implementations are intended to provide such things as native signer and Oath2.
/// </summary>
public interface IHttpSigner
{
    /// <summary>
    /// Adds authentication related header keys and values. The request headers and other properties,
    /// such as URI and method, are expected to be pre-populated with values the routine may require. The
    /// implementation should be reentrent, but need not be lock-free. It may throw any exception on failure.
    /// </summary>
    void Add(HttpRequestMessage request);
}
