using System.Net;

namespace DatasetGen
{
    // Partial class handling audio entries.
    //
    public static partial class MediaWiki
    {
        private static List<AudioResourceEntry> GetAllAudioResourcesProxy(List<SubscriptEntry> entries)
        {
            try
            {
                return GetAllAudioResources(entries);
            }
            catch (Exception)
            {
                Console.WriteLine("error: critical error occured during audio collection...\n\tterminating.");
                throw;
            }
        }

        private static List<AudioResourceEntry> GetAllAudioResources(List<SubscriptEntry> entries)
        {
            var result = new List<AudioResourceEntry>();

            Dictionary<string,Tuple<string, Guid>> urls = GetApiUrlsForAudioResources(entries);

            if (!Directory.Exists(saveDirPath))
                Directory.CreateDirectory(saveDirPath);

            foreach (string consolidatedDir in Speakers.Entities)
            {
                Directory.CreateDirectory(saveDirPath + "/" + consolidatedDir.ToLower() + wavDir);
            }

            foreach (KeyValuePair<string,Tuple<string,Guid>> fileNamePlusUrl in urls)
            {
                string fileName = fileNamePlusUrl.Key;

                string which =    (!fileNamePlusUrl.Key.Contains("Cm_"))
                                ? ("/" + fileNamePlusUrl.Key.Split('_')[0].ToLower())
                                : ("/" + fileNamePlusUrl.Key.Split('_')[1].ToLower());

                using var client = new WebClient();

                try
                {
                    string targetLocation = string.Concat(saveDirPath, which, wavDir);
                    client.DownloadFile(fileNamePlusUrl.Value.Item1, string.Concat(targetLocation, fileName));

                    var trainingRecord = new AudioResourceEntry()
                    {
                        IsSavedLocally = true,
                        AbsolutePath = targetLocation + fileName,
                        ApiUrl = fileNamePlusUrl.Value.Item1,
                        ForeignKeyAsSubscriptEntryId = fileNamePlusUrl.Value.Item2
                    };

                    result.Add(trainingRecord);
                }
                catch (Exception)
                {
                    ++WavFilesDroppedCount;
                    Console.WriteLine("warning: dropped wav download... \n\tfile: " + fileName);

                    continue;
                }
            }

            return result;
        }
    }
}