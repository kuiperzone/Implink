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
/// Abstract base class which repesents a client session with an external vendor. The
/// concrete subclass is to implement API conversion and necessary calls over the wire.
/// </summary>
public abstract class ClientSession : IDisposable
{
    public ClientSession(IReadOnlyClientProfile profile)
    {
        const char AuthSep = ',';

        Profile = profile;

        var auth = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AuthDictionary = auth;

        if (!string.IsNullOrWhiteSpace(profile.Authentication))
        {
            var split = profile.Authentication.Split(AuthSep, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in split)
            {
                var pos = pair.IndexOf('=');

                if (pos > 0 && pos < pair.Length - 1)
                {
                    var k = pair.Substring(0, pos).Trim();
                    var v = pair.Substring(pos + 1).Trim();

                    if (k.Length != 0 && v.Length != 0)
                    {
                        auth.Add(k, v);
                        continue;
                    }
                }

                throw new ArgumentException($"Invalid {nameof(AuthDictionary)} key-value pair: {pair}");
            }
        }

    }

    /// <summary>
    /// Gets the profile supplied on construction.
    /// </summary>
    public readonly IReadOnlyClientProfile Profile;

    /// <summary>
    /// Gets the authentication dictionary. The dictionary is empty if no authentication is specified.
    /// </summary>
    public readonly IReadOnlyDictionary<string, string> AuthDictionary;

    /// <summary>
    /// Abstract method which translates the submit data into a vendor specific call and provides the response.
    /// The return value is the HTTP status code. This method may block for up to
    /// <see cref="IReadOnlyClientProfile.Timeout"/> before throw an exception. It may throw any exception
    /// on failure.
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
    /// Can be overridden.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
    }

}