﻿using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class ProjectParameterSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public ProjectParameterSettingViewModel(ProjectParameterSettingModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            ProjectTitle = Model
                .ToReactivePropertyAsSynchronized(m => m.ProjectTitle)
                .SetValidateAttribute(() => ProjectTitle)
                .AddTo(Disposables);

            ProjectFolderPath = Model
                .ToReactivePropertyAsSynchronized(m => m.ProjectFolderPath)
                .SetValidateAttribute(() => ProjectFolderPath)
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                ProjectTitle.ObserveHasErrors,
                ProjectFolderPath.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim();

            ObserveChanges = new[]
            {
                ProjectTitle.ToUnit(),
                ProjectFolderPath.ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                decide.ToConstant(false),
                changes.ToConstant(true),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ProjectParameterSettingModel Model { get; }

        [Required(ErrorMessage = "Project title is required.")]
        [RegularExpression(@"[a-zA-Z0-9_\.\-]+", ErrorMessage = "Contains invalid characters.")]
        public ReactiveProperty<string> ProjectTitle { get; }

        [Required(ErrorMessage = "Project folder path is required.")]
        [PathExists(IsDirectory = true, ErrorMessage = "Project folder must exist.")]
        public ReactiveProperty<string> ProjectFolderPath { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        private readonly Subject<Unit> decide;

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public IObservable<Unit> ObserveChanges { get; }

        public void Next() {
            Model.Build();
            decide.OnNext(Unit.Default);
        }
    }
}
