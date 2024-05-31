namespace DatasetGen
{
    public static partial class MediaWiki
    {
        private const string apiBaseUrl                 = "https://wiki.teamfortress.com/w/api.php?";
        public static readonly string saveDirPath       = "tf2vo_ttstrain_dataset";
        public static readonly string wavDir            = "/wav/";

        private readonly struct TrainingCategories
        {
            public readonly static List<string> Types   =
            [
                 "Responses",
                 "Voice commands"
            ];
        }

        public readonly struct TrainingTargets
        {
            public readonly static List<string> Speakers =
            [
                 "Soldier",
                 "Pyro",
                 "Demoman",
                 "Heavy",
                 "Engineer",
                 "Medic",
                 "Sniper",
                 "Spy",
            ];
        }

        public static int EntriesFailedToParseCount     = 0;
        public static int AudioUrlsDroppedCount         = 0;
        public static int WavFilesDroppedCount          = 0;
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
    }
}