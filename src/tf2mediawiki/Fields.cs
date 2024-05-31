namespace DatasetGen
{
    // Partial class handling fields.
    //
    public static partial class MediaWiki
    {
        private const string apiBaseUrl                 = "https://wiki.teamfortress.com/w/api.php?";
        public static readonly string saveDirPath       = "../dataset";
        public static readonly string wavDir            = "/wav/";

        private readonly struct EntrySubCategories
        {
            public readonly static List<string> Types   =
            [
                 "Responses",
                 "Voice commands"
            ];
        }

        public readonly struct Speakers
        {
            public readonly static List<string> Entities =
            [
                 "Soldier",
                 "Pyro",
                 "Demoman",
                 "Heavy",
                 "Engineer",
                 "Medic",
                 "Sniper",
                 "Spy",
                 "Scout"
            ];
        }

        public static int EntriesFailedToParseCount     = 0;
        public static int AudioUrlsDroppedCount         = 0;
        public static int WavFilesDroppedCount          = 0;
        public class SubscriptEntry
        {
            public string? Owner;
            public string? WavId;
            public string? TransScript;
            public Guid Id;
        }

        public struct AudioResourceEntry
        {
            public string AbsolutePath;
            public string ApiUrl;
            public bool   IsSavedLocally;
            public Guid ForeignKeyAsSubscriptEntryId;
        }

        public struct Dataset
        {
            public List<SubscriptEntry> SubscriptEntries;
            public List<AudioResourceEntry> AudioResourceEntries;
        }
    }
}