using System.Text;
using System.Text.RegularExpressions;

namespace DatasetGen
{
    // Partial class handling text resources.
    //
    public static partial class MediaWiki
    {
        private static List<SubscriptEntry> GetParsedEntries(string rawData, Guid responseID)
        {
            var result = new List<SubscriptEntry>();

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

                var entry = new SubscriptEntry();

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

                    for (int i = 0; i < Speakers.Entities.Count; i++)
                    {
                        if (pair[0].Contains(Speakers.Entities[i]))
                        {
                            entry.Owner = Speakers.Entities[i];
                            break;
                        }
                    }
                }
                // When X Y.wav encountered.
                //
                else if (pair.Length == 2)
                {
                    entry.Owner = pair[0];

                    for (int i = 0; i < Speakers.Entities.Count; i++)
                    {
                        if (pair[0].Contains(Speakers.Entities[i]))
                        {
                            entry.WavId = Speakers.Entities[i] + "_" + pair[1]; ;
                            break;
                        }
                    }
                }
                // Can't handle, discard.
                //
                else
                {
                    Console.WriteLine("warning: dropped response: " + responseID + "...\n\treason: data could not be parsed.\n\tdirty: \'" + dirty + "\'");
                    ++EntriesFailedToParseCount;
                }

                result.Add(entry);
            }

            return result;
        }

        private static List<SubscriptEntry> GetAllTextResourcesProxy()
        {
            try
            {
                return  GetAllTextResources();
            }
            catch (Exception)
            {
                Console.WriteLine("error: critical error occured during text collection...\n\tterminating.");
                throw;
            }
        }
        private static List<SubscriptEntry> GetAllTextResources()
        {
            var result = new List<SubscriptEntry>();

            List<string> urls = GetApiUrlsForTextResources();
            var client = new HttpClient();

            for (int i = 0; i < urls.Count; i++)
            {
                string response = client.GetStringAsync(urls[i]).Result;
                var responseId = Guid.NewGuid();

                result.AddRange(GetParsedEntries(response, responseId)); 
            }

            return result;
        }
    }
}
