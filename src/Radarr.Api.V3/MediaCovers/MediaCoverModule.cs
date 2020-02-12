using System.IO;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.Responses;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace Radarr.Api.V3.MediaCovers
{
    public class MediaCoverModule : RadarrV3Module
    {
        private const string MEDIA_COVER_ROUTE = @"/(?<movieId>\d+)/(?<filename>(.+)\.(jpg|png|gif))";
        private static readonly Regex RegexResizedImage = new Regex(@"-\d+\.jpg$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IAppFolderInfo _appFolderInfo;
        private readonly IDiskProvider _diskProvider;

        public MediaCoverModule(IAppFolderInfo appFolderInfo, IDiskProvider diskProvider)
            : base("MediaCover")
        {
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;

            Get(MEDIA_COVER_ROUTE, options => GetMediaCover(options.movieId, options.filename));
        }

        private object GetMediaCover(int movieId, string filename)
        {
            var filePath = Path.Combine(_appFolderInfo.GetAppDataPath(), "MediaCover", movieId.ToString(), filename);

            if (!_diskProvider.FileExists(filePath) || _diskProvider.GetFileSize(filePath) == 0)
            {
                // Return the full sized image if someone requests a non-existing resized one.
                // TODO: This code can be removed later once everyone had the update for a while.
                var basefilePath = RegexResizedImage.Replace(filePath, ".jpg");
                if (basefilePath == filePath || !_diskProvider.FileExists(basefilePath))
                {
                    return new NotFoundResponse();
                }

                filePath = basefilePath;
            }

            return new StreamResponse(() => File.OpenRead(filePath), MimeTypes.GetMimeType(filePath));
        }
    }
}
