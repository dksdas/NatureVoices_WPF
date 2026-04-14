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
        private bool isDarkMode = false;

        public class AudioTrack : INotifyPropertyChanged
        {
            public string Title { get; set; }
            public string Category { get; set; }
            public string FileName { get; set; }
            private string _duration = "--:--";
            public string Duration
            {
                get => _duration;
                set { _duration = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration))); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public MainWindow()
        {
            InitializeComponent();
            CreateDataDirectory();
            LoadData();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += Timer_Tick;
        }

        private void CreateDataDirectory()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private void LoadData()
        {
            allTracks = new ObservableCollection<AudioTrack>
            {
                new AudioTrack { Title = "Ранковий ліс", Category = "Ліс", FileName = "forest_morning.mp3" },
                new AudioTrack { Title = "Злива та грім", Category = "Дощ", FileName = "heavy_rain.mp3" },
                new AudioTrack { Title = "Гірський струмок", Category = "Вода", FileName = "mountain_stream.mp3" },
                new AudioTrack { Title = "Спів солов'я", Category = "Птахи", FileName = "nightingale.mp3" }
            };
            SoundsList.ItemsSource = allTracks;
            foreach (var track in allTracks) UpdateTrackDuration(track);
        }

        private void UpdateTrackDuration(AudioTrack track)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", track.FileName);
            if (File.Exists(fullPath))
            {
                MediaPlayer tempPlayer = new MediaPlayer();
                tempPlayer.Open(new Uri(fullPath));
                tempPlayer.MediaOpened += (s, e) => {
                    if (tempPlayer.NaturalDuration.HasTimeSpan)
                        track.Duration = tempPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                    tempPlayer.Close();
                };
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
                TimelineSlider.Value = mediaPlayer.Position.TotalSeconds / mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void PlayTrack(AudioTrack track)
        {
            if (track == null) return;
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", track.FileName);
            if (File.Exists(fullPath))
            {
                mediaPlayer.Open(new Uri(fullPath));
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
            Button clickedButton = sender as Button;
            string category = clickedButton.Content.ToString();

            // Скидаємо фон усіх кнопок меню
            BtnHome.Background = Brushes.Transparent;
            BtnForest.Background = Brushes.Transparent;
            BtnWater.Background = Brushes.Transparent;

            // Ставимо активний фон натиснутій кнопці
            clickedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C8"));

            if (category.Contains("Головна")) SoundsList.ItemsSource = allTracks;
            else
            {
                string filter = category.Split(' ').Last();
                SoundsList.ItemsSource = allTracks.Where(t => t.Category == filter).ToList();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex < allTracks.Count - 1) PlayTrack(allTracks[++currentTrackIndex]);
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex > 0) PlayTrack(allTracks[--currentTrackIndex]);
        }

        private void DayButton_Click(object sender, RoutedEventArgs e) => SetTheme("#F4F7F4", "#E0EEE0", "#1B261E", Brushes.Black, Brushes.White, "#C8E6C8", true);

        private void NightButton_Click(object sender, RoutedEventArgs e) => SetTheme("#1B261E", "#0D140F", "#FFFFFF", Brushes.White, "#2D3436", "#1B261E", false);

        private void SetTheme(string rootBg, string sideBg, string textHex, Brush btnText, object playerBg, string themePanelBg, bool isDay)
        {
            isDarkMode = !isDay;
            var textCol = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textHex));
            RootWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(rootBg));
            SidePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(sideBg));

            MainTitle.Foreground = textCol;
            LogoText.Foreground = textCol;
            PlayingNow.Foreground = textCol;
            NowPlayingLabel.Foreground = textCol;
            BtnForest.Foreground = btnText;
            BtnWater.Foreground = btnText;
            BtnHome.Foreground = btnText;
            NightBtnText.Foreground = btnText;
            BtnPrev.Foreground = btnText;
            BtnNext.Foreground = btnText;
            VolIcon.Foreground = textCol;
            DecorLeaves.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isDay ? "#C8E6C8" : "#78A678"));
            ThemePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(themePanelBg));

            if (isDay)
            {
                DayBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678"));
                NightBtn.Background = Brushes.Transparent;
                MoonIcon.Opacity = 0.5;
            }
            else
            {
                NightBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678"));
                DayBtn.Background = Brushes.Transparent;
                MoonIcon.Opacity = 1;
            }

            if (playerBg is string hex) PlayerBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            else PlayerBar.Background = (Brush)playerBg;
        }
    }
}