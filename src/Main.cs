﻿using static DatasetGen.MediaWiki;

namespace DatasetGen
{
    // Program main entry point.
    //
    public class Program
    {
        static void Main()
        {
            ProcessGen.IntegrityCheck(GetDataset());
        }
    }
}