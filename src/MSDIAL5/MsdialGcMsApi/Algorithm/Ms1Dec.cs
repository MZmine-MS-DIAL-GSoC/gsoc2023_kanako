﻿using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public sealed class Ms1Dec {
        private readonly MsdialGcmsParameter _parameter;

        public Ms1Dec(MsdialGcmsParameter parameter) {
            _parameter = parameter;
        }

        public List<MSDecResult> GetMSDecResults(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, ReportProgress reporter) {
            return MSDecHandler.GetMSDecResults(spectrumList, chromPeakFeatures, _parameter, reporter.ReportAction, reporter.InitialProgress, reporter.ProgressMax);
        }
    }
}