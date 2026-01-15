using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.ViewModels;
using System.IO;
using System.Windows;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace InkCanvasForClass_Remastered
{
    /// <summary>
    /// Interaction logic for NamesInputWindow.xaml
    /// </summary>
    public partial class NamesInputWindow : Window
    {
        private readonly NamesInputViewModel ViewModel;
        private string _originText = string.Empty;
        private readonly string _path = Path.Combine(CommonDirectories.AppRootFolderPath, "Names.txt");
        public NamesInputWindow(NamesInputViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
        }

        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_path))
            {
                string names = File.ReadAllText(_path);
                ViewModel.NameText = names;
                _originText = names;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ViewModel.NameText != _originText)
            {
                if (MessageBox.Show("是否保存？", "名单导入", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    File.WriteAllText(_path, ViewModel.NameText);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
