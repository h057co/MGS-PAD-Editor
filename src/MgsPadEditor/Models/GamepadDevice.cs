using System;

namespace MgsPadEditor.Models
{
    public class GamepadDevice
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ProductGuid { get; set; }
        public Guid InstanceGuid { get; set; }
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public int ButtonsCount { get; set; }
        public int AxesCount { get; set; }
        public int HatsCount { get; set; }

        /// <summary>
        /// DirectInput GUID format used by dixi.ini: PPPPVVVV-0000-0000-0000-504944564944
        /// </summary>
        public string DixiGuid
        {
            get
            {
                if (VendorId > 0 && ProductId > 0)
                {
                    return $"{ProductId:X4}{VendorId:X4}-0000-0000-0000-504944564944";
                }
                return ProductGuid.ToString().ToUpperInvariant();
            }
        }

        public string DisplayLabel => $"[{Index}] {Name} (VID:{VendorId:X4} PID:{ProductId:X4})";

        public override string ToString() => DisplayLabel;
    }
}
