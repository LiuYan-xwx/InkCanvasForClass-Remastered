using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using InkCanvasForClass_Remastered.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using randomtest;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace InkCanvasForClass_Remastered
{
    /// <summary>
    /// Interaction logic for RandWindow.xaml
    /// </summary>
    public partial class RandWindow : Window
    {
        private ShuffleBag<string> ShuffleBag;
        public bool IsAutoClose = false;
        private List<string> NameList = [];

        private readonly SettingsService SettingsService;
        private readonly RandViewModel ViewModel;

        public Settings Settings => SettingsService.Settings;

        public RandWindow(SettingsService settingsService, RandViewModel viewModel)
        {
            InitializeComponent();
            SettingsService = settingsService;
            ViewModel = viewModel;
            DataContext = ViewModel;
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            ReloadNamesFromFile();
            ShuffleBag = new(NameList);
        }

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.DrawCount >= NameList.Count || ViewModel.DrawCount >= 10) return;
            ViewModel.DrawCount++;
            SymbolIconStart.Symbol = Symbol.People;
            BorderBtnAdd.Opacity = 1;
            BorderBtnMinus.Opacity = 1;
        }

        private void BorderBtnMinus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.DrawCount == 1)
            {
                SymbolIconStart.Symbol = Symbol.Contact;
                return;
            }
            ViewModel.DrawCount--;
        }

        private async void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (NameList.Count == 0)
            {
                MessageBox.Show("名单为空，请先导入名单！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (ViewModel.IsNoDuplicate && ViewModel.DrawCount > ShuffleBag.RemainingCount)
            {
                MessageBox.Show("没有足够的未被抽过的人！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<string> outputs = [];

            LabelOutput2.Visibility = Visibility.Collapsed;
            LabelOutput3.Visibility = Visibility.Collapsed;

            // 生成动画阶段
            await Task.Run(async () =>
            {
                // 动画持续时间（毫秒）
                int animationDuration = 600;
                // 名字切换间隔（毫秒）
                int switchInterval = 50;
                int elapsed = 0;

                while (elapsed < animationDuration)
                {
                    int animationIndex = Random.Shared.Next(0, NameList.Count);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LabelOutput.Content = NameList[animationIndex];
                    });
                    await Task.Delay(switchInterval);
                    elapsed += switchInterval;
                }
            });

            // 最终结果生成阶段
            if (ViewModel.IsNoDuplicate)
            {
                while (outputs.Count < ViewModel.DrawCount)
                {
                    outputs.Add(ShuffleBag.Next());
                }
            }
            else
            {
                while (outputs.Count < ViewModel.DrawCount)
                {
                    int i = RandomNumberGenerator.GetInt32(0, NameList.Count);
                    outputs.Add(NameList[i]);
                }
            }

            UpdateLabelOutputs(outputs);

            //if (IsAutoClose)
            //{
            //    await Task.Delay(RandDoneAutoCloseWaitTime);
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        PeopleControlPane.Opacity = 1;
            //        PeopleControlPane.IsHitTestVisible = true;
            //        Close();
            //    });
            //}
        }

        private void UpdateLabelOutputs(List<string> outputs)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ViewModel.DrawCount <= 5)
                {
                    LabelOutput.Content = string.Join(Environment.NewLine, outputs);
                }
                else if (ViewModel.DrawCount <= 10)
                {
                    LabelOutput2.Visibility = Visibility.Visible;
                    LabelOutput.Content = string.Join(Environment.NewLine, outputs.Take((outputs.Count + 1) / 2));
                    LabelOutput2.Content = string.Join(Environment.NewLine, outputs.Skip((outputs.Count + 1) / 2));
                }
                else
                {
                    LabelOutput2.Visibility = Visibility.Visible;
                    LabelOutput3.Visibility = Visibility.Visible;
                    LabelOutput.Content = string.Join(Environment.NewLine, outputs.Take((outputs.Count + 1) / 3));
                    LabelOutput2.Content = string.Join(Environment.NewLine, outputs.Skip((outputs.Count + 1) / 3).Take((outputs.Count + 1) / 3));
                    LabelOutput3.Content = string.Join(Environment.NewLine, outputs.Skip((outputs.Count + 1) * 2 / 3));
                }
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (NameList.Count == 0)
            {
                MessageBox.Show("名单为空，请先导入名单！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                BorderBtnHelp_MouseUp(null, null);
            }
            if (IsAutoClose)
            {
                PeopleControlPane.Opacity = 0.4;
                PeopleControlPane.IsHitTestVisible = false;

                BorderBtnRand_MouseUp(null, null);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Settings.RandWindowOnceCloseLatency) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    Close();
                };
                timer.Start();
            }
            ReloadNamesFromFile();
        }

        private void ReloadNamesFromFile()
        {
            NameList.Clear();
            if (File.Exists(App.AppRootFolderPath + "Names.txt"))
            {
                string[] nameArray = File.ReadAllLines(App.AppRootFolderPath + "Names.txt");
                NameList.AddRange(
                    from string s in nameArray
                    where s != "" // 过滤掉空行
                    select s);
                TextBlockPeopleCount.Text = NameList.Count.ToString();
                if (NameList.Count == 0)
                {
                    //TextBlockPeopleCount.Text = "点击此处以导入名单";
                    MessageBox.Show("名单为空，请先导入名单！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    BorderBtnHelp_MouseUp(null, null);
                }
            }
        }

        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            App.GetService<NamesInputWindow>().ShowDialog();
            ReloadNamesFromFile();
            ShuffleBag = new(NameList);
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}















