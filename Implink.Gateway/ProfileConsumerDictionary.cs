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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using KuiperZone.Utility.Yaal;

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// A specialised dictionary class which holds "TConsumer" class items, where instances of TConsumer are instantiated
/// using an instance of "TProfile". In effect, TProfile is the configuration for class instances of TConsumer
/// (a "consumer" of a profile). A real example of this a TProfile of <see cref="IReadOnlyNamedClientProfile"/>,
/// and TConsumer of <see cref="NameClientApi"/>. The purpose of dictionary is to faciliate the updating of TConsumer
/// items, while preserving their state data where possible. This class is abstract, and subclass must implement the
/// <see cref="CreateConsumer"/> method.
/// </summary>
public abstract class ProfileConsumerDictionary<TProfile, TConsumer> : IReadOnlyDictionary<string,TConsumer>
    where TProfile : IDictionaryKey
    where TConsumer : class, IEquatable<TProfile>
{
    private Dictionary<string, TConsumer> _dictionary = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    public IEnumerable<string> Keys
    {
        get { return ((IReadOnlyDictionary<string, TConsumer>)_dictionary).Keys; }
    }

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    public IEnumerable<TConsumer> Values
    {
        get { return ((IReadOnlyDictionary<string, TConsumer>)_dictionary).Values; }
    }

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    public TConsumer this[string key]
    {
        get { return ((IReadOnlyDictionary<string, TConsumer>)_dictionary)[key]; }
    }

    /// <summary>
    /// Gets the number of clients.
    /// </summary>
    public int Count
    {
        get { return _dictionary.Count; }
    }

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    public IEnumerator<KeyValuePair<string, TConsumer>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, TConsumer>>)_dictionary).GetEnumerator();
    }

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dictionary).GetEnumerator();
    }

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    public bool ContainsKey(string key)
    {
        return _dictionary.ContainsKey(key);
    }

    /// <summary>
    /// Implements IReadOnlyDictionary.
    /// </summary>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TConsumer value)
    {
        return _dictionary.TryGetValue(key, out value);
    }
    /// <summary>
    /// Clears all. The result is a sequence of removed items.
    /// </summary>
    public IEnumerable<TConsumer> Clear()
    {
        var temp = Values;
        _dictionary.Clear();
        return temp;
    }

    /// <summary>
    /// Gets values with given keys. If one or more individual keys are not found, the result
    /// will contain less items than the given key sequence. If no keys are found, the result
    /// is an empty sequence.
    /// </summary>
    public TConsumer[] GetMany(IEnumerable<string> keys)
    {
        var temp = new List<TConsumer>();

        foreach (var item in keys)
        {
            if (_dictionary.TryGetValue(item, out TConsumer? value))
            {
                temp.Add(value);
            }
        }

        return temp.ToArray();
    }

    /// <summary>
    /// Inserts a new value with the key defined by the profile's <see cref="IKeyId.GetKey()"/> method.
    /// The result is true on success. If an item with given key already, exists the result is false and
    /// the class does nothing. Equivalent to the Dictionary.TryAdd() method.
    /// </summary>
    public bool Insert(TProfile profile)
    {
        if (!_dictionary.ContainsKey(profile.GetKey()))
        {
            // IMPORTANT. We create only once we know that Add() is possible.
            _dictionary.Add(profile.GetKey(), CreateConsumer(profile));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Update or insert a value created from profile, returning true on success. A new item is inserted
    /// if the profile key does not exist. An existing item is replaced if one with a matching key is found AND
    /// the given profile does not have value equality with the existing item. In this case, the existing item
    /// which was replaced it is given out as "old", and the result is true. The result is false if not
    /// new item was added or replaced.
    /// </summary>
    public bool Upsert(TProfile profile, out TConsumer? old)
    {
        return Upsert(_dictionary, profile, out old);
    }

    /// <summary>
    /// Overload.
    /// </summary>
    public bool Upsert(TProfile profile)
    {
        return Upsert(_dictionary, profile, out _);
    }

    /// <summary>
    /// Upserts many, returning an array of any items removed.
    /// </summary>
    public TConsumer[] UpsertMany(IEnumerable<TProfile> profiles, IList<LogMessage>? log = null)
    {
        var temp = new Dictionary<string, TProfile>();
        var removals = new List<TConsumer>(_dictionary.Count);

        foreach (var item in profiles)
        {
            temp.TryAdd(item.GetKey(), item);
        }

        var newItems = new Dictionary<string, TConsumer>(_dictionary.Count);

        foreach (var item in _dictionary)
        {
            if (temp.ContainsKey(item.Key))
            {
                newItems.Add(item.Key, item.Value);
            }
            else
            {
                removals.Add(item.Value);
                log?.Add(new LogMessage($"Deprovision {item.Key}"));
            }
        }

        foreach (var item in temp)
        {
            if (Upsert(newItems, item.Value, out TConsumer? old) && old != null)
            {
                removals.Add(old);

                if (old == null)
                {
                    log?.Add(new LogMessage($"Provision new {item.Key}"));
                }
                else
                {
                    log?.Add(new LogMessage($"Update {item.Key}"));
                }
            }
            else
            {
                log?.Add(new LogMessage($"Unchanged {item.Key}"));
            }
        }

        _dictionary = newItems;
        return removals.ToArray();
    }

    /// <summary>
    /// Removes an item with the given key. On success, the removed item is returned. If not found, the result is null.
    /// </summary>
    public TConsumer? Remove(string key)
    {
        if (_dictionary.TryGetValue(key, out TConsumer? temp))
        {
            _dictionary.Remove(key);
            return temp;
        }

        return null;
    }

    /// <summary>
    /// Creates an instance of TConsumer given a profile containing its implementation.
    /// </summary>
    protected abstract TConsumer CreateConsumer(TProfile profile);

    private bool Upsert(Dictionary<string, TConsumer> dictionary, TProfile profile, out TConsumer? old)
    {
        old = null;
        var key = profile.GetKey();

        if (dictionary.TryGetValue(key, out TConsumer? temp))
        {
            if (temp.Equals(profile))
            {
                // Exists and value equal
                return false;
            }

            old = temp;
        }

        dictionary[key] = CreateConsumer(profile);
        return true;
    }
}