﻿using static DatasetGen.MediaWiki;

namespace DatasetGen
{
    public class Program
    {
        static void Main()
        {
            ProcessGen.IntegrityCheck(GetDataset());
        }
    }
}