﻿using Hardcodet.Wpf.TaskbarNotification;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using InkCanvasForClass_Remastered.Services.Logging;
using InkCanvasForClass_Remastered.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace InkCanvasForClass_Remastered
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IAppHost
    {
        private IHost _host;
        private Mutex mutex;
        private ILogger<App> Logger;
        private Settings Settings = new();

        public static T GetService<T>() => IAppHost.GetService<T>();

        public static readonly string AppRootFolderPath = "./";
        public static readonly string AppLogFolderPath = Path.Combine(AppRootFolderPath, "Logs");


        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ProcessUnhandledException(e.Exception);
        }

        private void ProcessUnhandledException(Exception e)
        {
            Logger.LogCritical(e, "发生严重错误");
            if (!Settings.IsCriticalSafeMode)
            {
                InkCanvasForClass_Remastered.MainWindow.ShowNewMessage("抱歉，出现未预期的异常，可能导致 ICC-Re 运行不稳定。\n建议保存墨迹后重启应用。", true);
                return;
            }
            switch(Settings.CriticalSafeModeMethod)
            {
                case 0:
                    Logger?.LogInformation("因教学安全模式设定，应用将自动退出");
                    Current.Shutdown();
                    break;
                case 1:
                    Logger?.LogInformation("因教学安全模式设定，应用将自动静默重启");
                    Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");
                    Current.Shutdown();
                    break;
                case 2:
                    Logger?.LogInformation("因教学安全模式设定，应用将忽略异常并显示一条通知");
                    InkCanvasForClass_Remastered.MainWindow.ShowNewMessage("抱歉，出现未预期的异常，可能导致 ICC-Re 运行不稳定。\n建议保存墨迹后重启应用。", true);
                    break;
                case 3:
                    Logger?.LogInformation("因教学安全模式设定，应用将直接忽略异常");
                    break;
                default:
                    Logger?.LogWarning("无效的教学安全模式设置：{}", Settings.CriticalSafeModeMethod);
                    break;
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            mutex = new Mutex(true, "InkCanvasForClass-Remastered", out bool ret);
            if (!ret && !e.Args.Contains("-m"))
            {
                MessageBox.Show("已有一个程序实例正在运行");
                Environment.Exit(0);
                return;
            }

            FileFolderService.CreateFolders();

            IAppHost.Host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 在这里注册所有的服务、视图模型和窗口
                    ConfigureServices(services);
                })
                .Build();

            Logger = GetService<ILogger<App>>();
            Logger.LogInformation("InkCanvasForClass-Remastered 启动，Version: {Version}", Assembly.GetExecutingAssembly().GetName().Version);

            Logger.LogInformation("加载设置");
            GetService<SettingsService>().LoadSettings();
            Settings = GetService<SettingsService>().Settings;

            await IAppHost.Host.StartAsync();

            var mainWindow = GetService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();

            var taskbar = (TaskbarIcon)FindResource("TaskbarTrayIcon");

            await GetService<FileFolderService>().ProcessOldFilesAsync();


            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 注册服务
            services.AddSingleton<SettingsService>();
            services.AddSingleton<IPowerPointService, PowerPointService>();
            services.AddSingleton<FileFolderService>();
            services.AddSingleton<ITimeMachineService, TimeMachineService>();
            // 注册视图模型
            services.AddTransient<MainViewModel>();
            services.AddTransient<RandViewModel>();
            services.AddTransient<NamesInputViewModel>();

            // 注册窗口
            services.AddSingleton<MainWindow>();
            services.AddTransient<RandWindow>();
            services.AddTransient<NamesInputWindow>();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new FileLoggerProvider());

                builder.AddConsoleFormatter<MyConsoleFormatter, ConsoleFormatterOptions>();
                builder.AddConsole(console =>
                {
                    console.FormatterName = "myformatter";
                });
                builder.SetMinimumLevel(LogLevel.Trace);
            });
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // 保存设置
            var settingsService = GetService<SettingsService>();
            settingsService?.SaveSettings();

            IAppHost.Host?.StopAsync();

            base.OnExit(e);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            try
            {
                if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines == -1)
                    e.Handled = false;
                else
                    try
                    {
                        ScrollViewerEx SenderScrollViewer = (ScrollViewerEx)sender;
                        SenderScrollViewer.ScrollToVerticalOffset(SenderScrollViewer.VerticalOffset - e.Delta * 10 * System.Windows.Forms.SystemInformation.MouseWheelScrollLines / (double)120);
                        e.Handled = true;
                    }
                    catch { }
            }
            catch { }
        }
    }
}
