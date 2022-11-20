# Jellyfin jellyfin-subtitle-sorter

<hr>

#### Previous version: [jellyfin-movie-subtitle-sorter](https://github.com/bennystarfighter/jellyfin-movie-subtitle-sorter)

If you're still using the old version. Please uninstall it and move over to this.

<hr>

#### Jellyfin plugin repository can be found here: https://github.com/bennystarfighter/Jellyfin-Plugin-Repository

## About

The Goal of this plugin is to create a generic filter system for detecting subtitles hidden in subfolders, which
Jellyfin most often does not recognize. And then link them to the right location so Jellyfin will recognize them.

This project had a previous version made specifically for movies and with a special file structure in mind. This new and
improved version lets you setup your own basic filters for how your files are located, and most importantly you get to
filter which files the rules applies to.

The idea of this plugin is divided up in two parts. You have your **Identifier** which is a text the plugin matches all
items against and sees if the filename contains that identifier. **WARNING: If your identifier is empty it will run
against all files.** And then you have your **Location filter** which tells the plugins where it should look for the
subtitles. The good thing about this is that you probably have a lot of media which uses the same file structure, but
isn't recognized by jellyfin and that can easily be fixed here.

**The Location filter should always point to a folder.**

The plugin runs automatically after each library scan.<br>And It looks for
these file extensions:
<ul>
<li>.ass</li>
<li>.srt</li>
<li>.ssa</li>
<li>.sub</li>
<li>.idx</li>
<li>.vtt</li>
</ul>

To build your Identifier and Location filter you have som keys that you will use. These keys will be replaced by the
plugin with the data they represent. This will be used inside a general path description to describe where relative to
the file the subtitles are.

### Available keys at this time:

<ul>
<li>{Directory}</li>
<li>{FileName}</li>
</ul>

# Disclaimer
**I am not responsible for anything that may happen to your jellyfin server, your media library,
your cat or anything else as a result of using this plugin.
Check out the code and use it at your own risk.**

# Examples

### Movies

This example is a Movie Filter for all movie files having **"GRAR"** in its name.<br>
Identifier: GRAR<br>
Location filter: {Directory}/Subs

File structure before sorting:

```
- MovieName.GRAR.1080p/
--- MovieName.Grar.1080p.mkv
--- Subs/
------ English.srt
------ Spanish.srt
```

File Structure after sorting with the filter above:

```
- MovieName.GRAR.1080p/<br>
--- Subs/<br>
------ English.srt<br>
 ------ Spanish.srt<br>
--- MovieName.Grar.1080p.mkv<br>
--- MovieName.Grar.1080p.English.srt (Symlink)<br>
--- MovieName.Grar.1080p.Spanish.srt (Symlink)<br>
```

<hr>

### TV

This example is an Episode filter (TV Shows) for all episode files having "<b>GRAR</b>" in its name.<br>
Identifier: GRAR<br>
Location filter: {Directory}/Subs/{FileName}

File structure before sorting:
```
- TvShowName.1080p.GRAR/
--- Subs/
------ TvShowName.s01e01.GRAR
--------- English.srt
--------- Spanish.srt
------ TvShowName.s01e02.GRAR
--------- English.srt
--------- Spanish.srt
--- TvShowName.s01e01.GRAR.mkv
--- TvShowName.s01e02.GRAR.mkv
```

File Structure after sorting with the filter above:
```
- TvShowName.1080p.GRAR/
--- Subs/
------ TvShowName.s01e01.GRAR
--------- English.srt
--------- Spanish.srt
------ TvShowName.s01e02.GRAR
--------- English.srt
--------- Spanish.srt
--- TvShowName.s01e01.GRAR.mkv
--- TvShowName.s01e01.GRAR.English.srt (Symlink)
--- TvShowName.s01e01.GRAR.Spanish.srt (Symlink)
--- TvShowName.s01e02.GRAR.mkv
--- TvShowName.s01e02.GRAR.English.srt (Symlink)
--- TvShowName.s01e02.GRAR.Spanish.srt (Symlink)
```
