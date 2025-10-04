using InkCanvasForClass_Remastered.Helpers;
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
        public NamesInputWindow(NamesInputViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
        }

        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(App.AppRootFolderPath + "Names.txt"))
            {
                string names = File.ReadAllText(App.AppRootFolderPath + "Names.txt");
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
                    File.WriteAllText(App.AppRootFolderPath + "Names.txt", ViewModel.NameText);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
