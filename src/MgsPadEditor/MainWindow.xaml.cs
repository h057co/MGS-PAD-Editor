using System.ComponentModel;
using System.Windows;
using MgsPadEditor.Models;
using MgsPadEditor.ViewModels;
using MgsPadEditor.Views;

namespace MgsPadEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ViewModel.RequestCaptureModal = OpenCaptureModal;
            ViewModel.RequestTestModal = OpenTestModal;
        }

        private void OpenCaptureModal(MappingRowViewModel row, bool isKeyboardMouseMode)
        {
            var dialog = new CaptureDialog(
                ViewModel.InputService,
                ViewModel.SelectedDevice,
                row.Description,
                row.InputKey,
                isKeyboardMouseMode
            )
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.CapturedInput))
            {
                row.TargetValue = dialog.CapturedInput;
            }
        }

        private void OpenTestModal(GamepadDevice device)
        {
            var window = new DeviceTestWindow(ViewModel.InputService, device)
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ViewModel.PromptSaveIfModified())
            {
                e.Cancel = true;
                return;
            }

            ViewModel.InputService.Dispose();
        }
    }
}