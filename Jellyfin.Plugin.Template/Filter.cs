using System;

namespace Jellyfin.Plugin.SubtitleSorter;

#pragma warning disable CS0162, CS1591, SA1201
public struct Filter : IEquatable<Filter>
{
    internal bool _enabled;
    internal string _identifier;
    internal string _locationFilter;

    public Filter(bool enabled, string identifier, string locationFilter)
    {
        _enabled = enabled;
        _identifier = identifier;
        _locationFilter = locationFilter;
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_enabled, _identifier, _locationFilter);
    }

    public bool Equals(Filter other)
    {
        return _enabled == other._enabled && _identifier == other._identifier && _locationFilter == other._locationFilter;
    }

    public static bool operator ==(Filter filter1, Filter filter2)
    {
        return filter1.Equals(filter2);
    }

    public static bool operator !=(Filter filter1, Filter filter2)
    {
        return !(filter1 == filter2);
    }
}
