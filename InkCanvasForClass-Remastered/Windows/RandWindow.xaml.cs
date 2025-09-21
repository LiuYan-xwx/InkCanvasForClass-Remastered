using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using InkCanvasForClass_Remastered.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace InkCanvasForClass_Remastered
{
    /// <summary>
    /// Interaction logic for RandWindow.xaml
    /// </summary>
    public partial class RandWindow : Window
    {
        private HashSet<int> drawnIndices = new HashSet<int>();
        
        // 使用静态加密安全随机数生成器实例，提供最高质量的随机性
        private static readonly RandomNumberGenerator _secureRng = RandomNumberGenerator.Create();
        
        // 用于动画效果的快速随机数生成器
        private static readonly Random _animationRng = new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode());

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
            RandDoneAutoCloseWaitTime = (int)Settings.RandWindowOnceCloseLatency * 1000;
        }

        public static int randSeed = 0;
        public bool IsAutoClose = false;
        public bool IsNotRepeatName = false;

        public int PeopleCount = 60;
        public List<string> NameList = [];

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.DrawCount >= PeopleCount || ViewModel.DrawCount >= 10) return;
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

        public int RandAnimationTimes = 100;
        public int RandAnimationInterval = 5;
        public int RandMaxPeopleOneTime = 10;
        public int RandDoneAutoCloseWaitTime = 2500;

        private async void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CheckBoxNotRepeatName.IsChecked == true && drawnIndices.Count + ViewModel.DrawCount > PeopleCount)
            {
                MessageBox.Show("没有足够的未被抽过的人！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<string> outputs = [];

            LabelOutput2.Visibility = Visibility.Collapsed;
            LabelOutput3.Visibility = Visibility.Collapsed;

            // 异步动画阶段 - 使用快速随机数生成器创建视觉效果
            await Task.Run(async () =>
            {
                for (int i = 0; i < RandAnimationTimes; i++)
                {
                    // 为动画使用快速随机数，增加视觉随机性
                    int animationIndex = GetFastRandomNumber(0, PeopleCount);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (NameList.Count != 0)
                        {
                            LabelOutput.Content = NameList[animationIndex];
                        }
                        else
                        {
                            LabelOutput.Content = (animationIndex + 1).ToString();
                        }
                    });
                    await Task.Delay(Math.Max(1, RandAnimationInterval));
                }
            });

            // 最终结果生成阶段 - 使用加密级随机数确保真正的随机性
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 生成高质量随机结果
                var finalResults = GenerateSecureRandomResults();
                
                foreach (var result in finalResults)
                {
                    if (NameList.Count != 0)
                    {
                        outputs.Add(NameList[result]);
                    }
                    else
                    {
                        outputs.Add((result + 1).ToString());
                    }
                }

                UpdateLabelOutputs(outputs);
                
                if (IsAutoClose)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(RandDoneAutoCloseWaitTime);
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            PeopleControlPane.Opacity = 1;
                            PeopleControlPane.IsHitTestVisible = true;
                            Close();
                        });
                    });
                }
            });
        }

        /// <summary>
        /// 生成加密安全的最终随机结果
        /// 使用多种熵源和高级算法确保真正的随机性
        /// </summary>
        /// <returns>随机选中的索引列表</returns>
        private List<int> GenerateSecureRandomResults()
        {
            var results = new List<int>();
            var attempts = 0;
            int maxAttempts = PeopleCount * 10; // 防止无限循环

            while (results.Count < ViewModel.DrawCount && attempts < maxAttempts)
            {
                attempts++;
                
                // 使用高质量随机数生成器
                int randomIndex = GetCryptographicallySecureRandomNumber(0, PeopleCount);
                
                // 检查是否需要避免重复
                if (CheckBoxNotRepeatName.IsChecked == true)
                {
                    if (drawnIndices.Contains(randomIndex))
                    {
                        continue; // 如果已经抽过，重新生成
                    }
                    drawnIndices.Add(randomIndex);
                }
                
                results.Add(randomIndex);
            }

            return results;
        }

        /// <summary>
        /// 生成加密安全的随机数
        /// 使用拒绝采样避免模运算偏差，确保真正的均匀分布
        /// </summary>
        /// <param name="minValue">最小值（包含）</param>
        /// <param name="maxValue">最大值（不包含）</param>
        /// <returns>高质量随机整数</returns>
        private int GetCryptographicallySecureRandomNumber(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentException($"maxValue ({maxValue}) must be greater than minValue ({minValue})");

            uint range = (uint)(maxValue - minValue);
            
            // 使用拒绝采样避免模运算偏差
            uint mask = uint.MaxValue - (uint.MaxValue % range);
            uint randomValue;
            
            do
            {
                Span<byte> bytes = stackalloc byte[4];
                _secureRng.GetBytes(bytes);
                randomValue = BitConverter.ToUInt32(bytes);
            } while (randomValue >= mask);
            
            return (int)(minValue + (randomValue % range));
        }

        /// <summary>
        /// 生成快速随机数用于动画效果
        /// 使用混合熵源提高随机性，但优化性能
        /// </summary>
        /// <param name="minValue">最小值（包含）</param>
        /// <param name="maxValue">最大值（不包含）</param>
        /// <returns>随机整数</returns>
        private int GetFastRandomNumber(int minValue, int maxValue)
        {
            // 使用时间戳和随机数混合，增加不可预测性
            var timeSeed = Environment.TickCount64;
            var rng = new Random(unchecked((int)(timeSeed ^ _animationRng.Next())));
            
            return rng.Next(minValue, maxValue);
        }

        /// <summary>
        /// 兼容性方法 - 保持向后兼容
        /// </summary>
        /// <param name="minValue">最小值（包含）</param>
        /// <param name="maxValue">最大值（不包含）</param>
        /// <returns>随机整数</returns>
        private int GetRandomNumber(int minValue, int maxValue)
        {
            return GetCryptographicallySecureRandomNumber(minValue, maxValue);
        }

        private void UpdateLabelOutputs(List<string> outputs)
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsAutoClose)
            {
                PeopleControlPane.Opacity = 0.4;
                PeopleControlPane.IsHitTestVisible = false;

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    BorderBtnRand_MouseUp(BorderBtnRand, null);
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
                    where s != ""
                    select s);
                PeopleCount = NameList.Count;
                TextBlockPeopleCount.Text = PeopleCount.ToString();
                if (PeopleCount == 0)
                {
                    TextBlockPeopleCount.Text = "点击此处以导入名单";
                }
            }
        }

        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new NamesInputWindow().ShowDialog();
            ReloadNamesFromFile();
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}















