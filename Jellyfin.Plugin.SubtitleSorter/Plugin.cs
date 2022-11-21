#pragma warning disable CS0162
#pragma warning disable CS1591
#pragma warning disable SA1111
#pragma warning disable SA1507
#pragma warning disable CA1304
#pragma warning disable CA1310
#pragma warning disable SA1404
#pragma warning disable SA1402
#pragma warning disable SA1505
#pragma warning disable SA1508
#pragma warning disable SA1201
#pragma warning disable SA1203

namespace Jellyfin.Plugin.SubtitleSorter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Jellyfin.Data.Enums;
    using Jellyfin.Plugin.SubtitleSorter.Configuration;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Model.IO;
    using MediaBrowser.Model.Plugins;
    using MediaBrowser.Model.Querying;
    using MediaBrowser.Model.Serialization;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The main plugin.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore")]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this")]
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, ILibraryPostScanTask
    {
        /*
            private readonly ILibraryManager _libraryManager;
            private readonly ILogger<SubtitleSorter> _logger;
            private readonly IFileSystem _fileSystem;

            public SubtitleSorter(
                ILibraryManager libraryManager,
                ILoggerFactory loggerFactory,
                IFileSystem fileSystem)
            {
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger<SubtitleSorter>();
            _fileSystem = fileSystem;
        }
         */

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILibraryManager libraryManager, ILoggerFactory loggerFactory, IFileSystem fileSystem)
            : base(applicationPaths, xmlSerializer)
        {
            this._libraryManager = libraryManager;
            this._logger = loggerFactory.CreateLogger<Plugin>();
            this._fileSystem = fileSystem;
            Instance = this;
        }

        /// <inheritdoc />
        public override string Name => "Subtitle Sorter";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("e1f630f5-6605-43be-96d8-d89a39c4d946");

        /// <inheritdoc />
        public override string Description => "Lets you set filters to properly copy subtitles to the appropriate location.";

        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<Plugin> _logger;
        private readonly IFileSystem _fileSystem;

        private const bool DebugMode = false;
        private const string FormatterDirectory = "Directory";
        private const string FormatterName = "FileName";
        private static readonly string[] _subtitleFileExtensions = new string[] { ".ass", ".srt", ".ssa", ".sub", ".idx", ".vtt" };

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc/>
        public override void SaveConfiguration(PluginConfiguration config)
        {
            if (Instance != null)
            {
                Instance.Configuration = config;
            }

            base.SaveConfiguration(config);
        }


        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[] { new PluginPageInfo { Name = this.Name, EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace) } };
        }

        /// <inheritdoc />
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            if (Instance?.Configuration == null || !Instance.Configuration.Enabled)
            {
                return Task.CompletedTask;
            }

            Collection<Filter>? movieFilters = Instance?.Configuration.MovieFilters;
            Collection<Filter>? episodeFilters = Instance?.Configuration.EpisodeFilters;

            if (movieFilters == null)
            {
                _logger.LogError("Movie Filters == Null");
                return Task.CompletedTask;
            }

            if (episodeFilters == null)
            {
                _logger.LogError("Episode Filters == Null");
                return Task.CompletedTask;
            }

            _logger.LogDebug("Current configuration:");
            foreach (var filter in movieFilters)
            {
                _logger.LogDebug("Movie Filter | Enabled: {Enabled} Identifier: {Identifier} LocationFilter: {Loc}", filter.Enabled, filter.Identifier, filter.LocationFilter);
            }

            foreach (var filter in episodeFilters)
            {
                _logger.LogDebug("Episode Filter | Enabled: {Enabled} Identifier: {Identifier} LocationFilter: {Loc}", filter.Enabled, filter.Identifier, filter.LocationFilter);
            }

            // Find Movies
            _logger.LogInformation("Finding Movies");
            var movieQuery = new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Movie }, IsVirtualItem = false, OrderBy = new List<(string, SortOrder)> { (ItemSortBy.SortName, SortOrder.Ascending) }, Recursive = true };
            var allMovies = _libraryManager.GetItemList(movieQuery, false)
                .Select(m => m as MediaBrowser.Controller.Entities.Movies.Movie).ToList();
            _logger.LogInformation("Found [{AllMoviesCount}] eligible movies", allMovies.Count);

            // Find Episodes
            _logger.LogInformation("Finding Episodes");
            var episodeQuery = new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Episode }, IsVirtualItem = false, OrderBy = new List<(string, SortOrder)> { (ItemSortBy.SortName, SortOrder.Ascending) }, Recursive = true };
            var allEpisodes = _libraryManager.GetItemList(episodeQuery, false)
                .Select(m => m as MediaBrowser.Controller.Entities.TV.Episode).ToList();
            _logger.LogInformation("Found [{AllEpisodesCount}] eligible episodes", allEpisodes.Count);

            int objectsFound = allMovies.Count + allEpisodes.Count;
            var completedCount = 0;

            // Run Movie Sorter
            foreach (var movie in allMovies)
            {
                if (movie == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(movie.Path))
                {
                    continue;
                }

                foreach (var filter in movieFilters)
                {
                    try
                    {
                        RunSorter(movie, filter);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Failure during sorting: {Error}", e);
                    }
                }

                ++completedCount;

                // calc percentage (current / maximum) * 100
                progress.Report((completedCount / objectsFound) * 100);
            }

            // Run episode sorter
            foreach (var episode in allEpisodes)
            {
                if (episode == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(episode.Path))
                {
                    continue;
                }

                foreach (var filter in episodeFilters)
                {
                    try
                    {
                        RunSorter(episode, filter);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Failure during sorting: {Error}", e);
                    }
                }

                ++completedCount;

                // calc percentage (current / maximum) * 100
                progress.Report((completedCount / objectsFound) * 100);
            }

            return Task.CompletedTask;
        }

        /*
        private string RemoveExtensionFromPath(string input, string extension)
        {
            return input.EndsWith(extension) ? input[..input.LastIndexOf(extension, StringComparison.Ordinal)] : input;
        }
        */

        private void CopySubtitleFile(string fileToCopy, string newFilePath)
        {
            try
            {
                File.CreateSymbolicLink(newFilePath, fileToCopy);
            }
            catch (Exception ex)
            {
                // Try copy file if symbolic link creation fails
                if (ex.GetType().IsAssignableFrom(typeof(IOException)))
                {
                    try
                    {
                        File.Copy(fileToCopy, newFilePath);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(ex, "Error copying subtitle file {File}. Error: {Error}", fileToCopy, e.ToString());
                    }
                }
                else
                {
                    _logger.LogError(ex, "Error creating subtitle symbolic link {Link}. Error: {Error}", fileToCopy, ex.ToString());
                }
            }
        }

        private static string ProcessLocationFilter(BaseItem item, string locationFilter)
        {
            string subtitlesLocation = locationFilter;
            subtitlesLocation = subtitlesLocation.Replace("{" + FormatterDirectory + "}", Path.GetDirectoryName(item.Path), StringComparison.Ordinal);
            subtitlesLocation = subtitlesLocation.Replace("{" + FormatterName + "}", item.FileNameWithoutExtension, StringComparison.Ordinal);
            return subtitlesLocation;
        }

        private void RunSorter(BaseItem item, Filter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Identifier))
            {
                if (!item.Path.Contains(filter.Identifier, StringComparison.Ordinal))
                {
                    return;
                }
            }

            string subtitlesLocation = ProcessLocationFilter(item, filter.LocationFilter);
            if (!_fileSystem.DirectoryExists(subtitlesLocation))
            {
                return;
            }

            IEnumerable<FileSystemMetadata> folderFiles;
            try
            {
                folderFiles = _fileSystem.GetFiles(subtitlesLocation);
            }
            catch (Exception e)
            {
                _logger.LogDebug("{Error}", e);
                return;
            }

            List<FileSystemMetadata> subtitleFiles = new List<FileSystemMetadata>();
            foreach (var file in folderFiles)
            {
                if (_subtitleFileExtensions.Contains(file.Extension))
                {
                    subtitleFiles.Add(file);
                }
            }

            foreach (var subFile in subtitleFiles)
            {
                string newSubFile = Path.Join(Path.GetDirectoryName(item.Path), Path.GetFileNameWithoutExtension(item.Path)) + "." + subFile.Name;
                if (_fileSystem.FileExists(newSubFile))
                {
                    continue;
                }

                _logger.LogDebug("Found: {SubFile} \nCreating: {NewFile}", subFile.FullName, newSubFile);
                CopySubtitleFile(subFile.FullName, newSubFile);
                item.ChangedExternally();
            }
        }
    }
}
