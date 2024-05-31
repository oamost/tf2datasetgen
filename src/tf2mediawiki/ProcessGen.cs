using System.Linq;

using static DatasetGen.MediaWiki;

namespace DatasetGen
{
    // Class dealing with integrity check.
    //
    public static class ProcessGen
    {
        private static readonly string output = "/metadata.csv";      

        private static bool IsValidWavRow(string fileName, string speaker)
        {
            bool result = false;
            string path = MediaWiki.saveDirPath + speaker + "/" + "wav/" + fileName + ".wav";
            bool assert = File.Exists(path);

            if (assert)
                result = true;

            return result;
        }

        public static void IntegrityCheck(Dataset dataset)
        {
            // Write csv as => id|text (transscript).
            // Then check csv <=> sample integrity.
            //
            if (!Directory.Exists(MediaWiki.saveDirPath) ||
                                  dataset.AudioResourceEntries.Count == 0 ||
                                  dataset.SubscriptEntries.Count == 0)
            {
                Console.WriteLine("error: requirements are not met, terminating...");
                throw new InvalidOperationException();
            }      

            for (int i = 0; i < dataset.SubscriptEntries.Count; i++)
            {
                if (dataset.SubscriptEntries[i].WavId == null)
                    continue;

                string which =    (!dataset.SubscriptEntries[i].WavId.Contains("Cm_"))
                                ? ("/" + dataset.SubscriptEntries[i].WavId.Split('_')[0].ToLower())
                                : ("/" + dataset.SubscriptEntries[i].WavId.Split('_')[1].ToLower());

                // Inconsistency...
                //
                if (which == "/demo")
                    which = "demoman";
                if (which == "/engie")
                    which = "engineer";
                if (which == "/admin")
                    which = "administrator";
                if (which.ToLower().Contains("your_team_cm_admin"))
                    which = "administrator";

                string target = MediaWiki.saveDirPath + which + output;

                try
                {
                    if (!File.Exists(target))
                    {
                        FileStream fileStream = File.Create(target);
                        fileStream.Dispose();

                        Console.WriteLine("info: generated " + target);
                    }

                    string wavized = dataset.SubscriptEntries[i].WavId.Replace(".wav", string.Empty);

                    string row = wavized +
                                 "|" +
                                 dataset.SubscriptEntries[i].TransScript +
                                 "\n";

                    // Only append this to file if the exact wav really existst.
                    //
                    if (IsValidWavRow(wavized, which)  
                        && dataset.AudioResourceEntries
                                  .Where(x => x.ForeignKeyAsSubscriptEntryId == dataset.SubscriptEntries[i].Id)
                                  .ToList()
                                  .Count == 1) File.AppendAllText(target, row);
                }
                catch (Exception) {}
            }
        }
    }
}