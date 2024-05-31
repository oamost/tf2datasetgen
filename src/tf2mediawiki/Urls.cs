using System.Text;
using System.Text.RegularExpressions;

namespace DatasetGen
{
    public static partial class MediaWiki
    {
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

        private static int whereWasI = 0;
        private static string GetQueryStringForBatch(List<TrainingEntry> entries, out bool terminateQuery, int limit = 25)
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
        
        [GeneratedRegex("\"url\":\\s*\"([^\"]+)\"")]
        private static partial Regex MyRegex();
        private static Dictionary<string,string> GetApiUrlsForAudioResources(List<TrainingEntry> entries)
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

            return result;
        }
    }
}