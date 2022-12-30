#pragma warning disable CA2227

namespace Jellyfin.Plugin.SubtitleSorter.Configuration;

using System.Collections.ObjectModel;
using MediaBrowser.Model.Plugins;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    // private const string FormatterDirectory = "Directory";
    // private const string FormatterName = "FileName";

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        this.Enabled = true;

        this.Filters = new Collection<Filter>();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin run any sorting at all.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets Filters to find subtitles.
    /// </summary>
    public Collection<Filter> Filters { get; set; }
}
