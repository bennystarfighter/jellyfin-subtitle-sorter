using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SubtitleSorter;

#pragma warning disable CS1591
/// <inheritdoc />
[SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "<Pending>")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1404:Code analysis suppression should have justification")]
[SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness")]
public class SubtitleSorter : ILibraryPostScanTask
{
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly ILibraryManager _libraryManager;
    private readonly ISubtitleManager _subtitleManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ILogger<SubtitleSorter> _logger;
    private readonly IFileSystem _fileSystem;

    public SubtitleSorter(
        ILibraryMonitor libraryMonitor,
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        ISubtitleManager subtitleManager,
        IMediaSourceManager mediaSourceManager)
    {
        _libraryMonitor = libraryMonitor;
        _libraryManager = libraryManager;
        _logger = loggerFactory.CreateLogger<SubtitleSorter>();
        _fileSystem = fileSystem;
        _subtitleManager = subtitleManager;
        _mediaSourceManager = mediaSourceManager;
    }

    /// <inheritdoc />
    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);

        // Find Movies
        _logger.LogInformation("Finding Movies");
        var allVirtualFolders = _libraryManager.GetVirtualFolders();
        var movieQuery = new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Movie }, IsVirtualItem = false, OrderBy = new List<(string, SortOrder)> { (ItemSortBy.SortName, SortOrder.Ascending) }, Recursive = true };
        var allMovies = _libraryManager.GetItemList(movieQuery, false)
            .Select(m => m as MediaBrowser.Controller.Entities.Movies.Movie).ToList();

        // Find Episodes
        _logger.LogInformation("Finding Episodes");
        var episodeQuery = new InternalItemsQuery { IncludeItemTypes = new[] { BaseItemKind.Episode }, IsVirtualItem = false, OrderBy = new List<(string, SortOrder)> { (ItemSortBy.SortName, SortOrder.Ascending) }, Recursive = true };
        var allEpisodes = _libraryManager.GetItemList(episodeQuery, false)
            .Select(m => m as MediaBrowser.Controller.Entities.TV.Episode).ToList();

        int objectsFound = allMovies.Count + allEpisodes.Count;

        // Run Movie Sorter
        var completedCount = 0;
        _logger.LogInformation("Found [{0}] eligible movies", allMovies.Count);

        foreach (var movie in allMovies)
        {
            if (movie != null)
            {
                _logger.LogDebug("Checking movie: {0}", movie.Name);
                _logger.LogDebug("Movie path: {0}", movie.Path);
                if (string.IsNullOrEmpty(movie.Path))
                {
                    continue;
                }

                var movieDir = Directory.GetParent(movie.Path);
                if (movieDir == null)
                {
                    continue;
                }

                var movieParent = movieDir.FullName;

                // Check if movie is in root folder of any movie library location.
                var isInRoot = false;
                foreach (var virtualFolder in allVirtualFolders)
                {
                    foreach (var location in virtualFolder.Locations)
                    {
                        if (location == movieParent)
                        {
                            isInRoot = true;
                        }
                    }
                }

                // We dont process movies directly in the root folder.
                // Causes root folder to fill upp with all subtitles from the actual movie subfolders.
                if (isInRoot)
                {
                    continue;
                }

                bool hasNewSubtitle = false;

                var dirName = Path.GetDirectoryName(movie.Path);
                if (dirName != null)
                {
                    // loop through subfolder inside movie folder
                    foreach (var subFolder in Directory.GetDirectories(dirName))
                    {
                        // Check all files in subfolder
                        foreach (var potentialSubFile in Directory.GetFiles(subFolder))
                        {
                            if (!new[] { ".ass", ".srt", ".ssa", ".sub", ".idx", ".vtt" }.Contains(Path
                                    .GetExtension(potentialSubFile).ToLower()))
                            {
                                continue;
                            }

                            // New method to try and get working later. This would remove the need for a symbolic link / copy of the subtitle file.
                            // Cant get the adding of the subtitle filepath to the library entry working.
                            /*
                        if (!movie.SubtitleFiles.Contains(potentialSubFile))
                        {
                            movie.SubtitleFiles = movie.SubtitleFiles.Append(potentialSubFile).ToArray();
                            movie.HasSubtitles = true;
                        }
                        */

                            _logger.LogDebug("Found eligible subtitle file: {0}", potentialSubFile);

                            var newSubFilePath =
                                RemoveExtensionFromPath(
                                    Path.GetFullPath(movie.Path),
                                    Path.GetExtension(movie.Path)) + "." + Path.GetFileName(potentialSubFile);

                            // Continue if file with same name already exists.
                            if (File.Exists(newSubFilePath))
                            {
                                continue;
                            }

                            hasNewSubtitle = true;

                            // Copy subtitle to movie folder
                            CopySubtitleFile(potentialSubFile, newSubFilePath);

                            // TODO: Trigger a scan on this movie again so the new subtitle files will be discovered
                        }
                    }
                }

                if (hasNewSubtitle)
                {
                    movie.ChangedExternally();
                }
            }

            ++completedCount;

            // calc percentage (current / maximum) * 100
            progress.Report((completedCount / objectsFound) * 100);
        }

        // Run episode sorter
        {
        }

        return Task.CompletedTask;
    }

    private string RemoveExtensionFromPath(string input, string extension)
    {
        return input.EndsWith(extension) ? input[..input.LastIndexOf(extension, StringComparison.Ordinal)] : input;
    }

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
                    _logger.LogError(ex, "Error copying subtitle file {0}. Error: {1}", fileToCopy, e.ToString());
                }
            }
            else
            {
                _logger.LogError(ex, "Error creating subtitle symbolic link {0}. Error: {1}", fileToCopy, ex.ToString());
            }
        }
    }
}
