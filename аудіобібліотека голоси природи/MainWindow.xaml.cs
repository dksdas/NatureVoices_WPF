using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.ComponentModel;

namespace аудіобібліотека_голоси_природи
{
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private ObservableCollection<AudioTrack> allTracks = new ObservableCollection<AudioTrack>();
        private int currentTrackIndex = -1;
        private bool isPlaying = false;
        private string userRole;

        public class AudioTrack : INotifyPropertyChanged
        {
            public string Title { get; set; }
            public string Category { get; set; }
            public string FileName { get; set; }
            private string _dur = "--:--";
            public string Duration { get => _dur; set { _dur = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration))); } }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public MainWindow() { InitializeComponent(); }

        public MainWindow(string role) : this()
        {
            this.userRole = role;
            if (userRole == "Admin") MainTitle.Text += " (Admin)";

            LoadData();

            timer.Interval = TimeSpan.FromMilliseconds(200); 
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimelineSlider.Value = mediaPlayer.Position.TotalSeconds / mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

                string current = mediaPlayer.Position.ToString(@"m\:ss");
                string total = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
                TimeStatus.Text = $"{current} / {total}";
            }
        }

        private void LoadData()
        {
            allTracks = new ObservableCollection<AudioTrack> {
                new AudioTrack { Title = "Ранковий ліс", Category = "Ліс", FileName = "forest_morning.mp3" },
                new AudioTrack { Title = "Злива та грім", Category = "Дощ", FileName = "heavy_rain.mp3" },
                new AudioTrack { Title = "Гірський струмок", Category = "Вода", FileName = "mountain_stream.mp3" },
                new AudioTrack { Title = "Спів солов'я", Category = "Птахи", FileName = "nightingale.mp3" }
            };
            SoundsList.ItemsSource = allTracks;

            foreach (var t in allTracks) UpdateTrackDuration(t);
        }

        private void UpdateTrackDuration(AudioTrack track)
        {
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", track.FileName);
            if (File.Exists(p))
            {
                MediaPlayer mp = new MediaPlayer();
                mp.Open(new Uri(p));
                mp.MediaOpened += (s, e) => {
                    if (mp.NaturalDuration.HasTimeSpan)
                        track.Duration = mp.NaturalDuration.TimeSpan.ToString(@"m\:ss");
                    mp.Close();
                };
            }
        }

        private void PlayTrack(AudioTrack track)
        {
            if (track == null) return;
            string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", track.FileName);
            if (File.Exists(p))
            {
                mediaPlayer.Open(new Uri(p));
                mediaPlayer.Play();
                PlayingNow.Text = track.Title;
                PlayPauseIcon.Text = "⏸";
                isPlaying = true;
                timer.Start();
                currentTrackIndex = allTracks.IndexOf(track);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e) => PlayTrack((sender as Button).DataContext as AudioTrack);

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex == -1 && allTracks.Count > 0) { PlayTrack(allTracks[0]); return; }
            if (isPlaying) { mediaPlayer.Pause(); PlayPauseIcon.Text = "▶"; isPlaying = false; }
            else { mediaPlayer.Play(); PlayPauseIcon.Text = "⏸"; isPlaying = true; }
        }

        private void TimelineSlider_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
                mediaPlayer.Position = TimeSpan.FromSeconds(TimelineSlider.Value * mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds);
        }

        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => mediaPlayer.Volume = e.NewValue;

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            var btn = (sender as Button);
            BtnHome.Background = BtnForest.Background = BtnWater.Background = Brushes.Transparent;
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C8"));
            if (btn.Name == "BtnHome") SoundsList.ItemsSource = allTracks;
            else SoundsList.ItemsSource = allTracks.Where(t => btn.Content.ToString().Contains(t.Category)).ToList();
        }

        private void Next_Click(object sender, RoutedEventArgs e) { if (currentTrackIndex < allTracks.Count - 1) PlayTrack(allTracks[++currentTrackIndex]); }
        private void Prev_Click(object sender, RoutedEventArgs e) { if (currentTrackIndex > 0) PlayTrack(allTracks[--currentTrackIndex]); }

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme("#F4F7F4", "#E0EEE0", "#1B261E", "#808080", "#2D3436", true);
        }

        private void NightButton_Click(object sender, RoutedEventArgs e)
        {
            SetTheme("#121212", "#1E1E1E", "#FFFFFF", "#B3B3B3", "#FFFFFF", false);
        }
        private void SetTheme(string bgHex, string sideHex, string textHex, string subTextHex, string btnTextHex, bool isDay)
        {
            var bgColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex));
            var sideColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sideHex));
            var textColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textHex));
            var subTextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(subTextHex));
            var btnTextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnTextHex));

            RootWindow.Background = bgColor;
            SidePanel.Background = sideColor;
            PlayerBar.Background = isDay ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#282828"));

            MainTitle.Foreground = textColor;
            LogoText.Foreground = textColor;
            PlayingNow.Foreground = textColor;
            NowPlayingLabel.Foreground = subTextColor;
            TimeStatus.Foreground = isDay ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678")) : subTextColor;

            BtnHome.Foreground = btnTextColor;
            BtnForest.Foreground = btnTextColor;
            BtnWater.Foreground = btnTextColor;

            VolIcon.Foreground = textColor;
            BtnPlayPause.Foreground = isDay ? Brushes.White : Brushes.Black;

            DecorLeaves.Foreground = isDay ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C8")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E4D3E"));
            
            ThemePanel.Background = isDay ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C8")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#282828"));
            if (isDay)
            {
                DayBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678"));
                NightBtn.Background = Brushes.Transparent;
                NightBtnText.Foreground = Brushes.Black;
            }
            else
            {
                NightBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678"));
                DayBtn.Background = Brushes.Transparent;
                NightBtnText.Foreground = Brushes.White;
            }
        }
    }
}
