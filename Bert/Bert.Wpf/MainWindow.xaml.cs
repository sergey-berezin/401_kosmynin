using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using Bert.Nuget;
using Bert.Storage;
using Bert.Wpf.ViewModels;
using Microsoft.Win32;

namespace Bert.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {  
            InitializeComponent();

            var bertModelConnection = BertModelConnection.GetBertModelConnection();
            var storage = new JsonStorage();

            var viewModel = new AnswerViewModel(bertModelConnection, storage);
            DataContext = viewModel;
        }

    }
   
}   
