using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace аудіобібліотека_голоси_природи
{
    public partial class LoginWindow : Window
    {
        private string usersPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "users.json");
        private bool isLoginMode = true;

        public class UserData
        {
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string Role { get; set; }
        }

        public LoginWindow()
        {
            InitializeComponent();
            string dir = Path.GetDirectoryName(usersPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private void SwitchMode_Click(object sender, RoutedEventArgs e)
        {
            isLoginMode = !isLoginMode;
            if (isLoginMode)
            {
                TitleTxt.Text = "Увійти";
                MainActionBtn.Content = "УВІЙТИ";
                SwitchModeBtn.Content = "Немає акаунту? Зареєструватися";
            }
            else
            {
                TitleTxt.Text = "Реєстрація";
                MainActionBtn.Content = "СТВОРИТИ АКАУНТ";
                SwitchModeBtn.Content = "Вже є акаунт? Увійти";
            }
        }

        private void MainAction_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUsername.Text) || string.IsNullOrWhiteSpace(TxtPassword.Password)) return;
            var users = LoadUsers();
            string hash = HashPassword(TxtPassword.Password);

            if (isLoginMode)
            {
                var user = users.Find(u => u.Username == TxtUsername.Text && u.PasswordHash == hash);
                if (user != null)
                {
                    new MainWindow(user.Role).Show();
                    this.Close();
                }
                else MessageBox.Show("Невірний логін або пароль");
            }
            else
            {
                if (users.Exists(u => u.Username == TxtUsername.Text)) { MessageBox.Show("Логін зайнятий"); return; }
                users.Add(new UserData
                {
                    Username = TxtUsername.Text,
                    PasswordHash = hash,
                    Role = TxtUsername.Text.ToLower().Contains("admin") ? "Admin" : "User"
                });
                File.WriteAllText(usersPath, JsonConvert.SerializeObject(users));
                MessageBox.Show("Готово! Тепер увійдіть.");
                SwitchMode_Click(null, null);
            }
        }

        private string HashPassword(string p)
        {
            using (SHA256 s = SHA256.Create()) return Convert.ToBase64String(s.ComputeHash(Encoding.UTF8.GetBytes(p)));
        }

        private List<UserData> LoadUsers()
        {
            if (!File.Exists(usersPath)) return new List<UserData>();
            return JsonConvert.DeserializeObject<List<UserData>>(File.ReadAllText(usersPath)) ?? new List<UserData>();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}