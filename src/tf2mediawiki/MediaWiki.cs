namespace DatasetGen
{
    public static partial class MediaWiki
    {
        public static VoiceTrainingData GetDataset()
        {
            var dataset = new VoiceTrainingData();

            dataset.TrainingTextEntries = GetAllTextResourcesProxy();
            dataset.TrainingAudioEntries = GetAllAudioResourcesProxy(dataset.TrainingTextEntries);

            return dataset;
        }
    }
}