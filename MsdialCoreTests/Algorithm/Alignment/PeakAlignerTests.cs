﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialCore.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class PeakAlignerTests
    {
        [TestMethod()]
        public void AlignmentTest() {
            var accessor = new MockAccessor(
                new List<List<IMSScanProperty>>
                {
                    new List<IMSScanProperty>
                    {
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(0), Mass = 100, ChromXsLeft = new ChromXs(0), ChromXsTop = new ChromXs(0), ChromXsRight = new ChromXs(0) },
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(0), Mass = 200, ChromXsLeft = new ChromXs(0), ChromXsTop = new ChromXs(0), ChromXsRight = new ChromXs(0) },
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(0), Mass = 300, ChromXsLeft = new ChromXs(0), ChromXsTop = new ChromXs(0), ChromXsRight = new ChromXs(0) },
                    },
                    new List<IMSScanProperty>
                    {
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(0.5), Mass = 100, ChromXsLeft = new ChromXs(0.5), ChromXsTop = new ChromXs(0.5), ChromXsRight = new ChromXs(0.5) },
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(1.0), Mass = 200, ChromXsLeft = new ChromXs(1.0), ChromXsTop = new ChromXs(1.0), ChromXsRight = new ChromXs(1.0) },
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(1.5), Mass = 300, ChromXsLeft = new ChromXs(1.5), ChromXsTop = new ChromXs(1.5), ChromXsRight = new ChromXs(1.5) },
                    },
                    new List<IMSScanProperty>
                    {
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(1.0), Mass = 200, ChromXsLeft = new ChromXs(1.0), ChromXsTop = new ChromXs(1.0), ChromXsRight = new ChromXs(1.0) },
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(2.0), Mass = 200, ChromXsLeft = new ChromXs(2.0), ChromXsTop = new ChromXs(2.0), ChromXsRight = new ChromXs(2.0) },
                        new ChromatogramPeakFeature{ ChromXs = new ChromXs(3.0), Mass = 200, ChromXsLeft = new ChromXs(3.0), ChromXsTop = new ChromXs(3.0), ChromXsRight = new ChromXs(3.0) },
                    },
                }
            );

            var aligner = CreateAligner(accessor);

            var container = aligner.Alignment(
                new List<AnalysisFileBean>
                {
                    new AnalysisFileBean { AnalysisFileId = 0 },
                    new AnalysisFileBean { AnalysisFileId = 1 },
                    new AnalysisFileBean { AnalysisFileId = 2 },
                    new AnalysisFileBean { AnalysisFileId = 3 },
                },
                new AlignmentFileBean { FileID = 0 },
                null
            );

            Assert.AreEqual(7, container.TotalAlignmentSpotCount);

            Assert.AreEqual(0.25, container.AlignmentSpotProperties[0].TimesCenter.Value);
            Assert.AreEqual(0, container.AlignmentSpotProperties[1].TimesCenter.Value);
            Assert.AreEqual(0, container.AlignmentSpotProperties[2].TimesCenter.Value);
            Assert.AreEqual(1, container.AlignmentSpotProperties[3].TimesCenter.Value);
            Assert.AreEqual(1.5, container.AlignmentSpotProperties[4].TimesCenter.Value);
            Assert.AreEqual(2.0, container.AlignmentSpotProperties[5].TimesCenter.Value);
            Assert.AreEqual(3.0, container.AlignmentSpotProperties[6].TimesCenter.Value);

            Assert.AreEqual(100, container.AlignmentSpotProperties[0].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[1].MassCenter);
            Assert.AreEqual(300, container.AlignmentSpotProperties[2].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[3].MassCenter);
            Assert.AreEqual(300, container.AlignmentSpotProperties[4].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[5].MassCenter);
            Assert.AreEqual(200, container.AlignmentSpotProperties[6].MassCenter);
        }

        PeakAligner CreateAligner(DataAccessor accessor) {
            var parameter = new MockParameter();
            var iupac = new Common.DataObj.Database.IupacDatabase();
            var joiner = new MockJoiner();
            var filler = new MockFiller(parameter);
            var refiner = new MockRefiner();
            return new PeakAligner(accessor, joiner, filler, refiner, parameter, iupac);
        }
    }

    class MockAccessor : DataAccessor
    {
        private List<List<IMSScanProperty>> scans;
        public MockAccessor(List<List<IMSScanProperty>> scans) { this.scans = scans; }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            if (analysisFile.AnalysisFileId < scans.Count)
                return scans[analysisFile.AnalysisFileId];
            return new List<IMSScanProperty>();
        }
    }

    class MockJoiner : PeakJoiner
    {
        protected override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.ChromXs.Value - y.ChromXs.Value) < 1 && Math.Abs(x.PrecursorMz - y.PrecursorMz) < 1;
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return 1 - Math.Abs(x.ChromXs.Value - y.ChromXs.Value) / 100 / 2 - Math.Abs(x.PrecursorMz - y.PrecursorMz) / 2;
        }
    }

    class MockFiller : GapFiller
    {
        public MockFiller(ParameterBase param) : base(param) {
        }

        protected override double AxTol => 0.5;

        protected override double GetAveragePeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Average(peak => peak.PeakWidth(ChromXType.RT));
        }

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.Value))
            {
                Mz = new MzValue(peaks.Average(peak => peak.Mass)),
            };
        }

        protected override List<ChromatogramPeak> GetPeaks(List<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            return new List<ChromatogramPeak> {
                new ChromatogramPeak(0, 50, 100, new RetentionTime(0.5)),
                new ChromatogramPeak(0, 70, 120, new RetentionTime(1)),
            };
        }
    }

    class MockRefiner : AlignmentRefiner
    {
        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments) {
            return alignments;
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) {
            return;
        }
    }

    class MockParameter : ParameterBase
    {

    }
}