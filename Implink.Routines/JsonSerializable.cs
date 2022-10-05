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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KuiperZone.Implink.Routines;

/// <summary>
/// A base class for all JSON serializable data for convenience and to enforce consistency.
/// </summary>
public class JsonSerializable
{
    /// <summary>
    /// Common JsonSerializerOptions value.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false, PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Deserialize using <see cref="JsonSerializable"/>.
    /// </summary>
    public static T Deserialize<T>(string? s) where T : JsonSerializable, new()
    {
        return JsonSerializer.Deserialize<T>(s ?? "", JsonOpts) ?? new T();
    }

    /// <summary>
    /// Overrides to output serializaed JSON string.
    /// </summary>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GetType(), JsonOpts);
    }

    /// <summary>
    /// Outputs JSON with indented option.
    /// </summary>
    public string ToString(bool indented)
    {
        var opts = new JsonSerializerOptions();
        opts.DefaultIgnoreCondition = JsonOpts.DefaultIgnoreCondition;
        opts.WriteIndented = indented;
        return JsonSerializer.Serialize(this, GetType(), opts);
    }
}
