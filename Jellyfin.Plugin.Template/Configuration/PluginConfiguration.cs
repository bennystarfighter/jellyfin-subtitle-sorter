using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MediaBrowser.Model.Plugins;

#pragma warning disable CA2227

namespace Jellyfin.Plugin.SubtitleSorter.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private const string FormatterDirectory = "Directory";
    // private const string FormatterName = "FileName";

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Enabled = true;
        // set default options here
        MovieFilters = new Collection<Filter>();
        EpisodeFilters = new Collection<Filter>(new List<Filter>(Array.Empty<Filter>()));
        MovieFilters.Add(new Filter() { _identifier = string.Empty, _locationFilter = "{" + FormatterDirectory + "}/Subs" });
    }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin run any sorting at all.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets Filters to find subtitles to movies.
    /// </summary>
    public Collection<Filter> MovieFilters { get; set; }

    /// <summary>
    /// Gets or sets Filters to find subtitles to episodes.
    /// </summary>
    public Collection<Filter> EpisodeFilters { get; set; }
}
