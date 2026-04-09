using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace аудіобібліотека_голоси_природи
{
    public partial class MainWindow : Window
    {
        public class AudioTrack
        {
            public string Title { get; set; }
            public string Category { get; set; }
            public string Duration { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            CreateDataDirectory();

            List<AudioTrack> tracks = new List<AudioTrack>
            {
                new AudioTrack { Title = "Ранковий ліс", Category = "Ліс", Duration = "05:12" },
                new AudioTrack { Title = "Злива та грім", Category = "Дощ", Duration = "12:30" },
                new AudioTrack { Title = "Гірський струмок", Category = "Вода", Duration = "04:15" },
                new AudioTrack { Title = "Спів солов'я", Category = "Птахи", Duration = "03:45" }
            };

            SoundsList.ItemsSource = tracks;
        }

        private void CreateDataDirectory()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            RootWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F4F7F4"));
            SidePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0EEE0"));
            MainTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B261E"));
            LogoText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B261E"));
            BtnForest.Foreground = Brushes.Black;
            BtnWater.Foreground = Brushes.Black;
            NightBtnText.Foreground = Brushes.Black;
        }

        private void NightButton_Click(object sender, RoutedEventArgs e)
        {
            RootWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B261E"));
            SidePanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D140F"));
            MainTitle.Foreground = Brushes.White;
            LogoText.Foreground = Brushes.White;
            BtnForest.Foreground = Brushes.White;
            BtnWater.Foreground = Brushes.White;
            NightBtnText.Foreground = Brushes.White;
        }
    }
}