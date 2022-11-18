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

namespace KuiperZone.Implink.Api.Util;

/// <summary>
/// Static utility.
/// </summary>
public static class StringParser
{
    /// <summary>
    /// Parses string of form "key1=value1,key2=value2" and returns a dictionary instance.
    /// An empty or null string results in an empty dictionary. The resulting dictionary is case insensitive.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid dictionary key-value pair</exception>
    /// <exception cref="ArgumentException">Key already exists</exception>
    public static Dictionary<string, string> ToDictionary(string? s)
    {
        var rslt = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (!string.IsNullOrWhiteSpace(s))
        {
            var split = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var pair in split)
            {
                var pos = pair.IndexOf('=');

                if (pos > 0 && pos < pair.Length - 1)
                {
                    var k = pair.Substring(0, pos).Trim();
                    var v = pair.Substring(pos + 1).Trim();

                    if (k.Length != 0 && v.Length != 0)
                    {
                        rslt.Add(k, v);
                        continue;
                    }
                }

                throw new ArgumentException($"Invalid dictionary key-value pair: {pair}");
            }
        }

        return rslt;
    }

    /// <summary>
    /// Parses comma separated string and returns hash set. An empty or null string results in an empty set.
    /// The resulting set is case insensitive.
    /// </summary>
    public static HashSet<string> ToSet(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            var split = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new HashSet<string>(split, StringComparer.InvariantCultureIgnoreCase);
        }

        return new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    }

}