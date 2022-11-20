using System;

namespace Jellyfin.Plugin.SubtitleSorter;

#pragma warning disable CS0162, CS1591, SA1201
public class Filter
{
    public bool Enabled { get; set; }

    public string Identifier { get; set; }

    public string LocationFilter { get; set; }

    public Filter()
    {
        this.Enabled = true;
        this.Identifier = string.Empty;
        this.LocationFilter = string.Empty;
    }
}
