using System.Windows.Input;

namespace MgsPadEditor.Services
{
    public static class KeyboardMouseCaptureService
    {
        public static string? ConvertKeyToDixi(Key key)
        {
            return key switch
            {
                Key.Up => "kb_up",
                Key.Down => "kb_down",
                Key.Left => "kb_left",
                Key.Right => "kb_right",
                Key.Space => "kb_space",
                Key.LeftCtrl => "kb_lcontrol",
                Key.RightCtrl => "kb_rcontrol",
                Key.LeftShift => "kb_lshift",
                Key.RightShift => "kb_lshift",
                Key.LeftAlt => "kb_lalt",
                Key.RightAlt => "kb_ralt",
                Key.Return => "kb_return",
                Key.Escape => "kb_escape",
                Key.Tab => "kb_tab",
                Key.A => "kb_a",
                Key.B => "kb_b",
                Key.C => "kb_c",
                Key.D => "kb_d",
                Key.E => "kb_e",
                Key.F => "kb_f",
                Key.G => "kb_g",
                Key.H => "kb_h",
                Key.I => "kb_i",
                Key.J => "kb_j",
                Key.K => "kb_k",
                Key.L => "kb_l",
                Key.M => "kb_m",
                Key.N => "kb_n",
                Key.O => "kb_o",
                Key.P => "kb_p",
                Key.Q => "kb_q",
                Key.R => "kb_r",
                Key.S => "kb_s",
                Key.T => "kb_t",
                Key.U => "kb_u",
                Key.V => "kb_v",
                Key.W => "kb_w",
                Key.X => "kb_x",
                Key.Y => "kb_y",
                Key.Z => "kb_z",
                Key.D0 => "kb_0",
                Key.D1 => "kb_1",
                Key.D2 => "kb_2",
                Key.D3 => "kb_3",
                Key.D4 => "kb_4",
                Key.D5 => "kb_5",
                Key.D6 => "kb_6",
                Key.D7 => "kb_7",
                Key.D8 => "kb_8",
                Key.D9 => "kb_9",
                Key.F1 => "kb_f1",
                Key.F2 => "kb_f2",
                Key.F3 => "kb_f3",
                Key.F4 => "kb_f4",
                Key.F5 => "kb_f5",
                Key.F6 => "kb_f6",
                Key.F7 => "kb_f7",
                Key.F8 => "kb_f8",
                Key.F9 => "kb_f9",
                Key.F10 => "kb_f10",
                Key.F11 => "kb_f11",
                Key.F12 => "kb_f12",
                _ => null
            };
        }

        public static string? ConvertMouseButtonToDixi(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => "ms_button0",
                MouseButton.Right => "ms_button1",
                MouseButton.Middle => "ms_button2",
                _ => null
            };
        }
    }
}
