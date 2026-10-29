using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MgsPadEditor.Models;
using SharpDX.DirectInput;

namespace MgsPadEditor.Services
{
    public class ControllerStateSnapshot
    {
        public bool[] Buttons { get; set; } = Array.Empty<bool>();
        
        // Interpreted Sticks (-1.0 to +1.0)
        public double LeftStickX { get; set; }
        public double LeftStickY { get; set; }
        public double RightStickX { get; set; }
        public double RightStickY { get; set; }
        
        // Interpreted Triggers (0.0 to 1.0)
        public double LeftTrigger { get; set; }
        public double RightTrigger { get; set; }
        
        public int PovDirection { get; set; } // -1 = Center, 0 = Up, 9000 = Right, 18000 = Down, 27000 = Left
        
        // Raw DirectInput axis readings for exact telemetry diagnostics
        public int RawX { get; set; }
        public int RawY { get; set; }
        public int RawZ { get; set; }
        public int RawRx { get; set; }
        public int RawRy { get; set; }
        public int RawRz { get; set; }
    }

    public class DirectInputService : IDisposable
    {
        private readonly DirectInput _directInput;
        private Joystick? _activeJoystick;
        private CancellationTokenSource? _pollCts;

        public DirectInputService()
        {
            _directInput = new DirectInput();
        }

        public static bool IsSonyDevice(GamepadDevice? device)
        {
            if (device == null) return false;
            if (device.VendorId == 0x054C) return true;
            string name = device.Name;
            return name.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("DualShock", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Wireless Controller", StringComparison.OrdinalIgnoreCase);
        }

        public List<GamepadDevice> EnumerateDevices()
        {
            var result = new List<GamepadDevice>();
            try
            {
                var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                
                // If GameControl returns nothing, check all devices for gamepads
                if (devices.Count == 0)
                {
                    var all = _directInput.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AttachedOnly);
                    devices = all.Where(d => 
                        d.Type == DeviceType.Gamepad || 
                        d.Type == DeviceType.Joystick || 
                        d.Type == DeviceType.FirstPerson || 
                        d.Type == DeviceType.Driving ||
                        d.Type == DeviceType.Flight).ToList();
                }

                int index = 0;
                foreach (var dev in devices)
                {
                    (int vid, int pid) = ExtractVidPid(dev.ProductGuid);

                    int buttonsCount = 16;
                    int axesCount = 6;
                    int hatsCount = 1;

                    try
                    {
                        using var joy = new Joystick(_directInput, dev.InstanceGuid);
                        var caps = joy.Capabilities;
                        buttonsCount = caps.ButtonCount;
                        axesCount = caps.AxeCount;
                        hatsCount = caps.PovCount;
                    }
                    catch
                    {
                        // Some devices might be locked by another process
                    }

                    result.Add(new GamepadDevice
                    {
                        Index = index++,
                        Name = dev.InstanceName,
                        ProductGuid = dev.ProductGuid,
                        InstanceGuid = dev.InstanceGuid,
                        VendorId = vid,
                        ProductId = pid,
                        ButtonsCount = buttonsCount,
                        AxesCount = axesCount,
                        HatsCount = hatsCount
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DirectInput] Error enumerating devices: {ex.Message}");
            }

            return result;
        }

        public static (int vid, int pid) ExtractVidPid(Guid guid)
        {
            string str = guid.ToString().Replace("-", "");
            if (str.Length >= 8)
            {
                try
                {
                    int pid = Convert.ToInt32(str.Substring(0, 4), 16);
                    int vid = Convert.ToInt32(str.Substring(4, 4), 16);
                    return (vid, pid);
                }
                catch
                {
                    // Ignore parse error
                }
            }
            return (0, 0);
        }

        public Joystick? OpenDevice(GamepadDevice device)
        {
            CloseDevice();
            try
            {
                var joy = new Joystick(_directInput, device.InstanceGuid);
                joy.Properties.BufferSize = 128;
                joy.Acquire();
                _activeJoystick = joy;
                return joy;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DirectInput] Failed to open device {device.Name}: {ex.Message}");
                return null;
            }
        }

        public void CloseDevice()
        {
            StopPolling();
            if (_activeJoystick != null)
            {
                try
                {
                    _activeJoystick.Unacquire();
                    _activeJoystick.Dispose();
                }
                catch
                {
                    // Ignore
                }
                finally
                {
                    _activeJoystick = null;
                }
            }
        }

        /// <summary>
        /// Waits until the user presses a button, moves an axis past threshold, or moves the D-pad.
        /// </summary>
        public async Task<string?> CaptureInputAsync(GamepadDevice device, CancellationToken cancellationToken)
        {
            var joy = OpenDevice(device);
            if (joy == null) return null;

            bool isSony = IsSonyDevice(device);

            try
            {
                joy.Poll();
                var baseState = joy.GetCurrentState();
                int baseX = baseState.X;
                int baseY = baseState.Y;
                int baseZ = baseState.Z;
                int baseRx = baseState.RotationX;
                int baseRy = baseState.RotationY;
                int baseRz = baseState.RotationZ;

                const int stickThreshold = 18000;
                const int triggerThreshold = 12000;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        joy.Poll();
                        var state = joy.GetCurrentState();

                        // 1. Check Buttons
                        for (int i = 0; i < state.Buttons.Length; i++)
                        {
                            if (state.Buttons[i])
                            {
                                return $"pad_button{i}";
                            }
                        }

                        // 2. Check POV / D-Pad
                        if (state.PointOfViewControllers.Length > 0)
                        {
                            int pov = state.PointOfViewControllers[0];
                            if (pov >= 0)
                            {
                                if (pov >= 31500 || pov <= 4500) return "pad_dpadup";
                                if (pov >= 4500 && pov <= 13500) return "pad_dpadright";
                                if (pov >= 13500 && pov <= 22500) return "pad_dpaddown";
                                if (pov >= 22500 && pov <= 31500) return "pad_dpadleft";
                            }
                        }

                        // 3. Check Left Stick (Universal: X, Y)
                        if (state.X - baseX > stickThreshold) return "pad_xaxisplus";
                        if (state.X - baseX < -stickThreshold) return "pad_xaxisminus";
                        if (state.Y - baseY > stickThreshold) return "pad_yaxisplus";
                        if (state.Y - baseY < -stickThreshold) return "pad_yaxisminus";

                        if (isSony)
                        {
                            // Sony DualSense / DualShock 4:
                            // Right Stick = Z (X-axis) and RotationZ (Y-axis)
                            if (state.Z - baseZ > stickThreshold) return "pad_zaxisplus";
                            if (state.Z - baseZ < -stickThreshold) return "pad_zaxisminus";
                            if (state.RotationZ - baseRz > stickThreshold) return "pad_rzaxisplus";
                            if (state.RotationZ - baseRz < -stickThreshold) return "pad_rzaxisminus";

                            // Triggers L2 / R2 = RotationX and RotationY
                            if (state.RotationX - baseRx > triggerThreshold) return "pad_rxaxisplus";
                            if (state.RotationY - baseRy > triggerThreshold) return "pad_ryaxisplus";
                        }
                        else
                        {
                            // Standard Xbox / DirectInput:
                            // Right Stick = RotationX and RotationY
                            if (state.RotationX - baseRx > stickThreshold) return "pad_rxaxisplus";
                            if (state.RotationX - baseRx < -stickThreshold) return "pad_rxaxisminus";
                            if (state.RotationY - baseRy > stickThreshold) return "pad_ryaxisplus";
                            if (state.RotationY - baseRy < -stickThreshold) return "pad_ryaxisminus";

                            // Triggers = Z axis
                            if (state.Z - baseZ > triggerThreshold) return "pad_zaxisplus";
                            if (state.Z - baseZ < -triggerThreshold) return "pad_zaxisminus";
                            if (state.RotationZ - baseRz > triggerThreshold) return "pad_rzaxisplus";
                            if (state.RotationZ - baseRz < -triggerThreshold) return "pad_rzaxisminus";
                        }
                    }
                    catch (SharpDX.SharpDXException)
                    {
                        try { joy.Acquire(); } catch { }
                    }

                    await Task.Delay(25, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            finally
            {
                CloseDevice();
            }

            return null;
        }

        public void StartStreamingState(GamepadDevice device, Action<ControllerStateSnapshot> onStateUpdate)
        {
            StopPolling();

            var joy = OpenDevice(device);
            if (joy == null) return;

            bool isSony = IsSonyDevice(device);
            _pollCts = new CancellationTokenSource();
            var token = _pollCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _activeJoystick != null)
                {
                    try
                    {
                        _activeJoystick.Poll();
                        var state = _activeJoystick.GetCurrentState();

                        double leftX, leftY, rightX, rightY, leftTrig, rightTrig;

                        if (isSony)
                        {
                            // SONY DUALSENSE / DUALSHOCK 4:
                            // Left Stick: X and Y (centered at 32767)
                            leftX = NormalizeStick(state.X);
                            leftY = -NormalizeStick(state.Y);

                            // Right Stick: Z and RotationZ (centered at 32767)
                            rightX = NormalizeStick(state.Z);
                            rightY = -NormalizeStick(state.RotationZ);

                            // Triggers: RotationX (L2) and RotationY (R2) (rest at 0, max 65535)
                            leftTrig = Math.Clamp(state.RotationX / 65535.0, 0.0, 1.0);
                            rightTrig = Math.Clamp(state.RotationY / 65535.0, 0.0, 1.0);

                            // In case digital buttons are clicked, reflect full trigger
                            if (state.Buttons.Length > 6 && state.Buttons[6]) leftTrig = Math.Max(leftTrig, 1.0);
                            if (state.Buttons.Length > 7 && state.Buttons[7]) rightTrig = Math.Max(rightTrig, 1.0);
                        }
                        else
                        {
                            // XBOX / GENERIC PC:
                            // Left Stick: X and Y
                            leftX = NormalizeStick(state.X);
                            leftY = -NormalizeStick(state.Y);

                            // Right Stick: RotationX and RotationY
                            rightX = NormalizeStick(state.RotationX);
                            rightY = -NormalizeStick(state.RotationY);

                            // Triggers: Z axis (0 to 65535, 32767 neutral)
                            leftTrig = state.Z < 30000 ? Math.Clamp((32767.0 - state.Z) / 32767.0, 0.0, 1.0) : 0.0;
                            rightTrig = state.Z > 35500 ? Math.Clamp((state.Z - 32767.0) / 32768.0, 0.0, 1.0) : 0.0;
                        }

                        var snapshot = new ControllerStateSnapshot
                        {
                            Buttons = state.Buttons.ToArray(),
                            LeftStickX = leftX,
                            LeftStickY = leftY,
                            RightStickX = rightX,
                            RightStickY = rightY,
                            LeftTrigger = leftTrig,
                            RightTrigger = rightTrig,
                            PovDirection = state.PointOfViewControllers.Length > 0 ? state.PointOfViewControllers[0] : -1,
                            
                            // Raw DirectInput telemetry
                            RawX = state.X,
                            RawY = state.Y,
                            RawZ = state.Z,
                            RawRx = state.RotationX,
                            RawRy = state.RotationY,
                            RawRz = state.RotationZ
                        };

                        onStateUpdate(snapshot);
                    }
                    catch (SharpDX.SharpDXException)
                    {
                        try { _activeJoystick?.Acquire(); } catch { }
                    }
                    catch
                    {
                        break;
                    }

                    await Task.Delay(20, token);
                }
            }, token);
        }

        public void StopPolling()
        {
            if (_pollCts != null)
            {
                _pollCts.Cancel();
                _pollCts.Dispose();
                _pollCts = null;
            }
        }

        private static double NormalizeStick(int raw)
        {
            // DirectInput axes are 0 to 65535, with center around 32767
            double norm = (raw - 32767.5) / 32767.5;
            if (Math.Abs(norm) < 0.06) return 0.0; // Deadzone
            return Math.Clamp(norm, -1.0, 1.0);
        }

        public void Dispose()
        {
            CloseDevice();
            _directInput.Dispose();
        }
    }
}