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

using KuiperZone.Implink.Routines.Util;

namespace KuiperZone.Implink.Routines.Api;

/// <summary>
/// Abstract base class for a client session with an external vendor. The concrete
/// subclass is to implement API conversion and necessary calls over the wire.
/// </summary>
public abstract class ClientSession : IClientApi, IDisposable
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid profile</exception>
    public ClientSession(IReadOnlyRouteProfile profile, bool remoteTerminated)
    {
        profile.AssertValidity();
        IsRemoteTerminated = remoteTerminated;
        Profile = profile;
        AuthDictionary = DictionaryParser.ToDictionary(profile.Authentication);
        Categories = DictionaryParser.ToSet(profile.Categories);
    }

    /// <summary>
    /// Implements <see cref="IClientApi.IsRemoteTerminated"/>.
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    /// Gets the profile.
    /// </summary>
    public IReadOnlyRouteProfile Profile { get; }

    /// <summary>
    /// Gets the authentication dictionary. The dictionary is empty if no authentication is specified.
    /// </summary>
    public IReadOnlyDictionary<string, string> AuthDictionary { get; }

    /// <summary>
    /// Gets the categories set.
    /// </summary>
    public IReadOnlySet<string> Categories { get; }

    /// <summary>
    /// Implements <see cref="IClientApi.SubmitPostRequest"/> as abstract.
    /// </summary>
    public abstract int SubmitPostRequest(SubmitPost submit, out SubmitResponse response);

    /// <summary>
    /// Implements the disposal pattern.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>
    /// Base implementation does nothing.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }

}