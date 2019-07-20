using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BGMNALateNightAnimeBotLib;
using BGMNALateNightAnimeBotLib.Model;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace BGMNALateNightAnimeBot
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            var configBuilder = new ConfigurationBuilder();
            // ReSharper disable once StringLiteralTypo
            configBuilder.AddEnvironmentVariables("lnab");
            configBuilder.AddJsonFile("config.json", true);
            ConfigurationRoot = configBuilder.Build();

            Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
                .CreateLogger();

            PublishInfo = new PublishInfo();
            DataContext = PublishInfo;
            InitializeComponent();
        }

        private Logger Logger { get; }
        private PublishInfo PublishInfo { get; }
        private IConfigurationRoot ConfigurationRoot { get; }

        private async void BangumiId_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var oldVal = PublishInfo.BangumiId;
            await Task.Delay(1000);
            if (PublishInfo.BangumiId != oldVal || string.IsNullOrWhiteSpace(PublishInfo.BangumiId)) return;
            try
            {
                ProgressBar.Visibility = Visibility.Visible;
                await PublishInfo.InitDataFromBangumi(PublishInfo.BangumiId);
            }
            catch
            {
                PublishInfo.ChsName = null;
                PublishInfo.JpnName = null;
                PublishInfo.Cover = null;
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void List_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                foreach (var file in files)
                    PublishInfo.Subtitle.Add(new FileInfo(file));
        }

        private void ListRemove_Click(object sender, RoutedEventArgs e)
        {
            foreach (var subtitleListSelectedItem in SubtitleList.SelectedItems.Cast<FileInfo>().ToList())
                PublishInfo.Subtitle.Remove(subtitleListSelectedItem);
        }

        private async void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            var bot = new Bot(ConfigurationRoot["token"]);
            var dialog = await this.ShowProgressAsync("Sending", "Sending Message");
            dialog.SetIndeterminate();
            try
            {
                var (success, result) = await bot.SendInfo(PublishInfo);
                if (!success)
                {
                    Logger.Error("Exception on validate: {@result}", result);
                    await this.ShowMessageAsync("Error", "Exception on validation. See log for details.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception on sending: ");
                await this.ShowMessageAsync("Error", "Exception on sending. See log for details.");
            }

            await dialog.CloseAsync();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ConfigurationRoot["token"]))
            {
                await this.ShowMessageAsync("Error", "Cannot find bot token. See Readme for more information.");
                Close();
            }
        }
    }
}