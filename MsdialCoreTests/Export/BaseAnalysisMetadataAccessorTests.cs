﻿using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Export.Tests
{
    [TestClass()]
    public class BaseAnalysisMetadataAccessorTests
    {
        private static readonly string[] headers = new string[] {
            "Peak ID",
            "Name",
            "Scan",
            "Height",
            "Area",
            "Model masses",
            "Adduct",
            "Isotope",
            "Comment",
            "Formula",
            "Ontology",
            "InChIKey",
            "SMILES",
            "Annotation tag (VS1.0)",
            "MS/MS matched",
            "Simple dot product",
            "Weighted dot product",
            "Reverse dot product",
            "Matched peaks count",
            "Matched peaks percentage",
            "Total score",
            "S/N",
            "MS1 isotopes",
            "MSMS spectrum" };

        [TestMethod()]
        public void GetHeadersTest() {
            var accessor = new TestAnalysisMetadataAccessor(null, null, default);
            CollectionAssert.AreEqual(headers, accessor.GetHeaders());
        }

        [TestMethod()]
        public void GetContentTest() {
            var parameter = new ParameterBase { CentroidMs1Tolerance = 0.01f, MS2DataType = MSDataType.Centroid, };
            var refer = new MockRefer();
            var accessor = new TestAnalysisMetadataAccessor(refer, parameter, ExportspectraType.deconvoluted);
            var feature = new ChromatogramPeakFeature
            {
                MasterPeakID = 100,
                Name = "Metabolite",
                MS1RawSpectrumIdTop = 2,
                PeakHeightTop = 1000.1,
                PeakAreaAboveZero = 900.2,
                PrecursorMz = 700d,
                AdductType = AdductIonParser.GetAdductIonBean("[M+H]+"),
                Comment = "nice comment",
                MatchResults = new MsScanMatchResultContainer
                {
                    MatchResults = new List<MsScanMatchResult>
                    {
                        new MsScanMatchResult
                        {
                            IsSpectrumMatch = true,
                            SimpleDotProduct = 0.811f,
                            WeightedDotProduct = 0.724f,
                            ReverseDotProduct = 0.631f,
                            MatchedPeaksCount = 4.00f,
                            MatchedPeaksPercentage = 0.901f,
                            TotalScore = 0.638f,
                        },
                    },
                },
            };
            feature.PeakCharacter.IsotopeWeightNumber = 1;
            feature.PeakShape.SignalToNoise = 6.78f;
            var msdec = new MSDecResult
            {
                ModelMasses = new List<double> { 700.001, 600.2, 100.00001, },
                RawSpectrumID = 1,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 700.00001, Intensity = 1000, },
                    new SpectrumPeak { Mass = 500.00002, Intensity = 200, },
                    new SpectrumPeak { Mass = 200.00003, Intensity = 50, },
                },
            };
            var provider = new MockDataProvider();
            var content = accessor.GetContent(feature, msdec, provider);

            Assert.AreEqual("100", content["Peak ID"]);
            Assert.AreEqual("Metabolite", content["Name"]);
            Assert.AreEqual("2", content["Scan"]);
            Assert.AreEqual("1000", content["Height"]);
            Assert.AreEqual("900", content["Area"]);
            Assert.AreEqual("700.001 600.2 100.00001", content["Model masses"]);
            Assert.AreEqual("[M+H]+", content["Adduct"]);
            Assert.AreEqual("1", content["Isotope"]);
            Assert.AreEqual("nice comment", content["Comment"]);
            Assert.AreEqual("C600H12000O", content["Formula"]);
            Assert.AreEqual("ontology", content["Ontology"]);
            Assert.AreEqual("inchikey", content["InChIKey"]);
            Assert.AreEqual("smiles", content["SMILES"]);
            Assert.AreEqual(DataAccess.GetAnnotationCode(feature.MatchResults.Representative, parameter).ToString(), content["Annotation tag (VS1.0)"]);
            Assert.AreEqual("True", content["MS/MS matched"]);
            Assert.AreEqual("0.81", content["Simple dot product"]);
            Assert.AreEqual("0.72", content["Weighted dot product"]);
            Assert.AreEqual("0.63", content["Reverse dot product"]);
            Assert.AreEqual("4.00", content["Matched peaks count"]);
            Assert.AreEqual("0.90", content["Matched peaks percentage"]);
            Assert.AreEqual("0.64", content["Total score"]);
            Assert.AreEqual("6.78", content["S/N"]);
            Assert.AreEqual("700.00000 1000;701.00467 100", content["MS1 isotopes"]);
            Assert.AreEqual("700.00001 1000;500.00002 200;200.00003 50", content["MSMS spectrum"]);
        }

        class TestAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
        {
            public TestAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

            }
        }

        class MockDataProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                return new List<RawSpectrum>
                { 
                    new RawSpectrum
                    {
                        OriginalIndex = 2,
                        Spectrum = new[]
                        {
                            new RawPeakElement { Mz = 700f, Intensity = 1000f, },
                            new RawPeakElement { Mz = 701.00001f, Intensity = 100f, },
                        },
                    },
                    new RawSpectrum
                    {
                        Spectrum = new[]
                        {
                            new RawPeakElement { Mz = 200.00003f, Intensity = 50, },
                            new RawPeakElement { Mz = 500.00002f, Intensity = 200, },
                            new RawPeakElement { Mz = 700.00001f, Intensity = 1000, },
                        },
                    },

                }.AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                return Task.FromResult(LoadMs1Spectrums());
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                throw new System.NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new System.NotImplementedException();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                throw new System.NotImplementedException();
            }
        }

        class MockRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
        {
            public string Key => throw new System.NotImplementedException();

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference
                {
                    Formula = new Formula { FormulaString = "C600H12000O", },
                    Ontology = "ontology",
                    InChIKey = "inchikey",
                    SMILES = "smiles",
                };
            }
        }
    }
}