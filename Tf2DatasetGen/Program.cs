﻿using System;

using static PiperTrainingCsvTf2Gen.TeamFortressMediaWiki;

namespace PiperTrainingCsvTf2Gen
{
    public class Program
    {
        static void Execute()
        {
            VoiceTrainingData dataset = new TeamFortressMediaWiki().PullVoiceTrainingData();
            FinalizeDatasetForPiper.PublishLocally(dataset);
        }

        static void Main()
        {
            Execute();
            Console.ReadKey();
        }
    }
}