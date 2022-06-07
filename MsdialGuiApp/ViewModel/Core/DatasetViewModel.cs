﻿using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Lcimms;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal sealed class DatasetViewModel : ViewModelBase
    {
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;
        private readonly IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService;
        private readonly IMessageBroker _messageBroker;

        public DatasetViewModel(
            IDatasetModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService,
            IMessageBroker messageBroker) {
            Model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;
            this.analysisFilePropertyResetService = analysisFilePropertyResetService;
            _messageBroker = messageBroker;
            MethodViewModel = model.ObserveProperty(m => m.Method)
                .Select(ConvertToViewModel)
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            FilePropertyResetCommand = new ReactiveCommand()
                .WithSubscribe(FilePropertyResetting)
                .AddTo(Disposables);
        }

        public IDatasetModel Model { get; }

        public ReadOnlyReactivePropertySlim<MethodViewModel> MethodViewModel { get; }

        public ReactiveCommand FilePropertyResetCommand { get; }

        private void FilePropertyResetting() {
            using (var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetViewModel(Model.AnalysisFilePropertySetModel)) {
                var afpsw_result = analysisFilePropertyResetService.ShowDialog(analysisFilePropertySetWindowVM);
                if (afpsw_result == true) {
                    Model.AnalysisFilePropertyUpdate();
                }
            }
        }

        private MethodViewModel ConvertToViewModel(IMethodModel model) {
            switch (model) {
                case LcmsMethodModel lc:
                    return LcmsMethodVM.Create(lc, compoundSearchService, peakSpotTableService, proteomicsTableService, _messageBroker);
                case ImmsMethodModel im:
                    return ImmsMethodVM.Create(im, compoundSearchService, peakSpotTableService, _messageBroker);
                case DimsMethodModel di:
                    return DimsMethodVM.Create(di, compoundSearchService, peakSpotTableService, _messageBroker);
                case LcimmsMethodModel lcim:
                    return LcimmsMethodVM.Create(lcim, compoundSearchService, peakSpotTableService);
                // case GcmsMethodModel _:
                default:
                    return null;
            }
        }
    }
}
