﻿using System;
using System.Windows;

namespace CompMs.CommonMVVM.WindowService
{
    public class DialogService<TView, TViewModel> : IWindowService<TViewModel> where TView : Window, new() {
        public DialogService() {

        }

        public DialogService(Window owner) {
            Owner = owner;
        }

        public Window Owner { get; }

        public WindowStartupLocation WindowStartupLocation { get; set; } = WindowStartupLocation.CenterOwner;

        public void Show(TViewModel viewmodel) {
            var dialog = new TView()
            {
                Owner = Owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = viewmodel,
            };

            dialog.Show();
        }

        public bool? ShowDialog(TViewModel viewmodel) {
            var dialog = new TView()
            {
                Owner = Owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DataContext = viewmodel,
            };

            return dialog.ShowDialog();
        }
    }
}
