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
public static class DictionaryParser
{
    /// <summary>
    /// Parses string of form "key1=value1,key2=value2" and returns a dictionary instance.
    /// An empty or null string results in an empty dictionary.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid dictionary key-value pair</exception>
    /// <exception cref="ArgumentException">Key alread exists</exception>
    public static Dictionary<string, string> ToDictionary(string? s)
    {
        var rslt = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        if (!string.IsNullOrWhiteSpace(s))
        {
            var split = s.Split(',', StringSplitOptions.RemoveEmptyEntries);

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

    public static HashSet<string> ToSet(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            // Use sorted - number of items expected to 1 or several only.
            return new HashSet<string>(s.Split(',', StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase);
        }

        return new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
    }

}