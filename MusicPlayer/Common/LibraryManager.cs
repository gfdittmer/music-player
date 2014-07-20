using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using Windows.Data.Json;
using Windows.Storage;

namespace MusicPlayer.Common
{
    /// <summary>
    /// Manages the user's library.
    /// </summary>
    public static class LibraryManager
    {
        /// <summary>
        /// Determines if the user's library has been created.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> Exists()
        {
            StorageFile library = null;

            try
            {
                //library = await GetLibraryFile();
            }
            catch (FileNotFoundException)
            {
                return false;
            }

            return library != null;
        }

        /// <summary>
        /// Gets the library file.
        /// </summary>
        /// <returns></returns>
        public static async Task<StorageFile> GetLibraryFile()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            return await localFolder.GetFileAsync(LibraryFileName);
        }

        public static async void InitializeLibrary(IProgress<InitializeLibraryTaskProgress> progress)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // get the music library folder
            StorageFolder musicFolder = KnownFolders.MusicLibrary;

            progress.Report(new InitializeLibraryTaskProgress() { Message = "Finding music files..." });

            // loop through every file in the library
            int fileCount = await countFiles(musicFolder, 0, progress);

            await importFiles(musicFolder, fileCount, 0, null, progress);
        }

        private static async Task<int> countFiles(StorageFolder folder, int fileCount, IProgress<InitializeLibraryTaskProgress> progress)
        {
            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

            // count each file
            foreach (StorageFile file in files)
            {
                if (file.ContentType == "audio/mpeg")
                {
                    fileCount++;

                    progress.Report(new InitializeLibraryTaskProgress
                    {
                        Message = "Finding music files..." + fileCount,
                        Progress = 0
                    });
                }
            }

            foreach (StorageFolder childFolder in folders)
            {
                fileCount = await countFiles(childFolder, fileCount, progress);
            }

            return fileCount;
        }

        private static async Task<int> importFiles(StorageFolder folder, int totalCount, int currentCount, JsonObject library, IProgress<InitializeLibraryTaskProgress> progress)
        {
            IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
            IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();

            // create the library json data
            if (library == null)
            {
                library = JsonObject.Parse("{ \"artists\": [] }");
            }

            foreach (StorageFile file in files)
            {
                if (file.ContentType == "audio/mpeg")
                {
                    using (Stream fileStream = await file.OpenStreamForReadAsync())
                    {
                        // get the id3 info
                        TagLib.File tagFile = TagLib.File.Create(new StreamFileAbstraction(file.Name, fileStream, fileStream));
                        TagLib.Tag tag = tagFile.GetTag(TagTypes.Id3v2);

                        // get the album from the library file
                        JsonObject album = GetAlbum(library, tag);
                        JsonObject track = new JsonObject();
                        track.Add("title", JsonValue.CreateStringValue(tag.Title));
                        track.Add("number", JsonValue.CreateNumberValue(tag.Track));

                        currentCount++;

                        // add tracks to the library
                        album.GetNamedArray("tracks").Add(track);

                        // notify user of progress
                        progress.Report(new InitializeLibraryTaskProgress {
                            Progress = (int)Math.Round((float)currentCount / (float)totalCount * 100),
                            Message = "Importing " + tag.Album + " - " + tag.Title + " (" + currentCount + " / " + totalCount + ")"
                        });
                    }
                }
            }

            foreach (StorageFolder childFolder in folders)
            {
               currentCount = await importFiles(childFolder, totalCount, currentCount, library, progress);
            }

            return currentCount;
        }

        private static JsonObject GetAlbum(JsonObject library, TagLib.Tag tag)
        {
            JsonArray artists = library.GetNamedArray("artists");

            // create the array if it doesn't exist
            if (artists == null)
            {
                artists = new JsonArray();
                library.Add("artists", artists);
            }

            JsonObject matchedArtist = null;
            JsonObject matchedAlbum = null;
            string artistName = "Unknown Artist";

            if (!string.IsNullOrEmpty(tag.FirstAlbumArtist))
            {
                artistName = tag.FirstAlbumArtist;
            }
            else if (!string.IsNullOrEmpty(tag.FirstArtist))
            {
                artistName = tag.FirstArtist;
            }
            else if (!string.IsNullOrEmpty(tag.FirstPerformer))
            {
                artistName = tag.FirstPerformer;
            }
            else if (!string.IsNullOrEmpty(tag.FirstComposer))
            {
                artistName = tag.FirstComposer;
            }

            foreach (JsonObject artist in artists.Select(x => x.GetObject()))
            {
                if (artist.GetNamedString("name") == artistName)
                {
                    matchedArtist = artist;
                }
            }

            // no artist found, create it
            if (matchedArtist == null)
            {
                matchedArtist = new JsonObject();
                matchedArtist.Add("name", JsonValue.CreateStringValue(artistName));
                matchedArtist.Add("albums", new JsonArray());
                artists.Add(matchedArtist);
            }

            // find the album
            JsonArray albums = matchedArtist.GetNamedArray("albums");

            // check to see if the albums array exists
            if (albums == null)
            {
                albums = new JsonArray();
                matchedArtist.Add("albums", albums);
            }

            // find the album
            foreach (JsonObject album in albums.Select(x => x.GetObject()))
            {
                if (album.GetNamedString("name") == tag.Album)
                {
                    matchedAlbum = album;
                }
            }

            // if the album wasn't found, create it
            if (matchedAlbum == null)
            {
                matchedAlbum = new JsonObject();
                matchedAlbum.Add("name", JsonValue.CreateStringValue(tag.Album));
                matchedAlbum.Add("tracks", new JsonArray());
                albums.Add(matchedAlbum);
            }

            return matchedAlbum;
        }

        private const string LibraryFileName = "library.json";
    }

    public class InitializeLibraryTaskProgress
    {
        public int Progress { get; set; }
        public string Message { get; set; }
    }
}
