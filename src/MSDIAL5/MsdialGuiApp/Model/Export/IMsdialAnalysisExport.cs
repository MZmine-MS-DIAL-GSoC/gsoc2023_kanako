﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal interface IMsdialAnalysisExport {
        void Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel);
    }

    internal sealed class MsdialAnalysisExportModel : BindableBase, IMsdialAnalysisExport
    {
        private readonly IAnalysisExporter _exporter;

        public MsdialAnalysisExportModel(IAnalysisExporter exporter) {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        }

        public string Label { get; set; }
        public string FilePrefix { get; set; } = string.Empty;
        public string FileSuffix { get; set; } = "txt";

        public bool ShouldExport {
            get => _shouldExport;
            set => SetProperty(ref _shouldExport, value);
        }
        private bool _shouldExport = false;

        void IMsdialAnalysisExport.Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            if (!ShouldExport) {
                return;
            }
            var filename = Path.Combine(destinationFolder, $"{FilePrefix}_{fileBeanModel.AnalysisFileName}.{FileSuffix}");
            using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write)) {
                var features = ChromatogramPeakFeatureCollection.LoadAsync(fileBeanModel.PeakAreaBeanInformationFilePath).Result;
                _exporter.Export(stream, fileBeanModel.File, features);
            }
        }
    }

    internal sealed class SpectraTypeSelectableMsdialAnalysisExportModel : BindableBase, IMsdialAnalysisExport
    {
        private readonly IReadOnlyDictionary<ExportspectraType, IAnalysisExporter> _exporters;

        public SpectraTypeSelectableMsdialAnalysisExportModel(IReadOnlyDictionary<ExportspectraType, IAnalysisExporter> exporters) {
            _exporters = exporters ?? throw new ArgumentNullException(nameof(exporters));
            Types = exporters.Keys.ToList().AsReadOnly();
            SelectedType = Types.FirstOrDefault();
        }

        public ReadOnlyCollection<ExportspectraType> Types { get; }
        public ExportspectraType SelectedType {
            get => _selectedType;
            set => SetProperty(ref _selectedType, value);
        }
        private ExportspectraType _selectedType;

        public string Label { get; set; }
        public string FilePrefix { get; set; } = string.Empty;
        public string FileSuffix { get; set; } = "txt";

        public bool ShouldExport {
            get => _shouldExport;
            set => SetProperty(ref _shouldExport, value);
        }
        private bool _shouldExport = false;

        void IMsdialAnalysisExport.Export(string destinationFolder, AnalysisFileBeanModel fileBeanModel) {
            if (!ShouldExport) {
                return;
            }
            var filename = Path.Combine(destinationFolder, $"{FilePrefix}_{fileBeanModel.AnalysisFileName}.{FileSuffix}");
            using (var stream = File.Open(filename, FileMode.Create, FileAccess.Write)) {
                var features = ChromatogramPeakFeatureCollection.LoadAsync(fileBeanModel.PeakAreaBeanInformationFilePath).Result;
                _exporters[SelectedType].Export(stream, fileBeanModel.File, features);
            }
        }
    }
}
