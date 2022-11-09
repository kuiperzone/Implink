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
        GroupName = other.GroupName;
        UserName = other.UserName;
        Category = other.Category;
        MsgId = other.MsgId;
        ParentId = other.ParentId;
        Text = other.Text;
        LinkUrl = other.LinkUrl;
    }

    /// <summary>
    /// Gets or sets the mandatory group name.
    /// </summary>
    public string GroupName { get; set; } = "";

    /// <summary>
    /// Get or sets the user name.
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
    public string? ParentId { get; set; }

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
        if (string.IsNullOrWhiteSpace(GroupName) || GroupName.Length > 64)
        {
            message = $"{nameof(GroupName)} undefined or invalid";
            return false;
        }

        if (string.IsNullOrWhiteSpace(UserName) || UserName.Length > 64)
        {
            message = $"{nameof(UserName)} undefined or invalid";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            message = $"{nameof(Text)} undefined";
            return false;
        }

        message = "";
        return true;
    }

}
