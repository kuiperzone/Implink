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

namespace KuiperZone.Implink.Api;

/// <summary>
/// The POST submit message body.
/// </summary>
public class SubmitPost : RequestMessage
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public SubmitPost()
    {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    public SubmitPost(SubmitPost other)
    {
        GroupId = other.GroupId;
        GatewayId = other.GatewayId;
        UserName = other.UserName;
        Category = other.Category;
        MsgId = other.MsgId;
        ParentMsgId = other.ParentMsgId;
        Text = other.Text;
        LinkUrl = other.LinkUrl;
    }

    /// <summary>
    /// Gets or sets the mandatory group ID. A message must belong to a group (channel or topic).
    /// </summary>
    public string GroupId { get; set; } = "";

    /// <summary>
    /// Gets or sets the gateway name. It is mandatory for traffic other than that on the internal LAN.
    /// </summary>
    public string GatewayId { get; set; } = "";

    /// <summary>
    /// Get or sets the mandatory name of the human use who originally posted the message.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// Gets or sets optional category name.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the message ID. If specified, it should be a unique string possibly composed of the
    /// user name ID and random or high precision time value. If omitted, the receiver will generate
    /// one itself.
    /// </summary>
    public string? MsgId { get; set; }

    /// <summary>
    /// Gets or sets the parent message ID. If empty, this is a top-level post. If specified,
    /// this is a reply to an existing message, which must exist on the destination.
    /// </summary>
    public string? ParentMsgId { get; set; }

    /// <summary>
    /// Gets or sets the mandatory message text.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets a link URL contained in the message.
    /// </summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Implements <see cref="Jsonizable.CheckValidity(out string)"/>.
    /// </summary>
    public override bool CheckValidity(out string message)
    {
        const int MaxLength = 64;

        if (string.IsNullOrWhiteSpace(GroupId) || GroupId.Length > MaxLength)
        {
            message = $"Invalid {nameof(GroupId)}";
            return false;
        }

        if (GatewayId != null && GatewayId.Length > MaxLength)
        {
            // Non-empty name NOT enforced
            message = $"Invalid {nameof(GatewayId)}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(UserName) || UserName.Length > 64)
        {
            message = $"Invalid {nameof(UserName)}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            message = $"Invalid {nameof(Text)}";
            return false;
        }

        message = "";
        return true;
    }

}
