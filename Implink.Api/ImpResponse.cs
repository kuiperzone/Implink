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

namespace KuiperZone.Implink.Api;

/// <summary>
/// An IMP response class which is intended to serve most request types.
/// </summary>
public class ImpResponse : Jsonizable
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public ImpResponse()
    {
    }

    /// <summary>
    /// Constructor. The <see cref="Status"/> is assumed to be 200 OK.
    /// </summary>
    public ImpResponse(string? content)
    {
        Content = content;
    }

    /// <summary>
    /// Constructor with status code.
    /// </summary>
    public ImpResponse(HttpStatusCode status, string? error = null)
    {
        Status = status;
        ErrorInfo = error;
    }

    /// <summary>
    /// Error constructor. The <see cref="Status"/> will be InternalServerError 500, while
    /// <see cref="Content"/> will be the exception message string.
    /// </summary>
    public ImpResponse(Exception e)
    {
        Status = HttpStatusCode.InternalServerError;
        Content = e.Message;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    /// <summary>
    /// Gets or sets error or warning info or reason -- additional information for diagnostic purposes.
    /// </summary>
    public string? ErrorInfo { get; set; }

    /// <summary>
    /// Gets or sets a response message. It's interpretation may depend on the request type. For
    /// <see cref="IMessagingApi.PostMessage(ImpMessage)"/>, it provides the message id on success.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Short-hand to check for success.
    /// </summary>
    public bool IsSuccess()
    {
        var i = (int)Status;
        return i >= 200 && i < 300;
    }

}

