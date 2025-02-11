﻿using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CompMs.App.Msdial.View.Dims
{
    /// <summary>
    /// Interaction logic for Footer.xaml
    /// </summary>
    public partial class Footer : UserControl
    {
        public static readonly DependencyProperty ChromAxisProperty =
            DependencyProperty.Register(nameof(ChromAxis), typeof(IAxisManager), typeof(Footer));
        public IAxisManager ChromAxis {
            get => (IAxisManager)GetValue(ChromAxisProperty);
            set => SetValue(ChromAxisProperty, value);
        }

        public static readonly DependencyProperty MassAxisProperty =
            DependencyProperty.Register(nameof(MassAxis), typeof(IAxisManager), typeof(Footer));
        public IAxisManager MassAxis {
            get => (IAxisManager)GetValue(MassAxisProperty);
            set => SetValue(MassAxisProperty, value);
        }

        public Footer() {
            InitializeComponent();
        }
    }
}
