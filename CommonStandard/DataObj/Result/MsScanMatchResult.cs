﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj.Result {
    [MessagePackObject]
    public class MsScanMatchResult {
        // basic annotated information
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public string InChIKey { get; set; }

        [Key(2)]
        public float TotalSimilarity { get; set; }

        // spectral similarity
        [Key(3)]
        public float WeightedDotProduct { get; set; }
        [Key(4)]
        public float SimpleDotProduct { get; set; }
        [Key(5)]
        public float ReverseDotProduct { get; set; }
        [Key(6)]
        public float MatchedPeaksCount { get; set; }
        [Key(7)]
        public float MatchedPeaksPercentage { get; set; }
        [Key(8)]
        public float EssentialFragmentMatchedScore { get; set; }

        // others
        [Key(9)]
        public float RtSimilarity { get; set; }
        [Key(10)]
        public float RiSimilarity { get; set; }
        [Key(11)]
        public float CcsSimilarity { get; set; }
        [Key(12)]
        public float IsotopeSimilarity { get; set; }
        [Key(13)]
        public float AcurateMassSimilarity { get; set; }

        // Link to database
        [Key(14)]
        public int LibraryID { get; set; } = -1;

        // Checker
        [Key(15)]
        public bool IsMs1Match { get; set; }
        [Key(16)]
        public bool IsMs2Match { get; set; }
        [Key(17)]
        public bool IsRtMatch { get; set; }
        [Key(23)]
        public bool IsRiMatch { get; set; }
        [Key(18)]
        public bool IsCcsMatch { get; set; }
        [Key(19)]
        public bool IsLipidClassMatch { get; set; }
        [Key(20)]
        public bool IsLipidChainsMatch { get; set; }
        [Key(21)]
        public bool IsLipidPositionMatch { get; set; }
        [Key(22)]
        public bool IsOtherLipidMatch { get; set; }
    }

}