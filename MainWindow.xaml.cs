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
using Newtonsoft.Json;
using Microsoft.VisualBasic;

namespace аудіобібліотека_голоси_природи
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private ObservableCollection<AudioTrack> allTracks = new ObservableCollection<AudioTrack>();
        private ObservableCollection<CategoryItem> categories = new ObservableCollection<CategoryItem>();
        private int currentTrackIndex = -1;
        private bool isPlaying = false;
        private string userRole;
        private Visibility _adminControlsVisibility = Visibility.Collapsed;

        public Visibility AdminControlsVisibility
        {
            get { return _adminControlsVisibility; }
            set { _adminControlsVisibility = value; OnPropertyChanged("AdminControlsVisibility"); }
        }

        private string DataPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private string JsonPath => Path.Combine(DataPath, "tracks.json");

        public class AudioTrack : INotifyPropertyChanged
        {
            public string Title { get; set; }
            public string Category { get; set; }
            public string FileName { get; set; }
            private string _dur = "--:--";
            public string Duration { get { return _dur; } set { _dur = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Duration")); } }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class CategoryItem
        {
            public string Name { get; set; }
        }

        public class AppData
        {
            public List<AudioTrack> Tracks { get; set; }
            public List<CategoryItem> Categories { get; set; }
        }

        public MainWindow() { InitializeComponent(); this.DataContext = this; }

        public MainWindow(string role) : this()
        {
            this.userRole = role;
            if (userRole == "Admin")
            {
                MainTitle.Text += " (Admin)";
                AdminPanel.Visibility = Visibility.Visible;
                AdminControlsVisibility = Visibility.Visible;
            }
            LoadData();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += Timer_Tick;
        }

        private void LoadData()
        {
            if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
            if (File.Exists(JsonPath))
            {
                var data = JsonConvert.DeserializeObject<AppData>(File.ReadAllText(JsonPath));
                if (data != null)
                {
                    allTracks = new ObservableCollection<AudioTrack>(data.Tracks ?? new List<AudioTrack>());
                    categories = new ObservableCollection<CategoryItem>(data.Categories ?? new List<CategoryItem>());
                }
            }

            if (categories.Count == 0)
            {
                categories.Add(new CategoryItem { Name = "🌲 Ліс" });
                categories.Add(new CategoryItem { Name = "🌊 Вода" });
            }

            SoundsList.ItemsSource = allTracks;
            CategoryListBox.ItemsSource = categories;
            foreach (var t in allTracks) UpdateTrackDuration(t);
        }

        private void SaveData()
        {
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(new AppData { Tracks = allTracks.ToList(), Categories = categories.ToList() }, Formatting.Indented));
        }

        private void AddTrack_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTrackTitle.Text)) return;
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Audio files (*.mp3)|*.mp3" };
            if (dlg.ShowDialog() == true)
            {
                string fn = Path.GetFileName(dlg.FileName);
                string dest = Path.Combine(DataPath, fn);
                if (!File.Exists(dest)) File.Copy(dlg.FileName, dest);
                var t = new AudioTrack { Title = NewTrackTitle.Text, Category = NewTrackCategory.Text, FileName = fn };
                allTracks.Add(t);
                SaveData();
                UpdateTrackDuration(t);
                NewTrackTitle.Clear(); NewTrackCategory.Clear();
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Введіть іконку та назву:", "Нова категорія", "📁 Нова категорія");
            if (!string.IsNullOrWhiteSpace(input)) { categories.Add(new CategoryItem { Name = input }); SaveData(); }
        }

        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as MenuItem).DataContext as CategoryItem;
            if (item != null) { categories.Remove(item); SaveData(); }
        }

        private void DeleteTrack_Click(object sender, RoutedEventArgs e)
        {
            var t = (sender as Button).DataContext as AudioTrack;
            if (t != null && MessageBox.Show($"Видалити {t.Title}?", "Видалення", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { allTracks.Remove(t); SaveData(); }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            CategoryListBox.SelectedItem = null; 
            BtnHome.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C8"));
            SoundsList.ItemsSource = allTracks;
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CategoryListBox.SelectedItem as CategoryItem;
            if (selected != null)
            {
                BtnHome.Background = Brushes.Transparent;
                string selectedCatName = selected.Name.Split(' ').Last().ToLower();

                SoundsList.ItemsSource = new ObservableCollection<AudioTrack>(
                    allTracks.Where(t => {
                        if (string.IsNullOrEmpty(t.Category)) return false;
                        var trackCategories = t.Category.Split(',')
                                                      .Select(c => c.Trim().ToLower())
                                                      .ToList();

                        return trackCategories.Contains(selectedCatName);
                    })
                );
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string s = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(s))
            {
                SoundsList.ItemsSource = allTracks;
                return;
            }

            SoundsList.ItemsSource = new ObservableCollection<AudioTrack>(
                allTracks.Where(x =>
                    x.Title.ToLower().Contains(s) ||
                    (x.Category != null && x.Category.ToLower().Contains(s))
                )
            );
        }

        private void PlayTrack(AudioTrack t)
        {
            if (t == null) return;
            string p = Path.Combine(DataPath, t.FileName);
            if (File.Exists(p))
            {
                mediaPlayer.Open(new Uri(p, UriKind.Absolute));
                mediaPlayer.Play();
                PlayingNow.Text = t.Title;
                PlayPauseIcon.Text = "⏸";
                isPlaying = true;
                timer.Start();
                currentTrackIndex = allTracks.IndexOf(t);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimelineSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                TimelineSlider.Value = mediaPlayer.Position.TotalSeconds;
                TimeStatus.Text = $"{mediaPlayer.Position:m\\:ss} / {mediaPlayer.NaturalDuration.TimeSpan:m\\:ss}";
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex == -1 && allTracks.Count > 0) { PlayTrack(allTracks[0]); return; }
            if (isPlaying) { mediaPlayer.Pause(); PlayPauseIcon.Text = "▶"; } else { mediaPlayer.Play(); PlayPauseIcon.Text = "⏸"; }
            isPlaying = !isPlaying;
        }

        private void TimelineSlider_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (mediaPlayer.NaturalDuration.HasTimeSpan) mediaPlayer.Position = TimeSpan.FromSeconds(TimelineSlider.Value); }
        private void PlayButton_Click(object sender, RoutedEventArgs e) => PlayTrack((sender as Button).DataContext as AudioTrack);
        private void Next_Click(object sender, RoutedEventArgs e) { if (currentTrackIndex < allTracks.Count - 1) PlayTrack(allTracks[++currentTrackIndex]); }
        private void Prev_Click(object sender, RoutedEventArgs e) { if (currentTrackIndex > 0) PlayTrack(allTracks[--currentTrackIndex]); }
        private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => mediaPlayer.Volume = e.NewValue;

        private void UpdateTrackDuration(AudioTrack t)
        {
            string p = Path.Combine(DataPath, t.FileName);
            if (File.Exists(p))
            {
                MediaPlayer mp = new MediaPlayer(); mp.Open(new Uri(p));
                mp.MediaOpened += (s, ev) => { if (mp.NaturalDuration.HasTimeSpan) t.Duration = mp.NaturalDuration.TimeSpan.ToString(@"m\:ss"); mp.Close(); };
            }
        }

        private void DayButton_Click(object sender, RoutedEventArgs e) => SetTheme("#F4F7F4", "#E0EEE0", "#1B261E", true);
        private void NightButton_Click(object sender, RoutedEventArgs e) => SetTheme("#121212", "#1E1E1E", "#FFFFFF", false);

        private void SetTheme(string bg, string side, string txt, bool isDay)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            var s = new SolidColorBrush((Color)ColorConverter.ConvertFromString(side));
            var t = new SolidColorBrush((Color)ColorConverter.ConvertFromString(txt));

            var cardBg = isDay ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
            var playerBg = isDay ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            var searchBg = isDay ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3D3D"));

            RootWindow.Background = b;
            SidePanel.Background = s;
            MainTitle.Foreground = t;
            LogoText.Foreground = t;
            PlayingNow.Foreground = t;
            NowPlayingLabel.Foreground = isDay ? Brushes.Gray : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));

            PlayerBar.Background = playerBg;
            SearchBox.Parent.GetValue(Border.BackgroundProperty); 

            this.Resources["CardBackground"] = cardBg;
            this.Resources["PrimaryText"] = t;

            DayBtn.Background = isDay ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678")) : Brushes.Transparent;
            NightBtn.Background = !isDay ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78A678")) : Brushes.Transparent;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}