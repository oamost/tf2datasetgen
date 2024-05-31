namespace DatasetGen
{
    // Partial class dispatching data retrieval.
    //
    public static partial class MediaWiki
    {
        public static Dataset GetDataset()
        {
            var dataset = new Dataset();

            dataset.SubscriptEntries = GetAllTextResourcesProxy();
            dataset.AudioResourceEntries = GetAllAudioResourcesProxy(dataset.SubscriptEntries);

            return dataset;
        }
    }
}