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

namespace KuiperZone.Implink.Gateway;

/// <summary>
/// Simple request rate counter. Thread safe.
/// </summary>
public class RateCounter
{
    private readonly object _syncObj = new();
    private DateTime _epoch = DateTime.UtcNow;
    private long _counter;

    /// <summary>
    /// Constructor with maximum rate in requests per minute.
    /// </summary>
    public RateCounter(int maxRate)
    {
        MaxRate = maxRate;
    }

    /// <summary>
    /// Gets the maximum rate in requests per minute. A zero or negative value disables throttling.
    /// </summary>
    public int MaxRate { get; }

    /// <summary>
    /// Resets rate.
    /// </summary>
    public void Reset()
    {
        lock (_syncObj)
        {
            _epoch = DateTime.UtcNow;
            _counter = 0;
        }
    }

    /// <summary>
    /// Increments.
    /// </summary>
    public void Increment()
    {
        lock (_syncObj)
        {
            _counter += 1;
        }
    }

    /// <summary>
    /// Gets requests per minute, as signaled by a call to <see cref="Increment"/>.
    /// </summary>
    public double GetRate()
    {
        lock (_syncObj)
        {
            return GetRateNoSync();
        }
    }

    /// <summary>
    /// Returns true if <see cref="GetRate"/> is above equal to <see cref="MaxRate"/>.
    /// Always returns false if <see cref="MaxRate"/> is 0 or less.
    /// </summary>
    public bool IsThrottled(bool inc = false)
    {
        lock (_syncObj)
        {
            if (MaxRate > 0 && GetRateNoSync() >= MaxRate)
            {
                return true;
            }

            if (inc)
            {
                _counter += 1;
            }
        }

        return false;
    }

    private double GetRateNoSync()
    {
        var now = DateTime.UtcNow;
        var sec = (now - _epoch).TotalSeconds;

        if (sec < 0 || sec >= 60)
        {
            _epoch = now;
            _counter = 0;
        }

        return _counter;
    }

}