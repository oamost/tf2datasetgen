using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Data;

namespace PiperTrainingCsvTf2Gen
{
    public partial class TeamFortressMediaWiki
    {
        private const string apiBaseUrl                 = "https://wiki.teamfortress.com/w/api.php?";
        public static readonly string saveDirPath       = "tf2vo_ttstrain_dataset";
        public static readonly string wavDir            = "/wav/";

        private readonly struct TrainingCategories
        {
            public readonly static List<string> Types =
            [
                 "Responses",
                 "Voice commands"
            ];
        }

        public readonly struct TrainingTargets
        {
            public readonly static List<string> Speakers =
            [
                 // Playable (all).
                 //
                 "Soldier",
                 "Pyro",
                 "Demoman",
                 "Heavy",
                 "Engineer",
                 "Medic",
                 "Sniper",
                 "Spy",

                 // Non-playable (just a few that can speak normally).
                 //
                 "Administrator",
                 "Miss Pauling"
            ];
        }

        public int EntriesFailedToParseCount = 0;
        public int AudioUrlsDroppedCount = 0;
        public int WavFilesDroppedCount = 0;
        public class TrainingEntry
        {
            public string? Owner;
            public string? WavId;
            public string? TransScript;
        }

        public struct TrainingAudio
        {
            public string AbsolutePath;
            public string ApiUrl;
            public bool   IsSavedLocally;
        }

        public struct VoiceTrainingData
        {
            public List<TrainingEntry> TrainingTextEntries;
            public List<TrainingAudio> TrainingAudioEntries;
        }

        private static List<string> GetApiUrlsForTextResources()
        {
            var result = new List<string>();

            for (int i = 0; i < TrainingCategories.Types.Count; i++)
            {
                for (int j = 0; j < TrainingTargets.Speakers.Count; j++)
                {
                    if ( TrainingCategories.Types[i] == "Voice commands" &&
                        (TrainingTargets.Speakers[j] == "Administrator" ||
                         TrainingTargets.Speakers[j] == "Miss Pauling"))
                            { continue; } // Avoid generating voice commands for these actors for now.

                    var builder = new StringBuilder(apiBaseUrl);

                    builder.Append("action=query&format=json&prop=revisions&titles=");
                    builder.Append(TrainingTargets.Speakers[j]);
                    builder.Append("%20");
                    builder.Append(TrainingCategories.Types[i].ToLower());
                    builder.Append("&utf8=1&ascii=1&rvprop=content");

                    result.Add(builder.ToString());
                }
            }

            return result;
        }

        private int whereWasI = 0;
        private string GetQueryStringForBatch(List<TrainingEntry> entries, out bool terminateQuery, int limit = 25)
        {
            string result = string.Empty;
            terminateQuery = false;
            
            int current = 0;
            int i = whereWasI;

            if (whereWasI >= entries.Count)
            {
                terminateQuery = true;
                return result; 
            }
                

            for (; i < entries.Count; i++)
            {
                if (current == limit + 1)
                    break; // Stop when reached the API limit.

                if (current == 0 || current == limit - 1 || current + 1 == entries.Count)
                    result += "File%3A" + entries[i].WavId;
                else
                    result += "%7C" + "File%3A" + entries[i].WavId + "%7C";

                ++current;
            }

            whereWasI += limit;

            return result;
        }

        private Dictionary<string,string> GetApiUrlsForAudioResources(List<TrainingEntry> entries)
        {
            var result = new Dictionary<string,string>();

            const int limit = 25;

            for (int i = 0; i < entries.Count;)
            {
                var builder = new StringBuilder(apiBaseUrl);

                builder.Append("action=query&format=json&prop=imageinfo&titles=File%3A");
                builder.Append(GetQueryStringForBatch(entries, out bool terminateQuery, limit));
                builder.Append("&iiprop=url");

                if (terminateQuery)
                    break;

                try
                {
                    string batchUrl = builder.ToString();

                    var client = new HttpClient();
                    string response = client.GetStringAsync(batchUrl).Result;

                    // "\"url\":\\s*\"([^\"]+)\""
                    //
                    var wavUrlRegex = MyRegex();
                    MatchCollection wavUrls = wavUrlRegex.Matches(response);
                    int j = i;

                    foreach (Match wavUrl in wavUrls.Cast<Match>())
                    {
                        string url = wavUrl.Groups[1].Value;                        

                        if (j < (i + limit))
                        {
                            string targetID = url.Split('/').Last(); // It will be always last.
                            var assert = entries.Where(e => e.WavId == targetID).FirstOrDefault();

                            if (assert == null)
                            {
                                ++j;

                                string ts = entries[i].TransScript;
                                string ti = targetID;

                                Console.WriteLine("warning: dropped a wav url. \nreason: missing wav id.\n\tentry: \'" + entries[i].TransScript + "\'");

                                continue;
                            }

                            result[targetID] = url;
                            ++j;
                        }
                    }

                    i = j;
                }
                catch (AggregateException)
                {
                    Console.WriteLine("warning: httpclient threw an exception... discarding entry: " + entries[i].WavId);

                    continue;
                }
            }

            float lossPercentage = ((entries.Count - result.Count) / entries.Count) * 100;
            int total = entries.Count - result.Count;

            Console.WriteLine("==============");
            Console.WriteLine("info: audio/dropped " + AudioUrlsDroppedCount + "[" + (int)lossPercentage + "%]");
            Console.WriteLine("info: audio/total " + total);
            Console.WriteLine("==============");

            Thread.Sleep(2000);

            return result;
        }

        private List<TrainingEntry> GetParsedEntries(string rawData, Guid responseID)
        {
            var result = new List<TrainingEntry>();

            // Parse received data into training entries.
            // There will be inconsistency. Handling all of that here.
            //
            const string pattern = @"\[\[(.*?)\]\]";
            MatchCollection patternMatches = Regex.Matches(rawData, pattern);

            foreach (Match match in patternMatches.Cast<Match>())
            {
                // Get the transscript first.
                //
                string dirty = match.Groups[1].Value;

                // Skip non-transcriptable voice lines.
                //
                if (!dirty.Contains("Media:"))
                    continue;
                if (dirty.Contains("BLU"))
                    continue;
                if (dirty.Contains("RED"))
                    continue;

                var entry = new TrainingEntry();

                string[] pair = dirty.Split('|');

                entry.TransScript = pair[1].Replace("\\", string.Empty)
                                           .Replace("\"", string.Empty);

                string tmp = pair[0].Replace("Media:", string.Empty);

                // Replace whitespaces after the first one with an underline,
                // just like with the corresponding "real" mediawiki wav url.
                //
                var underlining = new StringBuilder();
                bool skippedFirst = false;

                for (int i = 0; i < tmp.Length; i++)
                {
                    if (!skippedFirst && tmp[i] == ' ')
                    {
                        underlining.Append(' ');
                        skippedFirst = true;
                        
                        continue;
                    }
                    
                    if (tmp[i] == ' ')
                    {
                        underlining.Append('_');
                    }
                    else 
                    {
                        underlining.Append(tmp[i]);
                    }
                        
                }

                tmp = underlining.ToString();
                pair = tmp.Split(' ');

                // When X_Y.wav encountered.
                //
                if (pair.Length == 1)
                {
                    entry.WavId = pair[0];

                    for (int i = 0; i < TrainingTargets.Speakers.Count; i++)
                    {
                        if (pair[0].Contains(TrainingTargets.Speakers[i]))
                        {
                            entry.Owner = TrainingTargets.Speakers[i];
                            break;
                        }
                    }
                }
                // When X Y.wav encountered.
                //
                else if (pair.Length == 2)
                {
                    entry.Owner = pair[0];

                    for (int i = 0; i < TrainingTargets.Speakers.Count; i++)
                    {
                        if (pair[0].Contains(TrainingTargets.Speakers[i]))
                        {
                            entry.WavId = TrainingTargets.Speakers[i] + "_" + pair[1]; ;
                            break;
                        }
                    }
                }
                // Can't handle, discard.
                //
                else
                {
                    Console.WriteLine("warning: dropped " + responseID + "...\n\treason: data could not be parsed.\n\tdirty: \'" + dirty + "\'");
                    ++EntriesFailedToParseCount;
                }

                result.Add(entry);
            }

            return result;
        }

        private List<TrainingEntry> GetAllTextResourcesProxy()
        {
            try
            {
                var result =  GetAllTextResources();

                float parsed = result.Count;
                float dropped = EntriesFailedToParseCount;
                int total = result.Count + EntriesFailedToParseCount;
                float failRatePercentage = dropped / parsed * 100;

                Console.WriteLine("==============");
                Console.WriteLine("info: text/dropped " + EntriesFailedToParseCount + " entries... [" + (int) failRatePercentage + "%]");
                Console.WriteLine("info: text/total left " + result.Count + " entries...");
                Console.WriteLine("==============");

                Thread.Sleep(2000);

                return result;
            }
            catch (Exception)
            {
                Console.WriteLine("error: critical error occured during text collection...\n\tterminating.");
                throw;
            }
        }
        private List<TrainingEntry> GetAllTextResources()
        {
            Console.WriteLine("info: started collecting text resources...");

            var result = new List<TrainingEntry>();

            List<string> urls = GetApiUrlsForTextResources();
            var client = new HttpClient();

            for (int i = 0; i < urls.Count; i++)
            {
                string response = client.GetStringAsync(urls[i]).Result;
                var responseId = Guid.NewGuid();

                result.AddRange(GetParsedEntries(response, responseId)); 
            }

            Console.WriteLine("info: finished.");

            return result;
        }

        private List<TrainingAudio> GetAllAudioResourcesProxy(List<TrainingEntry> entries)
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

        private List<TrainingAudio> GetAllAudioResources(List<TrainingEntry> entries)
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
                    Console.WriteLine("==============");
                    Console.WriteLine("info: downloading wav files...");

                    string targetLocation = string.Concat(saveDirPath, which, wavDir);
                    client.DownloadFile(fileNamePlusUrl.Value, string.Concat(targetLocation, fileName));

                    var trainingRecord = new TrainingAudio()
                    {
                        IsSavedLocally = true,
                        AbsolutePath = targetLocation + fileName,
                        ApiUrl = fileNamePlusUrl.Value
                    };

                    result.Add(trainingRecord);

                    Console.WriteLine("\tinfo: downloaded " + fileName + " to " + targetLocation);
                }
                catch (Exception)
                {
                    ++WavFilesDroppedCount;
                    Console.WriteLine("warning: dropped wav download... \n\tfile: " + fileName);

                    continue;
                }
            }

            Console.WriteLine("==============");
            Console.WriteLine("info: audio/dropped " + WavFilesDroppedCount + " entries...");
            Console.WriteLine("info: audio/total " + result.Count + " entries...");
            Console.WriteLine("==============");

            return result;
        }

        public VoiceTrainingData PullVoiceTrainingData()
        {
            Console.WriteLine("===oam==ost===");
            Console.WriteLine("Welcome to TF2 Training Dataset utility for Piper TTS!\n\nDataset will be saved to " + saveDirPath + ".");
            Console.WriteLine("==============");

            Thread.Sleep(2000);

            var dataset = new VoiceTrainingData();

            List<TrainingEntry> text = 

            dataset.TrainingTextEntries = GetAllTextResourcesProxy();
            dataset.TrainingAudioEntries = GetAllAudioResourcesProxy(dataset.TrainingTextEntries);

            return dataset;
        }

        [GeneratedRegex("\"url\":\\s*\"([^\"]+)\"")]
        private static partial Regex MyRegex();
    }
}