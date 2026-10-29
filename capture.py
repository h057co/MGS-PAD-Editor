import time
import struct

try:
    import pygame
    PYGAME_AVAILABLE = True
except ImportError:
    PYGAME_AVAILABLE = False

_JOYSTICK_INITIALIZED = False

if PYGAME_AVAILABLE:
    import os as _os
    _os.environ["SDL_VIDEODRIVER"] = "windows"
    pygame.display.quit()
    pygame.init()
    pygame.display.set_mode((1, 1))
    del _os


def ensure_joystick():
    global _JOYSTICK_INITIALIZED
    if not _JOYSTICK_INITIALIZED:
        pygame.joystick.quit()
        pygame.joystick.init()
        time.sleep(0.05)
        _JOYSTICK_INITIALIZED = True


def extract_vid_pid_pairs_from_sdl_guid(guid_hex):
    try:
        data = bytes.fromhex(guid_hex)
        if len(data) < 10:
            return []
        pairs = []
        pairs.append((struct.unpack_from('<H', data, 2)[0], struct.unpack_from('<H', data, 4)[0]))
        pairs.append((struct.unpack_from('<H', data, 4)[0], struct.unpack_from('<H', data, 8)[0]))
        pairs.append((struct.unpack_from('<H', data, 2)[0], struct.unpack_from('<H', data, 8)[0]))
        pairs.append((struct.unpack_from('<H', data, 4)[0], struct.unpack_from('<H', data, 6)[0]))
        return pairs
    except Exception:
        return []


def extract_vid_pid_from_dixi_guid(dixi_guid):
    if not dixi_guid:
        return None, None
    first = dixi_guid.split('-')[0]
    if len(first) >= 8:
        try:
            pid = int(first[0:4], 16)
            vid = int(first[4:8], 16)
            return vid, pid
        except ValueError:
            pass
    return None, None


def get_axis_mapping(joystick):
    naxes = joystick.get_numaxes()
    mapping = {}
    if naxes >= 2:
        mapping[(0, True)] = "pad_xaxisplus"
        mapping[(0, False)] = "pad_xaxisminus"
        mapping[(1, True)] = "pad_yaxisplus"
        mapping[(1, False)] = "pad_yaxisminus"
    if naxes >= 4:
        mapping[(2, True)] = "pad_rxaxisplus"
        mapping[(2, False)] = "pad_rxaxisminus"
        mapping[(3, True)] = "pad_ryaxisplus"
        mapping[(3, False)] = "pad_ryaxisminus"
    if naxes >= 5:
        mapping[(4, True)] = "pad_zaxisplus"
        mapping[(4, False)] = "pad_zaxisminus"
    if naxes >= 6:
        mapping[(5, True)] = "pad_rzaxisplus"
        mapping[(5, False)] = "pad_rzaxisminus"
    return mapping


HAT_NAMES = {
    (0, 1): "pad_dpadup",
    (1, 0): "pad_dpadright",
    (0, -1): "pad_dpaddown",
    (-1, 0): "pad_dpadleft",
}


class GamepadCapture:
    def __init__(self):
        self._device_index = 0
        self._joystick_idx = -1
        self._joystick = None
        self._dualsense = None
        self._ds_listening = False
        self._ds_events = []
        self._ds_event_lock = None
        if PYGAME_AVAILABLE:
            ensure_joystick()

    def _init_dualsense(self):
        if self._dualsense is None:
            try:
                from dualsense_hid import DualsenseReader
                self._dualsense = DualsenseReader()
                devs = self._dualsense.list_devices()
                if devs:
                    self._dualsense.open_device(0)
                    self._ds_events = []
                    import threading
                    self._ds_event_lock = threading.Lock()
            except Exception:
                self._dualsense = None
        return self._dualsense

    def _ds_callback(self, event_tuple, state):
        if self._ds_event_lock:
            with self._ds_event_lock:
                self._ds_events.append(event_tuple)

    def list_devices_full(self):
        devices = []
        self._device_sources = {}

        ds = self._init_dualsense()
        if ds:
            for dd in ds.list_devices():
                idx = len(devices)
                devices.append({
                    "index": idx,
                    "name": dd["name"],
                    "guid": "",
                    "vid": dd["vid"],
                    "pid": dd["pid"],
                    "buttons": 17,
                    "axes": 6,
                    "hats": 1,
                })
                self._device_sources[idx] = "dualsense"

        if PYGAME_AVAILABLE:
            try:
                ensure_joystick()
                for i in range(pygame.joystick.get_count()):
                    joy = pygame.joystick.Joystick(i)
                    joy.init()
                    guid = joy.get_guid()
                    pairs = extract_vid_pid_pairs_from_sdl_guid(guid)
                    vid = pairs[0][0] if pairs else None
                    pid = pairs[0][1] if pairs else None
                    name = joy.get_name()

                    skip = False
                    for dev in devices:
                        if dev.get("name") == name:
                            skip = True
                            break
                    if not skip:
                        idx = len(devices)
                        devices.append({
                            "index": idx,
                            "name": name,
                            "guid": guid,
                            "vid": vid,
                            "pid": pid,
                            "buttons": joy.get_numbuttons(),
                            "axes": joy.get_numaxes(),
                            "hats": joy.get_numhats(),
                        })
                        self._device_sources[idx] = "pygame"
            except Exception:
                pass

        return devices

    def select_device(self, index):
        self._device_index = index
        self._joystick = None
        self._joystick_idx = -1

    def _is_dualsense(self, device_index):
        return self._device_sources.get(device_index) == "dualsense"

    def poll_event(self, device_index=None):
        idx = device_index if device_index is not None else self._device_index
        if self._is_dualsense(idx):
            self.start_capture()
            if self._ds_event_lock:
                with self._ds_event_lock:
                    if self._ds_events:
                        ev = self._ds_events.pop(0)
                        if ev[0] == "button":
                            return ("capture", ev[2])
                        elif ev[0] == "axis":
                            return ("capture", ev[3])
                        elif ev[0] == "hat":
                            return ("capture", ev[2])
            return None

        if not PYGAME_AVAILABLE:
            return None
        try:
            self._init_joystick(idx)
            events = pygame.event.get()
            return self._process_events(events, idx)
        except Exception:
            return None

    def poll_test_event(self, device_index=None):
        idx = device_index if device_index is not None else self._device_index
        if self._is_dualsense(idx):
            self.start_capture()
            if self._ds_event_lock:
                with self._ds_event_lock:
                    if self._ds_events:
                        ev = self._ds_events.pop(0)
                        if ev[0] == "button":
                            return ("button", ev[1], ev[2])
                        elif ev[0] == "axis":
                            return ("axis", ev[1], ev[2], ev[3])
                        elif ev[0] == "hat":
                            return ("hat", ev[1], ev[2])
            return None

        if not PYGAME_AVAILABLE:
            return None
        try:
            self._init_joystick(idx)
            events = pygame.event.get()
            return self._process_test_events(events, idx)
        except Exception:
            return None

    def _process_events(self, events, device_index):
        for event in events:
            if event.type == pygame.JOYBUTTONDOWN:
                return ("capture", f"pad_button{event.button}")
            elif event.type == pygame.JOYAXISMOTION:
                if abs(event.value) > 0.8:
                    joy = self._init_joystick(device_index)
                    am = get_axis_mapping(joy) if joy else {}
                    name = am.get((event.axis, event.value > 0))
                    if name:
                        return ("capture", name)
            elif event.type == pygame.JOYHATMOTION:
                if event.value != (0, 0):
                    name = HAT_NAMES.get(event.value)
                    if name:
                        return ("capture", name)
        return None

    def _process_test_events(self, events, device_index):
        for event in events:
            if event.type == pygame.JOYBUTTONDOWN:
                return ("button", event.button, "pressed")
            elif event.type == pygame.JOYAXISMOTION:
                if abs(event.value) > 0.4:
                    joy = self._init_joystick(device_index)
                    am = get_axis_mapping(joy) if joy else {}
                    name = am.get((event.axis, event.value > 0))
                    return ("axis", event.axis, event.value, name)
            elif event.type == pygame.JOYHATMOTION:
                if event.value != (0, 0):
                    name = HAT_NAMES.get(event.value)
                    return ("hat", event.value, name)
        return None

    def _init_joystick(self, idx=None):
        if not PYGAME_AVAILABLE:
            return None
        i = idx if idx is not None else self._device_index
        if self._joystick_idx != i:
            self._joystick = None
        if self._joystick is None:
            try:
                if pygame.joystick.get_count() > i:
                    self._joystick = pygame.joystick.Joystick(i)
                    self._joystick.init()
                    self._joystick_idx = i
            except Exception:
                return None
        return self._joystick

    def start_capture(self):
        if self._dualsense and not self._ds_listening:
            if self._ds_event_lock:
                with self._ds_event_lock:
                    self._ds_events.clear()
            self._ds_listening = True
            return self._dualsense.start_listening(self._ds_callback)
        return True

    def stop_capture(self):
        if self._dualsense and self._ds_listening:
            self._dualsense.stop_listening()
            self._ds_listening = False
            if self._ds_event_lock:
                with self._ds_event_lock:
                    self._ds_events.clear()


def capture_keyboard_on_widget(widget, callback):
    def handler(event):
        from mapping_defs import KB_KEY_MAP
        name = KB_KEY_MAP.get(event.keysym)
        if not name and len(event.keysym) == 1:
            name = f"kb_{event.keysym.lower()}"
        if name:
            widget.unbind("<Key>")
            callback(name, None)
    widget.bind("<Key>", handler)
    widget.focus_set()


def capture_mouse_on_widget(widget, callback):
    def handler(event):
        name_map = {1: "ms_button0", 2: "ms_button1", 3: "ms_button2"}
        name = name_map.get(event.num)
        if name:
            widget.unbind("<Button>")
            callback(name, None)
    widget.bind("<Button>", handler)
