﻿using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialLcImMsApi.Parameter {
    public class MsdialLcImMsParameter : ParameterBase {
        public float DriftTimeBegin { get; set; } = 0;
        public float DriftTimeEnd { get; set; } = 2000;
        public float AccumulatedRtRagne { get; set; } = 0.2F;
        public bool IsAccumulateMS2Spectra { get; set; } = false;

        public float DriftTimeAlignmentTolerance { get; set; } = 0.02F; // msec
        public float DriftTimeAlignmentFactor { get; set; } = 0.5F;
        public IonMobilityType IonMobilityType { get; set; } = IonMobilityType.Tims;
        public bool IsAllCalibrantDataImported { get; set; } = false;

        public Dictionary<int, CoefficientsForCcsCalculation> FileID2CcsCoefficients { get; set; } = new Dictionary<int, CoefficientsForCcsCalculation>();

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();

            pStrings.Add("\r\n");
            pStrings.Add("# IMMS specific parameters");
            pStrings.Add(String.Join(": ", new string[] { "Drift time begin", DriftTimeBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Drift time end", DriftTimeEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Accumulated RT ragne", AccumulatedRtRagne.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Accumulate MS2 spectra", IsAccumulateMS2Spectra.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Drift time alignment tolerance", DriftTimeAlignmentTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Drift time alignment factor", DriftTimeAlignmentFactor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Ion mobility type", IonMobilityType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "All calibrant data imported", IsAllCalibrantDataImported.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# File ID CCS coefficients");
            foreach (var item in FileID2CcsCoefficients) {

                pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, String.Join(",", new string[] {
                    "Agilent IM=" + item.Value.IsAgilentIM, "Bruker IM=" + item.Value.IsBrukerIM, "Waters IM=" + item.Value.IsWatersIM,
                    "Agilent Beta=" + item.Value.AgilentBeta, "Agilent TFix=" + item.Value.AgilentTFix,
                    "Waters coefficient=" + item.Value.WatersCoefficient, "Waters T0=" + item.Value.WatersT0, "Waters exponent=" + item.Value.WatersExponent 
                }) }));
            }
            return pStrings;
        }
    }
}
