using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BangumiApi;
using BGMNALateNightAnimeBotLib.Annotations;

namespace BGMNALateNightAnimeBotLib.Model
{
    public class PublishInfo : INotifyPropertyChanged
    {
        private string _downloadUrl;
        private Uri _cover;
        private string _jpnName;
        private string _chsName;
        [CanBeNull] private string _subtitleUrl;
        private string _bangumiId;

        [Required]
        public string ChsName
        {
            get => _chsName;
            set
            {
                if (value == _chsName) return;
                _chsName = value;
                OnPropertyChanged();
            }
        }

        [Required]
        public string JpnName
        {
            get => _jpnName;
            set
            {
                if (value == _jpnName) return;
                _jpnName = value;
                OnPropertyChanged();
            }
        }

        [Required]
        public Uri Cover
        {
            get => _cover;
            set
            {
                if (Equals(value, _cover)) return;
                _cover = value;
                OnPropertyChanged();
            }
        }

        [Required]
        public string DownloadUrl
        {
            get => _downloadUrl;
            set
            {
                if (value == _downloadUrl) return;
                _downloadUrl = value;
                OnPropertyChanged();
            }
        }

        [CanBeNull]
        public string SubtitleUrl
        {
            get => _subtitleUrl;
            set
            {
                if (value == _subtitleUrl) return;
                _subtitleUrl = value;
                OnPropertyChanged();
            }
        }

        public string BangumiId
        {
            get => _bangumiId;
            set
            {
                value = Regex.Match(value, @"\d*", RegexOptions.Compiled).Value;
                if (value == _bangumiId) return;
                _bangumiId = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FileInfo> Subtitle { get; } = new ObservableCollection<FileInfo>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task InitDataFromBangumi(string bangumiId)
        {
            using (var bangumiApiClient = new BangumiApiClient())
            {
                var subject = await bangumiApiClient.GetSubjectAsync(bangumiId);
                ChsName = subject.ChsName;
                JpnName = subject.JpnName;
                Cover = new Uri(subject.Cover.LargeCover);
                BangumiId = bangumiId;
            }
        }
    }
}