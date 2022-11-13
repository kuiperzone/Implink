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
using KuiperZone.Implink.Api;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// A container class for an instance of <see cref="ClientApi"/>, which also provides rate limiting.
/// </summary>
public class ClientContainer : IClientApi, IDisposable
{
    /// <summary>
    /// Constructor. This instance will own the client and dispose of it.
    /// </summary>
    public ClientContainer(ClientApi client)
    {
        IsRemoteTerminated = client.Profile.Endpoint == EndpointKind.Remote;

        Client = client;
        Counter = new(client.Profile.ThrottleRate);

        if (!IsRemoteTerminated)
        {
            AuthenticationSecret = new ImpSecret(client);
        }
    }

    /// <summary>
    /// Constructor in which the client is created according to <see cref="IReadOnlyClientProfile.Api"/>.
    /// Where remoteTerminated is false, the API kind is ignored as the client is always an instance of
    /// <see cref="ImpHttpClient"/>.
    /// </summary>
    public ClientContainer(IReadOnlyClientProfile profile)
        : this(CreateClient(profile))
    {
    }

    /// <summary>
    /// Gets whether remote terminated.
    /// </summary>
    public bool IsRemoteTerminated { get; }

    /// <summary>
    /// Gets a rate counter.
    /// </summary>
    public RateCounter Counter { get; }

    /// <summary>
    /// Gets the client.
    /// </summary>
    public ClientApi Client { get; }

    /// <summary>
    /// Gets IMP authentication.
    /// </summary>
    public ImpSecret? AuthenticationSecret { get; }

    /// <summary>
    /// Calls <see cref="IClientApi.SubmitPostRequest"/>, implements throttling and truncates message if too long.
    /// </summary>
    public int SubmitPostRequest(SubmitPost submit, out SubmitResponse response)
    {
        var prof = Client.Profile;

        if (Counter.IsThrottled())
        {
            response = new();
            response.ErrorReason = "Request throttled";
            return (int)HttpStatusCode.TooManyRequests;
        }

        if (prof.MaxText > 3 && submit.Text.Length > prof.MaxText - 3)
        {
            // Make a clone
            submit = new SubmitPost(submit);
            submit.Text = submit.Text.Substring(0, prof.MaxText - 3) + "...";
        }

        return Client.SubmitPostRequest(submit, out response);
    }

    /// <summary>
    /// Implements disposal.
    /// </summary>
    public void Dispose()
    {
        Client.Dispose();
    }

    private static ClientApi CreateClient(IReadOnlyClientProfile profile)
    {
        if (profile.Endpoint == EndpointKind.Remote)
        {
            return ClientFactory.Create(profile);
        }

        // Populate base address
        var temp = new ClientProfile(profile);

        return new ImpHttpClient(temp);
    }
}