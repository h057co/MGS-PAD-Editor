using System;
using System.Diagnostics;
using System.Windows;

namespace MgsPadEditor.Views
{
    public partial class AboutWindow : Window
    {
        private const string GitHubUrl = "https://github.com/h057co";

        public AboutWindow()
        {
            InitializeComponent();
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GitHubUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo abrir el navegador predeterminado:\n{ex.Message}\n\nPuedes visitar: {GitHubUrl}",
                    "Abrir Enlace",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void BtnCopyLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(GitHubUrl);
                MessageBox.Show(
                    $"Enlace copiado al portapapeles:\n{GitHubUrl}",
                    "Portapapeles",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al copiar: {ex.Message}",
                    "Portapapeles",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

