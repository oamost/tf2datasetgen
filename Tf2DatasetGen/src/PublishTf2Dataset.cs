using System;
using System.IO;
using System.Text;

using static PiperTrainingCsvTf2Gen.TeamFortressMediaWiki;

namespace PiperTrainingCsvTf2Gen
{
    public static class FinalizeDatasetForPiper
    {
        private static readonly string output = "\\metadata.csv";      

        private static bool IsValidWavRow(string fileName, string speaker)
        {
            bool result = false;
            string path = TeamFortressMediaWiki.saveDirPath + speaker + "\\" + "wav\\" + fileName + ".wav";
            bool assert = File.Exists(path);

            if (assert)
                result = true;

            return result;
        }

        public static void PublishLocally(VoiceTrainingData dataset)
        {
            // Write csv as => id|text (transscript).
            // Then check csv <=> sample integrity.
            //
            if (!Directory.Exists(TeamFortressMediaWiki.saveDirPath) ||
                                  dataset.TrainingAudioEntries.Count == 0 ||
                                  dataset.TrainingTextEntries.Count == 0)
            {
                Console.WriteLine("error: requirements are not met, terminating...");
                throw new InvalidOperationException();
            }

            Console.WriteLine("==============");
            Console.WriteLine("info: generating csv files...");            

            for (int i = 0; i < dataset.TrainingTextEntries.Count; i++)
            {
                if (dataset.TrainingTextEntries[i].WavId == null)
                    continue;

                string which =    (!dataset.TrainingTextEntries[i].WavId.Contains("Cm_"))
                                ? ("\\" + dataset.TrainingTextEntries[i].WavId.Split('_')[0].ToLower())
                                : ("\\" + dataset.TrainingTextEntries[i].WavId.Split('_')[1].ToLower());

                // Inconsistency...
                //
                if (which == "\\demo")
                    which = "demoman";
                if (which == "\\engie")
                    which = "engineer";
                if (which == "\\admin")
                    which = "administrator";
                if (which.ToLower().Contains("your_team_cm_admin"))
                    which = "administrator";

                string target = TeamFortressMediaWiki.saveDirPath + which + output;

                try
                {
                    if (!File.Exists(target))
                    {
                        FileStream fileStream = File.Create(target);
                        fileStream.Dispose();

                        Console.WriteLine("info: generated " + target);
                    }

                    string wavized = dataset.TrainingTextEntries[i].WavId.Replace(".wav", string.Empty);

                    string row = wavized +
                                 "|" +
                                 dataset.TrainingTextEntries[i].TransScript +
                                 "\n";

                    // Only append this to file if the exact wav really existst.
                    //
                    if (IsValidWavRow(wavized, which))
                        File.AppendAllText(target, row);
                }
                catch (Exception e) 
                { 
                }                
            }

            Console.WriteLine("==============");
            Console.WriteLine("info: successfully generated training dataset...");
            Console.WriteLine("===oam==ost===");
        }
    }
}