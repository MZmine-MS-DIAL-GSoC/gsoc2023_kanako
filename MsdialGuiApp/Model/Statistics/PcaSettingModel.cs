﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Mathematics.Statistics;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class PcaSettingModel : BindableBase
    {
        private readonly ParameterBase _parameter;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public PcaSettingModel(ParameterBase parameter, ObservableCollection<AlignmentSpotPropertyModel> spotprops, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            _parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            _spotprops = spotprops ?? throw new System.ArgumentNullException(nameof(spotprops));
            _evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            maxPcNumber = 5;
        }

        public int MaxPcNumber {
            get => maxPcNumber;
            set => SetProperty(ref maxPcNumber, value);
        }
        private int maxPcNumber;
        public bool IsIdentifiedImportedInStatistics {
            get => isIdentifiedImportedInStatistics;
            set => SetProperty(ref isIdentifiedImportedInStatistics, value);
        }
        private bool isIdentifiedImportedInStatistics;

        public bool IsAnnotatedImportedInStatistics {
            get => isAnnotatedImportedInStatistics;
            set => SetProperty(ref isAnnotatedImportedInStatistics, value);
        }
        private bool isAnnotatedImportedInStatistics;

        public bool IsUnknownImportedInStatistics {
            get => isUnknownImportedInStatistics;
            set => SetProperty(ref isUnknownImportedInStatistics, value);
        }
        private bool isUnknownImportedInStatistics;

        public PcaResultModel PcaResultModel {
            get => _pcaResultModel;
            set => SetProperty(ref _pcaResultModel, value);
        }
        private PcaResultModel _pcaResultModel;

        public void RunPca() {
            //private Dictionary<int, string> ColumnIndex_MetaboliteName { get; set; } = null;

            int counterSample = 0;
            int counterMetabolite = 0;

            var metaboliteIDs = new ObservableCollection<int>();
            var metaboliteSpotProps = new ObservableCollection<AlignmentSpotPropertyModel>();

            foreach (var spot in _spotprops) {
                if (isIdentifiedImportedInStatistics && _evaluator.IsReferenceMatched(spot.ScanMatchResult)) {
                    metaboliteIDs.Add(spot.MasterAlignmentID);
                    metaboliteSpotProps.Add(spot);
                }
                if (isAnnotatedImportedInStatistics && _evaluator.IsAnnotationSuggested(spot.ScanMatchResult)) {
                    metaboliteIDs.Add(spot.MasterAlignmentID);
                    metaboliteSpotProps.Add(spot);
                }
                if (isUnknownImportedInStatistics && !_evaluator.IsReferenceMatched(spot.ScanMatchResult) && !_evaluator.IsAnnotationSuggested(spot.ScanMatchResult)) {
                    metaboliteIDs.Add(spot.MasterAlignmentID);
                    metaboliteSpotProps.Add(spot);
                }
            }

            var statObj = new StatisticsObject()
            {
                //XDataMatrix = new double[_spotprops.Count, _parameter.FileID_AnalysisFileType.Keys.Count],
                XScaled = new double[_parameter.FileID_AnalysisFileType.Keys.Count, metaboliteSpotProps.Count],
                XLabels = new ObservableCollection<string>(metaboliteSpotProps.Select(prop => $@"ID: {prop.MasterAlignmentID}_{(string.IsNullOrEmpty(prop.Name) ? "Unknown" : prop.Name)}"))
            };

            for (int i = 0; i < _parameter.FileID_AnalysisFileType.Keys.Count; i++) {
                counterMetabolite = 0;
                for (int j = 0; j < metaboliteSpotProps.Count; j++) {
                    if (!metaboliteIDs.Contains(metaboliteSpotProps[j].MasterAlignmentID)) continue;
                    var alignProp = metaboliteSpotProps[j].AlignedPeakProperties;
                    statObj.XScaled[counterSample, counterMetabolite] = alignProp[i].NormalizedPeakHeight;
                    counterMetabolite++;
                }
                counterSample++;
            }

            //for (int i = 0; i < _spotprops.Count; i++) {
            //    var alignProp = _spotprops[i].AlignedPeakProperties;
            //    counterMetabolite = 0;
            //    for (int j = 0; j < _parameter.FileID_AnalysisFileType.Keys.Count; j++) {
            //        //counterMetabolite = 0;
            //        //Console.WriteLine("CounterSample" + counterSample);
            //        //Console.WriteLine("CounterMetabolite" + counterMetabolite);
            //        statObj.XDataMatrix[counterSample, counterMetabolite] = alignProp[j].NormalizedPeakHeight;
            //        //counterSample++;
            //    }                
            //    counterSample++;
            //}

            //foreach (var spot in _spotprops) {
            //    foreach(var peak in spot.AlignedPeakProperties) {
            //        statObj.XDataMatrix[peak.FileID, spot.MasterAlignmentID] = peak.NormalizedPeakHeight;
            //    }
            //}

            var pcaResult = StatisticsMathematics.PrincipalComponentAnalysis(statObj, MultivariateAnalysisOption.Pca, MaxPcNumber);
            PcaResultModel = new PcaResultModel(pcaResult, _parameter, metaboliteSpotProps);
        }
    }
}
