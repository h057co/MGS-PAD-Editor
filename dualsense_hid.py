import ctypes
import ctypes.wintypes
import threading

INVALID_HANDLE_VALUE = ctypes.c_void_p(-1).value
FILE_SHARE_READ   = 0x00000001
FILE_SHARE_WRITE  = 0x00000002
OPEN_EXISTING     = 3
GENERIC_READ      = 0x80000000
FILE_FLAG_OVERLAPPED = 0x40000000

DIGCF_PRESENT         = 0x00000002
DIGCF_DEVICEINTERFACE = 0x00000010

class GUID(ctypes.Structure):
    _fields_ = [("Data1", ctypes.c_ulong),
                ("Data2", ctypes.c_ushort),
                ("Data3", ctypes.c_ushort),
                ("Data4", ctypes.c_ubyte * 8)]

HID_GUID = GUID(0x4D1E55B2, 0xF16F, 0x11CF,
                (ctypes.c_ubyte * 8)(0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30))

class HIDD_ATTRIBUTES(ctypes.Structure):
    _fields_ = [("Size", ctypes.c_ulong),
                ("VendorID", ctypes.c_ushort),
                ("ProductID", ctypes.c_ushort),
                ("VersionNumber", ctypes.c_ushort)]

class SP_DEVICE_INTERFACE_DATA(ctypes.Structure):
    _fields_ = [("cbSize", ctypes.c_ulong),
                ("InterfaceClassGuid", GUID),
                ("Flags", ctypes.c_ulong),
                ("Reserved", ctypes.c_void_p)]

class OVERLAPPED(ctypes.Structure):
    _fields_ = [("Internal", ctypes.c_void_p),
                ("InternalHigh", ctypes.c_void_p),
                ("Pointer", ctypes.c_void_p),
                ("hEvent", ctypes.c_void_p)]

_setupapi = ctypes.WinDLL("setupapi", use_last_error=True)
_kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
_hid = ctypes.WinDLL("hid", use_last_error=True)

SetupDiGetClassDevsW = _setupapi.SetupDiGetClassDevsW
SetupDiGetClassDevsW.argtypes = [ctypes.POINTER(GUID), ctypes.c_wchar_p,
                                 ctypes.c_void_p, ctypes.c_ulong]
SetupDiGetClassDevsW.restype = ctypes.c_void_p

SetupDiEnumDeviceInterfaces = _setupapi.SetupDiEnumDeviceInterfaces
SetupDiEnumDeviceInterfaces.argtypes = [ctypes.c_void_p, ctypes.c_void_p,
                                        ctypes.POINTER(GUID), ctypes.c_ulong,
                                        ctypes.POINTER(SP_DEVICE_INTERFACE_DATA)]
SetupDiEnumDeviceInterfaces.restype = ctypes.c_bool

SetupDiGetDeviceInterfaceDetailW = _setupapi.SetupDiGetDeviceInterfaceDetailW
SetupDiGetDeviceInterfaceDetailW.argtypes = [ctypes.c_void_p,
                                             ctypes.POINTER(SP_DEVICE_INTERFACE_DATA),
                                             ctypes.c_void_p, ctypes.c_ulong,
                                             ctypes.POINTER(ctypes.c_ulong),
                                             ctypes.c_void_p]
SetupDiGetDeviceInterfaceDetailW.restype = ctypes.c_bool

SetupDiDestroyDeviceInfoList = _setupapi.SetupDiDestroyDeviceInfoList
SetupDiDestroyDeviceInfoList.argtypes = [ctypes.c_void_p]
SetupDiDestroyDeviceInfoList.restype = ctypes.c_bool

CreateFileW = _kernel32.CreateFileW
CreateFileW.argtypes = [ctypes.c_wchar_p, ctypes.c_ulong, ctypes.c_ulong,
                        ctypes.c_void_p, ctypes.c_ulong, ctypes.c_ulong,
                        ctypes.c_void_p]
CreateFileW.restype = ctypes.c_void_p

CloseHandle = _kernel32.CloseHandle
CloseHandle.argtypes = [ctypes.c_void_p]
CloseHandle.restype = ctypes.c_bool

ReadFile = _kernel32.ReadFile
ReadFile.argtypes = [ctypes.c_void_p, ctypes.c_void_p, ctypes.c_ulong,
                     ctypes.POINTER(ctypes.c_ulong), ctypes.c_void_p]
ReadFile.restype = ctypes.c_bool

GetLastError = _kernel32.GetLastError
GetLastError.restype = ctypes.c_ulong

GetOverlappedResult = _kernel32.GetOverlappedResult
GetOverlappedResult.argtypes = [ctypes.c_void_p, ctypes.POINTER(OVERLAPPED),
                                ctypes.POINTER(ctypes.c_ulong), ctypes.c_bool]
GetOverlappedResult.restype = ctypes.c_bool

WaitForSingleObject = _kernel32.WaitForSingleObject
WaitForSingleObject.argtypes = [ctypes.c_void_p, ctypes.c_ulong]
WaitForSingleObject.restype = ctypes.c_ulong

CreateEventW = _kernel32.CreateEventW
CreateEventW.argtypes = [ctypes.c_void_p, ctypes.c_bool, ctypes.c_bool, ctypes.c_wchar_p]
CreateEventW.restype = ctypes.c_void_p

ResetEvent = _kernel32.ResetEvent
ResetEvent.argtypes = [ctypes.c_void_p]
ResetEvent.restype = ctypes.c_bool

HidD_GetAttributes = _hid.HidD_GetAttributes
HidD_GetAttributes.argtypes = [ctypes.c_void_p, ctypes.POINTER(HIDD_ATTRIBUTES)]
HidD_GetAttributes.restype = ctypes.c_bool

# SP_DEVICE_INTERFACE_DETAIL_DATA_W for enumerating device paths
class _DEVICE_DETAIL(ctypes.Structure):
    _fields_ = [("cbSize", ctypes.c_ulong),
                ("DevicePath", ctypes.c_wchar * 512)]

ERROR_IO_PENDING = 997
ERROR_INSUFFICIENT_BUFFER = 122
WAIT_OBJECT_0 = 0
WAIT_TIMEOUT = 258

DUALSENSE_VID = 0x054c
REPORT_SIZE = 78

BTN_NAMES = [
    "pad_button0", "pad_button1", "pad_button2", "pad_button3",
    "pad_button4", "pad_button5", "pad_button6", "pad_button7",
    "pad_button8", "pad_button9", "pad_button10", "pad_button11",
    "pad_button12", "pad_button13", "pad_button14",
]

HAT_MAP = {
    0: "pad_dpadup",    1: "pad_dpadup",
    2: "pad_dpadright", 3: "pad_dpadright",
    4: "pad_dpaddown",  5: "pad_dpaddown",
    6: "pad_dpadleft",  7: "pad_dpadleft",
}

STICK_MAP = [
    ("pad_xaxisminus",   "pad_xaxisplus"),
    ("pad_yaxisminus",   "pad_yaxisplus"),
    ("pad_rxaxisminus",  "pad_rxaxisplus"),
    ("pad_ryaxisminus",  "pad_ryaxisplus"),
]

STICK_THRESHOLD = 0.5

# Button bit positions per byte:
# Byte 0 (b0): D-pad bits 0-3, Triangle[7] Circle[6] Cross[5] Square[4]
# Byte 1 (b1): L1[0] R1[1] L2[2] R2[3] Share[4] Options[5] L3[6] R3[7]
# Byte 2 (b2): PS[0] Touchpad[1] Mic[2]
BTN_BITS = [
    (0, 7, 0), (0, 6, 1), (0, 5, 2), (0, 4, 3),
    (1, 0, 4), (1, 1, 5), (1, 2, 6), (1, 3, 7),
    (1, 4, 8), (1, 5, 9), (1, 6, 10), (1, 7, 11),
    (2, 0, 12), (2, 1, 13), (2, 2, 14),
]

# USB: button bytes at 8,9,10 ; active-LOW (0=pressed, 1=released) ; D-pad in low nibble of byte 8
# Bluetooth: button bytes at 5,6,8  ; active-HIGH (1=pressed, 0=released) ; byte 7 is sequence
LAYOUT_USB = (8, 9, 10, False)   # (b0_off, b1_off, b2_off, active_high)
LAYOUT_BT  = (5, 6, 8, True)     # Bluetooth


import re as _re


def _parse_vid_pid_from_path(path):
    # USB HID format: vid_054c&pid_0ce6
    m = _re.search(r'vid_([0-9a-fA-F]{4}).*?pid_([0-9a-fA-F]{4})', path)
    if m:
        return int(m.group(1), 16), int(m.group(2), 16)
    # Bluetooth HID format: _vid&XXXXXXXX_pid&XXXX
    m = _re.search(r'_vid&[0-9a-fA-F]*?(0?[0-9a-fA-F]{4})_pid&([0-9a-fA-F]{4})', path)
    if m:
        return int(m.group(1), 16), int(m.group(2), 16)
    # Fallback: just check for 054c VID
    if '054c' in path.lower():
        m = _re.search(r'_pid&([0-9a-fA-F]{4})', path)
        if m:
            return 0x054c, int(m.group(1), 16)
        m = _re.search(r'pid_([0-9a-fA-F]{4})', path)
        if m:
            return 0x054c, int(m.group(2), 16)
    return None, None


def _enumerate_hid_device_paths():
    """Enumerate all HID devices. Returns list of (device_path, vid, pid)."""
    devinfo = SetupDiGetClassDevsW(
        ctypes.byref(HID_GUID), None, None,
        DIGCF_PRESENT | DIGCF_DEVICEINTERFACE)
    if devinfo == INVALID_HANDLE_VALUE:
        return []

    results = []
    try:
        for idx in range(256):
            dev_iface = SP_DEVICE_INTERFACE_DATA()
            dev_iface.cbSize = ctypes.sizeof(SP_DEVICE_INTERFACE_DATA)
            if not SetupDiEnumDeviceInterfaces(
                    devinfo, None, ctypes.byref(HID_GUID), idx,
                    ctypes.byref(dev_iface)):
                break

            required = ctypes.c_ulong(0)
            SetupDiGetDeviceInterfaceDetailW(
                devinfo, ctypes.byref(dev_iface), None, 0,
                ctypes.byref(required), None)
            if ctypes.get_last_error() != ERROR_INSUFFICIENT_BUFFER:
                continue

            if required.value > ctypes.sizeof(_DEVICE_DETAIL):
                continue

            detail = _DEVICE_DETAIL()
            detail.cbSize = 8  # On x64, cbSize must be 8

            if not SetupDiGetDeviceInterfaceDetailW(
                    devinfo, ctypes.byref(dev_iface),
                    ctypes.byref(detail), ctypes.sizeof(_DEVICE_DETAIL),
                    ctypes.byref(required), None):
                continue

            path = detail.DevicePath
            if not path:
                continue

            vid, pid = _parse_vid_pid_from_path(path)
            if vid is not None:
                results.append((path, vid, pid))
    finally:
        SetupDiDestroyDeviceInfoList(devinfo)

    return results


class DualsenseReader:
    def __init__(self):
        self._handle = None
        self._path = None
        self._thread = None
        self._running = False
        self._callback = None
        self._prev = {}

    def list_devices(self):
        devs = _enumerate_hid_device_paths()
        filtered = [d for d in devs if d[1] == DUALSENSE_VID]
        return [{"index": i, "vid": d[1], "pid": d[2],
                 "name": f"DualSense (VID:{d[1]:04X} PID:{d[2]:04X})"}
                for i, d in enumerate(filtered)]

    def open_device(self, index=0):
        self.close()
        devs = _enumerate_hid_device_paths()
        filtered = [d for d in devs if d[1] == DUALSENSE_VID]
        if index >= len(filtered):
            return False
        path = filtered[index][0]
        h = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
                        None, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, None)
        if h == INVALID_HANDLE_VALUE:
            return False
        self._handle = h
        self._path = path
        return True

    def close(self):
        self.stop_listening()
        if self._handle:
            CloseHandle(self._handle)
            self._handle = None
            self._path = None

    def start_listening(self, callback):
        if not self._handle or self._running:
            return False
        self._running = True
        self._callback = callback
        self._prev = {}
        self._thread = threading.Thread(target=self._read_loop, daemon=True)
        self._thread.start()
        return True

    def stop_listening(self):
        self._running = False
        if self._thread:
            self._thread.join(timeout=1)
            self._thread = None

    def _read_loop(self):
        buf = (ctypes.c_ubyte * REPORT_SIZE)()
        overlapped = OVERLAPPED()
        event_handle = CreateEventW(None, True, False, None)
        overlapped.hEvent = event_handle

        while self._running:
            bytes_read = ctypes.c_ulong(0)
            buf[:] = (ctypes.c_ubyte * REPORT_SIZE)()

            if ReadFile(self._handle, ctypes.byref(buf), REPORT_SIZE,
                        ctypes.byref(bytes_read), ctypes.byref(overlapped)):
                if bytes_read.value > 0:
                    self._process(bytes(buf[:bytes_read.value]))
            else:
                err = GetLastError()
                if err == ERROR_IO_PENDING:
                    while self._running:
                        res = WaitForSingleObject(event_handle, 50)
                        if res == WAIT_OBJECT_0:
                            bytes_read = ctypes.c_ulong(0)
                            if GetOverlappedResult(
                                    self._handle, ctypes.byref(overlapped),
                                    ctypes.byref(bytes_read), False):
                                if bytes_read.value > 0:
                                    self._process(bytes(buf[:bytes_read.value]))
                            break
                        elif res == WAIT_TIMEOUT:
                            continue
                        else:
                            break
                    ResetEvent(event_handle)
                else:
                    break

        CloseHandle(event_handle)

    def _detect_layout(self, data):
        if len(data) < 11:
            return None
        # Bluetooth: button bytes at 5,6,8 ; byte 8 is Button[2] (PS/touchpad/mic only) ≤ 7
        dp_bt = data[5] & 0x0F
        b2_bt = data[8]
        if dp_bt <= 8 and b2_bt <= 7:
            return LAYOUT_BT
        # USB: button bytes at 8,9,10 ; D-pad in low nibble of byte 8
        dp_usb = data[8] & 0x0F
        if dp_usb <= 8:
            return LAYOUT_USB
        return None

    def _process(self, data):
        if len(data) < 11:
            return
        cb = self._callback
        if not cb:
            return
        p = self._prev

        layout = self._detect_layout(data)
        if layout is None:
            return

        b0_off, b1_off, b2_off, active_high = layout

        if len(data) <= max(b0_off, b1_off, b2_off):
            return

        b0 = data[b0_off]
        b1 = data[b1_off]
        b2 = data[b2_off]

        dp = b0 & 0x0F
        old_dp = p.get("dp", 8)
        if dp != old_dp:
            if dp < 8 and old_dp == 8:
                name = HAT_MAP.get(dp)
                if name:
                    cb(("hat", dp, name), p)
            elif dp == 8 and old_dp < 8:
                pass
            p["dp"] = dp

        btn_bytes = [b0, b1, b2]
        for byte_idx, bit, ni in BTN_BITS:
            raw = bool(btn_bytes[byte_idx] & (1 << bit))
            pressed = raw if active_high else not raw
            k = f"b{byte_idx}_{bit}"
            if pressed and not p.get(k):
                cb(("button", ni, BTN_NAMES[ni]), p)
            p[k] = pressed

        if len(data) >= 5:
            for a_idx in range(4):
                val = data[1 + a_idx]
                np_val = val / 255.0 * 2 - 1
                pv = p.get(f"s{a_idx}", 128)
                pn_val = pv / 255.0 * 2 - 1
                if abs(np_val) > STICK_THRESHOLD and abs(pn_val) <= STICK_THRESHOLD:
                    mn, pn = STICK_MAP[a_idx]
                    name = pn if np_val > 0 else mn
                    cb(("axis", a_idx, np_val, name), p)
                p[f"s{a_idx}"] = val
