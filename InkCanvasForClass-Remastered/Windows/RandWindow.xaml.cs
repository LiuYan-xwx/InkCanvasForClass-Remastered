using InkCanvasForClass_Remastered.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.VisualBasic;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;


namespace InkCanvasForClass_Remastered
{
    /// <summary>
    /// Interaction logic for RandWindow.xaml
    /// </summary>
    public partial class RandWindow : Window
    {
        private HashSet<int> drawnIndices = new HashSet<int>();

        public RandWindow(Settings settings)
        {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            BorderBtnHelp.Visibility = settings.RandSettings.DisplayRandWindowNamesInputBtn == false ? Visibility.Collapsed : Visibility.Visible;
            RandMaxPeopleOneTime = settings.RandSettings.RandWindowOnceMaxStudents;
            RandDoneAutoCloseWaitTime = (int)settings.RandSettings.RandWindowOnceCloseLatency * 1000;
        }

        public RandWindow(Settings settings, bool IsAutoClose)
        {
            InitializeComponent();
            isAutoClose = IsAutoClose;
            PeopleControlPane.Opacity = 0.4;
            PeopleControlPane.IsHitTestVisible = false;
            BorderBtnHelp.Visibility = settings.RandSettings.DisplayRandWindowNamesInputBtn == false ? Visibility.Collapsed : Visibility.Visible;
            RandMaxPeopleOneTime = settings.RandSettings.RandWindowOnceMaxStudents;
            RandDoneAutoCloseWaitTime = (int)settings.RandSettings.RandWindowOnceCloseLatency * 1000;

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BorderBtnRand_MouseUp(BorderBtnRand, null);
                });
            })).Start();
        }

        public static int randSeed = 0;
        public bool isAutoClose = false;
        public bool isNotRepeatName = false;

        public int TotalCount = 1;
        public int PeopleCount = 60;
        public List<string> Names = new List<string>();

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (RandMaxPeopleOneTime == -1 && TotalCount >= PeopleCount) return;
            if (RandMaxPeopleOneTime != -1 && TotalCount >= RandMaxPeopleOneTime) return;
            TotalCount++;
            LabelNumberCount.Text = TotalCount.ToString();
            SymbolIconStart.Symbol = Symbol.People;
            BorderBtnAdd.Opacity = 1;
            BorderBtnMinus.Opacity = 1;
        }

        private void BorderBtnMinus_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (TotalCount < 2) return;
            TotalCount--;
            LabelNumberCount.Text = TotalCount.ToString();
            if (TotalCount == 1)
            {
                SymbolIconStart.Symbol = Symbol.Contact;
            }
        }

        public int RandWaitingTimes = 100;
        public int RandWaitingThreadSleepTime = 5;
        public int RandMaxPeopleOneTime = 10;
        public int RandDoneAutoCloseWaitTime = 2500;

        private void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (CheckBoxNotRepeatName.IsChecked == true && drawnIndices.Count + TotalCount > PeopleCount)
            {
                MessageBox.Show("没有足够的未被抽过的人！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<string> outputs = new List<string>();
            List<int> rands = new List<int>();

            LabelOutput2.Visibility = Visibility.Collapsed;
            LabelOutput3.Visibility = Visibility.Collapsed;

            new Thread(new ThreadStart(() =>
            {
                for (int i = 0; i < RandWaitingTimes; i++)
                {
                    int rand = GetRandomNumber(1, PeopleCount + 1);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Names.Count != 0)
                        {
                            LabelOutput.Content = Names[rand - 1];
                        }
                        else
                        {
                            LabelOutput.Content = rand.ToString();
                        }
                    });

                    Thread.Sleep(RandWaitingThreadSleepTime);
                }

                List<int> shuffledIndices = Enumerable.Range(0, PeopleCount).ToList();
                Shuffle(shuffledIndices);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    int count = 0;
                    for (int i = 0; i < shuffledIndices.Count && count < TotalCount; i++)
                    {
                        int index = shuffledIndices[i];
                        if (CheckBoxNotRepeatName.IsChecked == true)
                        {
                            if (drawnIndices.Contains(index))
                            {
                                continue;
                            }
                            drawnIndices.Add(index);
                        }
                        if (Names.Count != 0)
                        {
                            outputs.Add(Names[index]);
                        }
                        else
                        {
                            outputs.Add((index + 1).ToString());
                        }
                        count++;
                    }

                    UpdateLabelOutputs(outputs);
                    if (isAutoClose)
                    {
                        new Thread(new ThreadStart(() =>
                        {
                            Thread.Sleep(RandDoneAutoCloseWaitTime);
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                PeopleControlPane.Opacity = 1;
                                PeopleControlPane.IsHitTestVisible = true;
                                Close();
                            });
                        })).Start();
                    }
                });
            })).Start();
        }

        private int GetRandomNumber(int minValue, int maxValue)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] randomNumber = new byte[4];
                rng.GetBytes(randomNumber);
                int value = BitConverter.ToInt32(randomNumber, 0);
                return new Random(value).Next(minValue, maxValue);
            }
        }

        private void UpdateLabelOutputs(List<string> outputs)
        {
            string outputString = "";
            if (TotalCount <= 5)
            {
                LabelOutput.Content = string.Join(Environment.NewLine, outputs);
            }
            else if (TotalCount <= 10)
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

        private void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Names = new List<string>();
            if (File.Exists(App.RootPath + "Names.txt"))
            {
                string[] fileNames = File.ReadAllLines(App.RootPath + "Names.txt");
                string[] replaces = new string[0];

                if (File.Exists(App.RootPath + "Replace.txt"))
                {
                    replaces = File.ReadAllLines(App.RootPath + "Replace.txt");
                }

                //Fix emtpy lines
                foreach (string str in fileNames)
                {
                    string s = str;
                    //Make replacement
                    foreach (string replace in replaces)
                    {
                        if (s == Strings.Left(replace, replace.IndexOf("-->")))
                        {
                            s = Strings.Mid(replace, replace.IndexOf("-->") + 4);
                        }
                    }

                    if (s != "") Names.Add(s);
                }

                PeopleCount = Names.Count();
                TextBlockPeopleCount.Text = PeopleCount.ToString();
                if (PeopleCount == 0)
                {
                    PeopleCount = 60;
                    TextBlockPeopleCount.Text = "点击此处以导入名单";
                }
            }
        }

        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            new NamesInputWindow().ShowDialog();
            Window_Loaded(this, null);
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}















