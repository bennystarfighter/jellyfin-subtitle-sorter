namespace Jellyfin.Plugin.SubtitleSorter;

public enum FilterType
{
    None,
    Movie,
    Episode,
    Video,
}

public class Filter
{
    public bool Enabled { get; set; } = true;

    public FilterType Type { get; set; } = FilterType.None;

    public string Identifier { get; set; } = string.Empty;

    public string LocationFilter { get; set; } = string.Empty;
}
