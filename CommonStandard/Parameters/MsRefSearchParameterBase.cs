﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Parameter {
    [MessagePackObject]
    public class MsRefSearchParameterBase {
        [Key(0)]
        public float MassRangeBegin { get; set; } = 0;
        [Key(1)]
        public float MassRangeEnd { get; set; } = 2000;
        [Key(2)]
        public float RtTolerance { get; set; } = 100.0F;
        [Key(3)]
        public float RiTolerance { get; set; } = 100.0F;
        [Key(4)]
        public float CcsTolerance { get; set; } = 10.0F;
        [Key(5)]
        public float Ms1Tolerance { get; set; } = 0.01F;
        [Key(6)]
        public float Ms2Tolerance { get; set; } = 0.05F;
        [Key(7)]
        public float RelativeAmpCutoff { get; set; } = 0.1F;
        [Key(8)]
        public float AbsoluteAmpCutoff { get; set; } = 5;

        // by [0-1]
        [Key(9)]
        public float WeightedDotProductCutOff { get; set; } = 0.5F;
        [Key(10)]
        public float SimpleDotProductCutOff { get; set; } = 0.5F;
        [Key(11)]
        public float ReverseDotProductCutOff { get; set; } = 0.5F;
        [Key(12)]
        public float MatchedPeaksPercentageCutOff { get; set; } = 0.5F;
        [Key(13)]
        public float TotalScoreCutoff { get; set; } = 0.8F;

        // by absolute value
        [Key(14)]
        public float MinimumSpectrumMatch { get; set; } = 3;

        // option
        [Key(15)]
        public bool IsUseTimeForAnnotationFiltering { get; set; } = false;
        [Key(16)]
        public bool IsUseTimeForAnnotationScoring { get; set; } = false;
        [Key(17)]
        public bool IsUseCcsForAnnotationFiltering { get; set; } = false;
        [Key(18)]
        public bool IsUseCcsForAnnotationScoring { get; set; } = false;

    }
}
