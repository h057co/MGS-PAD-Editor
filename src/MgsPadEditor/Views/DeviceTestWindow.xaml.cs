using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MgsPadEditor.Models;
using MgsPadEditor.Services;

namespace MgsPadEditor.Views
{
    public partial class DeviceTestWindow : Window
    {
        private readonly DirectInputService _inputService;
        private readonly GamepadDevice _device;
        private readonly List<Border> _buttonBadges = new();
        private readonly bool[] _prevButtons = new bool[32];
        private int _prevDpad = -1;
        private bool _prevL2Active = false;
        private bool _prevR2Active = false;

        private static readonly SolidColorBrush BrushNormalBg = new(Color.FromRgb(0x10, 0x13, 0x1B));
        private static readonly SolidColorBrush BrushPressedBg = new(Color.FromRgb(0x00, 0xFF, 0x66)); // MGS Neon Green
        private static readonly SolidColorBrush BrushBlackBorder = new(Color.FromRgb(0x00, 0x00, 0x00));
        private static readonly SolidColorBrush BrushNormalText = new(Color.FromRgb(0x9A, 0xA5, 0xB8));

        public DeviceTestWindow(DirectInputService inputService, GamepadDevice device)
        {
            InitializeComponent();
            _inputService = inputService;
            _device = device;
            bool isSony = DirectInputService.IsSonyDevice(device);
            string profileType = isSony ? "SONY DUALSENSE/DUALSHOCK" : "DIRECTINPUT/XINPUT";
            TxtDeviceName.Text = $"TELEMETRÍA // {device.Name.ToUpperInvariant()} [{profileType}] (VID:{device.VendorId:X4} PID:{device.ProductId:X4})";

            BuildButtonBadges(Math.Max(16, device.ButtonsCount));
        }

        private void BuildButtonBadges(int count)
        {
            ButtonsPanel.Children.Clear();
            _buttonBadges.Clear();

            for (int i = 0; i < count; i++)
            {
                var border = new Border
                {
                    Width = 32,
                    Height = 28,
                    CornerRadius = new CornerRadius(0),
                    Background = BrushNormalBg,
                    BorderBrush = BrushBlackBorder,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(3),
                    Child = new TextBlock
                    {
                        Text = i.ToString(),
                        Foreground = BrushNormalText,
                        FontWeight = FontWeights.Black,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 11,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                _buttonBadges.Add(border);
                ButtonsPanel.Children.Add(border);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _inputService.StartStreamingState(_device, OnStateReceived);
            AppendLog($"[SYS_INIT] Captura continua iniciada para '{_device.Name}'.");
            if (DirectInputService.IsSonyDevice(_device))
            {
                AppendLog("[PROFILE] Mando Sony detectado: L-Stick (X,Y), R-Stick (Z,Rz), Gatillo L2 (Rx/B6), Gatillo R2 (Ry/B7).");
            }
        }

        private void OnStateReceived(ControllerStateSnapshot snapshot)
        {
            Dispatcher.InvokeAsync(() =>
            {
                // 1. Update Left Stick Dot Position (Canvas 90x90, Dot 14x14, Center at 38,38)
                double lx = 38 + (snapshot.LeftStickX * 36);
                double ly = 38 - (snapshot.LeftStickY * 36);
                Canvas.SetLeft(DotLeftStick, Math.Clamp(lx, 2, 74));
                Canvas.SetTop(DotLeftStick, Math.Clamp(ly, 2, 74));
                TxtLeftStickCoords.Text = $"X: {snapshot.LeftStickX:+0.00;-0.00} | Y: {snapshot.LeftStickY:+0.00;-0.00}";

                // 2. Update Right Stick Dot Position
                double rx = 38 + (snapshot.RightStickX * 36);
                double ry = 38 - (snapshot.RightStickY * 36);
                Canvas.SetLeft(DotRightStick, Math.Clamp(rx, 2, 74));
                Canvas.SetTop(DotRightStick, Math.Clamp(ry, 2, 74));
                TxtRightStickCoords.Text = $"X: {snapshot.RightStickX:+0.00;-0.00} | Y: {snapshot.RightStickY:+0.00;-0.00}";

                // 3. Update Triggers (0.0 to 1.0)
                int l2Pct = (int)Math.Round(snapshot.LeftTrigger * 100);
                int r2Pct = (int)Math.Round(snapshot.RightTrigger * 100);
                TxtLeftTrigger.Text = $"L2 (GATILLO IZQ): {l2Pct}%";
                PbLeftTrigger.Value = snapshot.LeftTrigger;
                TxtRightTrigger.Text = $"R2 (GATILLO DER): {r2Pct}%";
                PbRightTrigger.Value = snapshot.RightTrigger;

                if (l2Pct > 20 && !_prevL2Active)
                {
                    AppendLog($"[GATILLO] L2 presionado ({l2Pct}%) -> Menú Ítems (pad_button6)");
                    _prevL2Active = true;
                }
                else if (l2Pct <= 5)
                {
                    _prevL2Active = false;
                }

                if (r2Pct > 20 && !_prevR2Active)
                {
                    AppendLog($"[GATILLO] R2 presionado ({r2Pct}%) -> Menú Armas (pad_button7)");
                    _prevR2Active = true;
                }
                else if (r2Pct <= 5)
                {
                    _prevR2Active = false;
                }

                // 4. Update Raw DirectInput Telemetry
                TxtRawX.Text = $"X: {snapshot.RawX}";
                TxtRawY.Text = $"Y: {snapshot.RawY}";
                TxtRawZ.Text = $"Z: {snapshot.RawZ}";
                TxtRawRx.Text = $"Rx: {snapshot.RawRx}";
                TxtRawRy.Text = $"Ry: {snapshot.RawRy}";
                TxtRawRz.Text = $"Rz: {snapshot.RawRz}";

                // 5. Update D-Pad
                string dpadText = snapshot.PovDirection switch
                {
                    -1 => "CRUCETA: NEUTRAL",
                    >= 0 and <= 4500 or >= 31500 => "CRUCETA: ARRIBA ▲",
                    > 4500 and <= 13500 => "CRUCETA: DERECHA ▶",
                    > 13500 and <= 22500 => "CRUCETA: ABAJO ▼",
                    > 22500 and < 31500 => "CRUCETA: IZQUIERDA ◀",
                    _ => $"CRUCETA: {snapshot.PovDirection}"
                };
                TxtDpad.Text = dpadText;
                if (snapshot.PovDirection != _prevDpad && snapshot.PovDirection != -1)
                {
                    AppendLog($"[D-PAD] {dpadText}");
                }
                _prevDpad = snapshot.PovDirection;

                // 6. Update Buttons
                for (int i = 0; i < _buttonBadges.Count; i++)
                {
                    bool isPressed = i < snapshot.Buttons.Length && snapshot.Buttons[i];

                    if (isPressed)
                    {
                        _buttonBadges[i].Background = BrushPressedBg;
                        if (_buttonBadges[i].Child is TextBlock tb) tb.Foreground = Brushes.Black;

                        if (i < _prevButtons.Length && !_prevButtons[i])
                        {
                            string btnName = i switch
                            {
                                0 => "Cuadrado (□)",
                                1 => "Cruz (✕)",
                                2 => "Círculo (○)",
                                3 => "Triángulo (△)",
                                4 => "L1",
                                5 => "R1",
                                6 => "L2",
                                7 => "R2",
                                8 => "Create/Share",
                                9 => "Options",
                                10 => "L3",
                                11 => "R3",
                                12 => "PS",
                                13 => "Touchpad",
                                _ => $"Botón {i}"
                            };
                            AppendLog($"[BOTÓN] {btnName} PRESIONADO -> pad_button{i}");
                        }
                    }
                    else
                    {
                        _buttonBadges[i].Background = BrushNormalBg;
                        if (_buttonBadges[i].Child is TextBlock tb) tb.Foreground = BrushNormalText;
                    }

                    if (i < _prevButtons.Length)
                    {
                        _prevButtons[i] = isPressed;
                    }
                }
            });
        }

        private void AppendLog(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            TxtLog.AppendText($"[{time}] {message}\r\n");
            TxtLog.ScrollToEnd();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _inputService.StopPolling();
            _inputService.CloseDevice();
        }
    }
}