using System.Net;

namespace DatasetGen
{
    public static partial class MediaWiki
    {
        private static List<TrainingAudio> GetAllAudioResourcesProxy(List<TrainingEntry> entries)
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

        private static List<TrainingAudio> GetAllAudioResources(List<TrainingEntry> entries)
        {
            var result = new List<TrainingAudio>();

            Dictionary<string,string> urls = GetApiUrlsForAudioResources(entries);

            if (!Directory.Exists(saveDirPath))
                Directory.CreateDirectory(saveDirPath);

            foreach (string consolidatedDir in TrainingTargets.Speakers)
            {
                Directory.CreateDirectory(saveDirPath + "/" + consolidatedDir.ToLower() + wavDir);
            }

            foreach (KeyValuePair<string,string> fileNamePlusUrl in urls)
            {
                string fileName = fileNamePlusUrl.Key;

                string which =    (!fileNamePlusUrl.Key.Contains("Cm_"))
                                ? ("/" + fileNamePlusUrl.Key.Split('_')[0].ToLower())
                                : ("/" + fileNamePlusUrl.Key.Split('_')[1].ToLower());

                using var client = new WebClient();

                try
                {
                    string targetLocation = string.Concat(saveDirPath, which, wavDir);
                    client.DownloadFile(fileNamePlusUrl.Value, string.Concat(targetLocation, fileName));

                    var trainingRecord = new TrainingAudio()
                    {
                        IsSavedLocally = true,
                        AbsolutePath = targetLocation + fileName,
                        ApiUrl = fileNamePlusUrl.Value
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