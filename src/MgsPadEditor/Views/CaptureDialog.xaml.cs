using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using MgsPadEditor.Models;
using MgsPadEditor.Services;

namespace MgsPadEditor.Views
{
    public partial class CaptureDialog : Window
    {
        private readonly DirectInputService _inputService;
        private readonly GamepadDevice? _device;
        private readonly bool _isKeyboardMouseMode;
        private readonly CancellationTokenSource _cts = new();

        public string? CapturedInput { get; private set; }

        public CaptureDialog(DirectInputService inputService, GamepadDevice? device, string actionDescription, string inputKey, bool isKeyboardMouseMode)
        {
            InitializeComponent();
            _inputService = inputService;
            _device = device;
            _isKeyboardMouseMode = isKeyboardMouseMode;

            TxtTargetAction.Text = $"{inputKey} — {actionDescription}";

            if (_isKeyboardMouseMode)
            {
                TxtPrompt.Text = "Presiona cualquier tecla del teclado o botón del ratón...";
                Title = "Capturar Tecla / Ratón";
            }
            else
            {
                TxtPrompt.Text = device != null
                    ? $"Presiona un botón o eje en: {device.Name}"
                    : "Conecta un mando o selecciónalo en la lista principal...";
                Title = "Capturar Entrada de Mando";
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isKeyboardMouseMode)
            {
                if (_device == null)
                {
                    TxtStatus.Text = "No hay mando seleccionado";
                    TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
                    return;
                }

                try
                {
                    string? captured = await _inputService.CaptureInputAsync(_device, _cts.Token);
                    if (!string.IsNullOrEmpty(captured))
                    {
                        OnInputDetected(captured);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Dialog closed
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = $"Error: {ex.Message}";
                }
            }
        }

        private void OnInputDetected(string dixiInput)
        {
            CapturedInput = dixiInput;
            TxtStatus.Text = $"Detectado: {dixiInput}";
            TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            TxtSubtitle.Text = "Pulsa Aceptar o presiona Enter para confirmar";
            BtnAccept.IsEnabled = true;
            BtnAccept.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isKeyboardMouseMode)
            {
                if (e.Key == Key.Escape)
                {
                    BtnCancel_Click(sender, e);
                    return;
                }

                string? key = KeyboardMouseCaptureService.ConvertKeyToDixi(e.Key);
                if (!string.IsNullOrEmpty(key))
                {
                    e.Handled = true;
                    OnInputDetected(key);
                }
            }
            else
            {
                if (e.Key == Key.Enter && BtnAccept.IsEnabled)
                {
                    BtnAccept_Click(sender, e);
                }
                else if (e.Key == Key.Escape)
                {
                    BtnCancel_Click(sender, e);
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isKeyboardMouseMode)
            {
                string? btn = KeyboardMouseCaptureService.ConvertMouseButtonToDixi(e.ChangedButton);
                if (!string.IsNullOrEmpty(btn))
                {
                    e.Handled = true;
                    OnInputDetected(btn);
                }
            }
        }

        private void BtnAccept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
