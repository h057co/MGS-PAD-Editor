import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import os
import time
from mapping_defs import *
from ini_model import IniFile
from capture import (GamepadCapture, capture_keyboard_on_widget,
                     capture_mouse_on_widget,
                     extract_vid_pid_from_dixi_guid, PYGAME_AVAILABLE)


class MappingTable(tk.Frame):
    def __init__(self, parent, target_options,
                 enable_capture=False, capture_icon="🎮",
                 capture_callback=None):
        super().__init__(parent)
        self.target_options = target_options
        self.enable_capture = enable_capture
        self.capture_icon = capture_icon
        self.capture_callback = capture_callback
        self.current_section = None
        self.comboboxes = {}
        self.ini = None

        ttk.Label(self, text="Game Input", font=("", 9, "bold"), width=18, anchor=tk.W).grid(row=0, column=0, padx=2)
        ttk.Label(self, text="Description", font=("", 9, "bold"), width=28, anchor=tk.W).grid(row=0, column=1, padx=2)
        ttk.Label(self, text="Target", font=("", 9, "bold"), width=22, anchor=tk.W).grid(row=0, column=2, padx=2)
        if self.enable_capture:
            ttk.Label(self, text="Capture", font=("", 9, "bold"), width=6).grid(row=0, column=3, padx=2)

        canvas = tk.Canvas(self, highlightthickness=0, bg="#f0f0f0")
        scrollbar = tk.Scrollbar(self, orient=tk.VERTICAL, command=canvas.yview)
        self.inner = tk.Frame(canvas, bg="#f0f0f0")
        self.inner.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))
        canvas.create_window((0, 0), window=self.inner, anchor=tk.NW)
        canvas.configure(yscrollcommand=scrollbar.set)

        canvas.grid(row=1, column=0, columnspan=4, sticky="nsew")
        scrollbar.grid(row=1, column=4, sticky="ns")
        self.grid_rowconfigure(1, weight=1)
        self.grid_columnconfigure(1, weight=1)

    def load_lines(self, section_lines):
        self._lines = section_lines
        self._section_name = None
        self.ini = None
        self._rebuild()

    def load_section(self, ini, section_name):
        self.ini = ini
        self._section_name = section_name
        self._lines = None
        self._rebuild()

    def _get_mappings(self):
        if self._lines is not None:
            from collections import OrderedDict
            result = OrderedDict()
            for item in self._lines:
                if item[0] == "kv":
                    result[item[1]] = item[2]
            return result
        if self._section_name and self.ini:
            return self.ini.get_all_kv(self._section_name)
        return {}

    def _rebuild(self):
        for w in self.inner.winfo_children():
            w.destroy()
        self.comboboxes = {}

        if self._lines is None and (self._section_name is None or self.ini is None):
            return

        mappings = self._get_mappings()
        sorted_inputs = sorted(ALL_PAD_INPUTS, key=lambda x: SORT_ORDER.get(x, 99))

        for i, inp in enumerate(sorted_inputs):
            bg = "#ffffff" if i % 2 == 0 else "#f5f5f5"
            row = tk.Frame(self.inner, bg=bg)
            row.pack(fill=tk.X)

            current_val = mappings.get(inp, "")

            tk.Label(row, text=inp, width=18, anchor=tk.W, bg=bg, font=("Consolas", 9)).pack(side=tk.LEFT, padx=2)
            tk.Label(row, text=GAME_INPUT_LABELS.get(inp, ""), width=28, anchor=tk.W,
                     bg=bg, font=("", 8), fg="#555555").pack(side=tk.LEFT, padx=2)

            var = tk.StringVar(value=current_val if current_val else "")
            cb = ttk.Combobox(row, textvariable=var, values=self.target_options,
                              width=24, state="readonly")
            cb.pack(side=tk.LEFT, padx=2)
            cb.bind("<<ComboboxSelected>>", lambda e, k=inp, v=var: self._on_change(k, v.get()))
            self.comboboxes[inp] = (var, cb)

            if self.enable_capture and self.capture_callback:
                btn = tk.Button(row, text=self.capture_icon, width=4,
                                command=lambda i=inp: self.capture_callback(i, self._set_value))
                btn.pack(side=tk.LEFT, padx=2)

    def _on_change(self, key, value):
        if self._lines is not None:
            if value:
                for i, item in enumerate(self._lines):
                    if item[0] == "kv" and item[1] == key:
                        self._lines[i] = ("kv", key, value)
                        return
                self._lines.append(("kv", key, value))
            else:
                self._lines[:] = [item for item in self._lines if not (item[0] == "kv" and item[1] == key)]
        elif self._section_name and self.ini:
            if value:
                self.ini.set_value(self._section_name, key, value)
            else:
                self.ini.remove_value(self._section_name, key)

    def _set_value(self, key, value):
        if key in self.comboboxes:
            var, cb = self.comboboxes[key]
            var.set(value)
            self._on_change(key, value)


class MgsPadEditor:
    def __init__(self, root):
        self.root = root
        self.root.title("MGS PAD Editor")
        self.root.geometry("1024x700")
        self.root.minsize(800, 500)

        self.ini = IniFile()
        self.current_file = None
        self.modified = False
        self.gamepad = GamepadCapture()
        self._devices = []

        self._create_menu()
        self._create_widgets()
        self._create_statusbar()

        self.root.protocol("WM_DELETE_WINDOW", self._on_exit)

    def _create_menu(self):
        mb = tk.Menu(self.root)
        self.root.config(menu=mb)

        fm = tk.Menu(mb, tearoff=0)
        mb.add_cascade(label="File", menu=fm)
        fm.add_command(label="Open...", command=self.open_file, accelerator="Ctrl+O")
        fm.add_command(label="Save", command=self.save_file, accelerator="Ctrl+S")
        fm.add_command(label="Save As...", command=self.save_as_file)
        fm.add_separator()
        fm.add_command(label="Exit", command=self._on_exit)

        hm = tk.Menu(mb, tearoff=0)
        mb.add_cascade(label="Help", menu=hm)
        hm.add_command(label="About", command=self._show_about)

        self.root.bind("<Control-o>", lambda e: self.open_file())
        self.root.bind("<Control-s>", lambda e: self.save_file())

    def _create_statusbar(self):
        self.status = ttk.Label(self.root, text="Ready", relief=tk.SUNKEN, anchor=tk.W)
        self.status.pack(fill=tk.X, side=tk.BOTTOM)

    def set_status(self, msg):
        self.status.config(text=msg)

    def _create_widgets(self):
        self.notebook = ttk.Notebook(self.root)
        self.notebook.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        self._build_general_tab()
        self._build_controller_tab()
        self._build_keyboard_tab()
        self._build_mouse_tab()

    def _build_general_tab(self):
        self._general_frame = ttk.Frame(self.notebook)
        self.notebook.add(self._general_frame, text=" General ")

        self._gen_vars = {}

        row = 0
        for setting in ("onelayout", "dpadtoanalog", "keyboardandmouse"):
            var = tk.StringVar(value="FALSE")
            cb = ttk.Checkbutton(self._general_frame, text=setting, variable=var,
                                 onvalue="TRUE", offvalue="FALSE",
                                 command=lambda s=setting, v=var: self._on_gen_change(s, v.get()))
            cb.grid(row=row, column=0, sticky=tk.W, padx=15, pady=3)
            self._gen_vars[setting] = var
            row += 1

        row += 1
        ttk.Label(self._general_frame, text="Mouse Speed:").grid(row=row, column=0, sticky=tk.W, padx=15, pady=5)
        self._speed_var = tk.IntVar(value=5)
        sp = ttk.Spinbox(self._general_frame, from_=0, to=10, textvariable=self._speed_var,
                         width=5, command=self._on_speed_change)
        sp.grid(row=row, column=1, sticky=tk.W)

    def _on_gen_change(self, key, value):
        self.ini.set_value("general", key, value)
        self.modified = True
        self.set_status(f"Changed: {key} = {value}")

    def _on_speed_change(self):
        val = str(self._speed_var.get())
        self.ini.set_value("general", "mousespeed", val)
        self.modified = True

    def _build_controller_tab(self):
        self._ctrl_frame = ttk.Frame(self.notebook)
        self.notebook.add(self._ctrl_frame, text=" Controller ")

        top = ttk.Frame(self._ctrl_frame)
        top.pack(fill=tk.X, pady=5)

        ttk.Label(top, text="Profile:").pack(side=tk.LEFT, padx=5)
        self._profile_var = tk.StringVar()
        self._profile_combo = ttk.Combobox(top, textvariable=self._profile_var,
                                           state="readonly", width=45)
        self._profile_combo.pack(side=tk.LEFT, padx=5)
        self._profile_combo.bind("<<ComboboxSelected>>", self._on_profile_selected)

        ttk.Button(top, text="+ Add", command=self._add_profile).pack(side=tk.LEFT, padx=2)
        ttk.Button(top, text="Remove", command=self._remove_profile).pack(side=tk.LEFT, padx=2)

        gf = ttk.Frame(self._ctrl_frame)
        gf.pack(fill=tk.X, pady=3)
        ttk.Label(gf, text="GUID:").pack(side=tk.LEFT, padx=5)
        self._guid_var = tk.StringVar()
        self._guid_entry = ttk.Entry(gf, textvariable=self._guid_var, width=55)
        self._guid_entry.pack(side=tk.LEFT, padx=5, fill=tk.X, expand=True)
        self._guid_entry.bind("<FocusOut>", self._on_guid_changed)

        df = ttk.LabelFrame(self._ctrl_frame, text="Capture Device")
        df.pack(fill=tk.X, pady=3, padx=5)

        r1 = ttk.Frame(df)
        r1.pack(fill=tk.X, pady=2)
        ttk.Label(r1, text="Device:").pack(side=tk.LEFT, padx=5)
        self._dev_var = tk.StringVar()
        self._dev_combo = ttk.Combobox(r1, textvariable=self._dev_var, state="readonly", width=35)
        self._dev_combo.pack(side=tk.LEFT, padx=5)
        self._dev_combo.bind("<<ComboboxSelected>>", self._on_device_selected)
        ttk.Button(r1, text="Refresh", command=self._refresh_device_list).pack(side=tk.LEFT, padx=2)

        self._dev_info_var = tk.StringVar(value="No devices scanned")
        tk.Label(r1, textvariable=self._dev_info_var, font=("", 8), fg="#555555").pack(side=tk.LEFT, padx=10)

        r2 = ttk.Frame(df)
        r2.pack(fill=tk.X, pady=2)
        ttk.Button(r2, text="Auto-match Profile", command=self._auto_match_device).pack(side=tk.LEFT, padx=5)
        ttk.Button(r2, text="Test Device", command=self._show_test_dialog).pack(side=tk.LEFT, padx=5)

        self._ctrl_table = MappingTable(
            self._ctrl_frame, PAD_TARGETS,
            enable_capture=True, capture_icon="Capture",
            capture_callback=self._show_capture_dialog
        )
        self._ctrl_table.pack(fill=tk.BOTH, expand=True, pady=5, padx=5)

    def _build_keyboard_tab(self):
        self._kbd_frame = ttk.Frame(self.notebook)
        self.notebook.add(self._kbd_frame, text=" Keyboard ")

        self._kbd_table = MappingTable(
            self._kbd_frame, KB_TARGETS,
            enable_capture=True, capture_icon="Key",
            capture_callback=self._show_kbd_capture_dialog
        )
        self._kbd_table.pack(fill=tk.BOTH, expand=True, pady=5, padx=5)

    def _build_mouse_tab(self):
        self._mouse_frame = ttk.Frame(self.notebook)
        self.notebook.add(self._mouse_frame, text=" Mouse ")

        self._mouse_table = MappingTable(
            self._mouse_frame, MS_TARGETS,
            enable_capture=True, capture_icon="Click",
            capture_callback=self._show_mouse_capture_dialog
        )
        self._mouse_table.pack(fill=tk.BOTH, expand=True, pady=5, padx=5)

    def _refresh_profile_list(self):
        names = self.ini.get_section_names()
        ctrl_names = [n for n in names
                      if n not in ("general", "keyboardandmouse")]
        self._profile_combo["values"] = ctrl_names
        if ctrl_names:
            self._profile_combo.set(ctrl_names[0])
            self._load_profile(ctrl_names[0])
        else:
            self._profile_combo.set("")
            self._ctrl_table.load_section(None, None)
            self._guid_var.set("")

    def _load_profile(self, display_name):
        lines = self.ini.get_lines_by_display_name(display_name)
        if lines is None:
            section_name = self._find_raw_name(display_name)
            self._ctrl_table.load_section(self.ini, section_name)
            guid = self.ini.get_value(section_name, "guid")
        else:
            self._ctrl_table.load_lines(lines)
            guid = None
            for item in lines:
                if item[0] == "kv" and item[1] == "guid":
                    guid = item[2]
                    break
        self._guid_var.set(guid or "")

    def _find_raw_name(self, display_name):
        seen = {}
        for name, _ in self.ini.sections:
            seen[name] = seen.get(name, 0) + 1
            count = seen[name]
            current = name if count == 1 else f"{name} #{count}"
            if current == display_name:
                return name
        return display_name

    def _on_profile_selected(self, event=None):
        display = self._profile_var.get()
        if display:
            self._load_profile(display)

    def _add_profile(self):
        d = tk.Toplevel(self.root)
        d.title("Add Profile")
        d.geometry("400x180")
        d.transient(self.root)
        d.grab_set()

        tk.Label(d, text="Profile name:").pack(pady=(15, 0))
        name_var = tk.StringVar(value="new_controller")
        ttk.Entry(d, textvariable=name_var, width=40).pack(pady=5)

        tk.Label(d, text="GUID:").pack()
        guid_var = tk.StringVar()
        ttk.Entry(d, textvariable=guid_var, width=40).pack(pady=5)

        def confirm():
            name = name_var.get().strip()
            if not name:
                messagebox.showwarning("Error", "Name required", parent=d)
                return
            guid = guid_var.get().strip()
            self.ini.sections.append(
                (name, [("header", f"[{name}]\n"), ("kv", "guid", guid)])
            )
            self.modified = True
            d.destroy()
            self._refresh_profile_list()

        ttk.Button(d, text="Add", command=confirm).pack(side=tk.LEFT, padx=40, pady=15)
        ttk.Button(d, text="Cancel", command=d.destroy).pack(side=tk.RIGHT, padx=40, pady=15)

    def _remove_profile(self):
        display = self._profile_var.get()
        if not display:
            return
        if not messagebox.askyesno("Remove Profile", f"Remove '{display}'?"):
            return
        lines = self.ini.get_lines_by_display_name(display)
        if lines is not None:
            self.ini.sections = [
                (n, d) for n, d in self.ini.sections if d is not lines
            ]
        else:
            raw = self._find_raw_name(display)
            self.ini.sections = [(n, d) for n, d in self.ini.sections if n != raw]
        self.modified = True
        self._refresh_profile_list()
        self.set_status(f"Removed: {display}")

    def _on_guid_changed(self, event=None):
        display = self._profile_var.get()
        if not display:
            return
        lines = self.ini.get_lines_by_display_name(display)
        val = self._guid_var.get().strip()
        if lines is not None:
            for i, item in enumerate(lines):
                if item[0] == "kv" and item[1] == "guid":
                    lines[i] = ("kv", "guid", val)
                    break
            else:
                lines.append(("kv", "guid", val))
        else:
            raw = self._find_raw_name(display)
            self.ini.set_value(raw, "guid", val)
        self.modified = True
        self.set_status(f"GUID updated for {display}")

    def _refresh_device_list(self):
        self._devices = self.gamepad.list_devices_full()
        display = []
        for d in self._devices:
            label = f"{d['index']}: {d['name']}"
            display.append(label)
        self._dev_combo["values"] = display
        if display:
            self._dev_combo.set(display[0])
            self.gamepad.select_device(self._devices[0]["index"])
            self._update_device_info(self._devices[0])
        else:
            self._dev_combo.set("")
            self._dev_info_var.set("No gamepad detected")

    def _update_device_info(self, info):
        parts = []
        if info["buttons"]:
            parts.append(f"Buttons: {info['buttons']}")
        if info["axes"]:
            parts.append(f"Axes: {info['axes']}")
        if info["hats"]:
            parts.append(f"Hats: {info['hats']}")
        text = " | ".join(parts) if parts else "Unknown"
        self._dev_info_var.set(text)

    def _on_device_selected(self, event=None):
        label = self._dev_var.get()
        if not label:
            return
        for d in self._devices:
            if f"{d['index']}: {d['name']}" == label:
                self.gamepad.select_device(d["index"])
                self._update_device_info(d)
                break

    def _auto_match_device(self):
        if not self._devices or not self._dev_combo.get():
            messagebox.showinfo("Auto-match", "No gamepad detected. Connect one and click Refresh.")
            return

        selected_label = self._dev_combo.get()
        selected = None
        for d in self._devices:
            if f"{d['index']}: {d['name']}" == selected_label:
                selected = d
                break
        if not selected:
            return

        device_name = selected["name"].lower()
        device_guid = selected.get("guid", "")
        sdl_pairs = extract_vid_pid_pairs_from_sdl_guid(device_guid) if device_guid else []

        names = self.ini.get_section_names()
        ctrl_names = [n for n in names if n not in ("general", "keyboardandmouse")]

        matched_name = None

        for cname in ctrl_names:
            raw = self._find_raw_name(cname)
            dixi_guid = self.ini.get_value(raw, "guid")
            if dixi_guid:
                d_vid, d_pid = extract_vid_pid_from_dixi_guid(dixi_guid)
                if d_vid and d_pid:
                    for svid, spid in sdl_pairs:
                        if svid == d_vid and spid == d_pid:
                            matched_name = cname
                            break
                if matched_name:
                    break

        if not matched_name:
            for cname in ctrl_names:
                c_clean = cname.lower().replace(" ", "").replace("_", "").replace("-", "")
                d_clean = device_name.replace(" ", "").replace("_", "").replace("-", "")
                if c_clean in d_clean or d_clean in c_clean:
                    matched_name = cname
                    break

        if matched_name:
            self._profile_combo.set(matched_name)
            self._load_profile(matched_name)
            self.set_status(f"Auto-matched: {selected['name']}  →  {matched_name}")
        else:
            msg = f"No matching profile found for:\n{selected['name']}"
            if sdl_pairs:
                vid, pid = sdl_pairs[0]
                msg += f"\nVID:PID = {vid:04X}:{pid:04X}"
            msg += "\n\nYou can add a new profile manually."
            messagebox.showinfo("Auto-match", msg)

    def _show_test_dialog(self):
        if not self._devices or not self._dev_combo.get():
            messagebox.showinfo("Test Device", "No gamepad selected.")
            return

        d = tk.Toplevel(self.root)
        d.title("Test Gamepad")
        d.geometry("500x350")
        d.transient(self.root)
        d.grab_set()

        tk.Label(d, text="Press buttons or move axes — values update live", font=("", 9)).pack(pady=5)

        text = tk.Text(d, font=("Consolas", 9), bg="#1e1e1e", fg="#d4d4d4",
                       wrap=tk.WORD, height=16, width=60)
        text.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        text.insert(tk.END, "Waiting for input...\n")

        sb = tk.Scrollbar(d, command=text.yview)
        sb.pack(side=tk.RIGHT, fill=tk.Y)
        text.config(yscrollcommand=sb.set)

        dialog_alive = [True]
        line_count = [0]
        poll_id = [None]

        dev_idx = 0
        for dd in self._devices:
            if f"{dd['index']}: {dd['name']}" == self._dev_var.get():
                dev_idx = dd["index"]
                break

        def _handle(ed, pn):
            if not dialog_alive[0]:
                return
            if line_count[0] > 100:
                text.delete("1.0", "2.0")
                line_count[0] -= 1
            ts = time.strftime("%H:%M:%S")
            if ed[0] == "button":
                text.insert(tk.END, f"[{ts}] Button {ed[1]} {ed[2] if len(ed)>2 else 'pressed'}\n")
                line_count[0] += 1
            elif ed[0] == "axis" and pn:
                text.insert(tk.END, f"[{ts}] Axis {ed[1]} = {ed[2]:+.2f}  →  {pn}\n")
                line_count[0] += 1
            elif ed[0] == "hat" and pn:
                text.insert(tk.END, f"[{ts}] Hat {ed[1]}  →  {pn}\n")
                line_count[0] += 1
            text.see(tk.END)

        def _poll():
            if not dialog_alive[0]:
                return
            ev = self.gamepad.poll_test_event(device_index=dev_idx)
            if ev:
                _handle(ev, ev[3] if len(ev) > 3 else None)
            poll_id[0] = d.after(50, _poll)

        _poll()

        def stop():
            dialog_alive[0] = False
            self.gamepad.stop_capture()
            if poll_id[0]:
                try:
                    d.after_cancel(poll_id[0])
                except Exception:
                    pass
            d.destroy()

        ttk.Button(d, text="Stop", command=stop).pack(pady=5)

        d.protocol("WM_DELETE_WINDOW", stop)

    def _show_capture_dialog(self, input_key, setter):
        d = tk.Toplevel(self.root)
        d.title("Capture Gamepad Input")
        d.geometry("420x160")
        d.transient(self.root)
        d.grab_set()
        d.focus_set()

        tk.Label(d, text=f"Press a button or move an axis for:", font=("", 10)).pack(pady=(15, 2))
        tk.Label(d, text=f"{input_key}  {GAME_INPUT_LABELS.get(input_key, '')}",
                 font=("", 10, "bold")).pack()

        selected_label = self._dev_var.get()
        dev_idx = None
        for dd in self._devices:
            if f"{dd['index']}: {dd['name']}" == selected_label:
                dev_idx = dd["index"]
                break

        result = [None]
        capture_done = [False]
        poll_id = [None]
        status_var = tk.StringVar(value="Waiting...")
        tk.Label(d, textvariable=status_var, font=("", 12, "bold"),
                 fg="#0078d7").pack(pady=8)

        def _poll():
            if capture_done[0] or not d.winfo_exists():
                return
            ev = self.gamepad.poll_event(device_index=dev_idx)
            if ev:
                _, name = ev
                result[0] = name
                status_var.set(f"Detected: {name}")
                capture_done[0] = True
                return
            poll_id[0] = d.after(50, _poll)

        _poll()

        def accept():
            if result[0]:
                setter(input_key, result[0])
            capture_done[0] = True
            self.gamepad.stop_capture()
            if poll_id[0]:
                try:
                    d.after_cancel(poll_id[0])
                except Exception:
                    pass
            d.destroy()

        def cancel():
            capture_done[0] = True
            self.gamepad.stop_capture()
            if poll_id[0]:
                try:
                    d.after_cancel(poll_id[0])
                except Exception:
                    pass
            d.destroy()

        bf = ttk.Frame(d)
        bf.pack(pady=10)
        ttk.Button(bf, text="Accept", command=accept).pack(side=tk.LEFT, padx=15)
        ttk.Button(bf, text="Cancel", command=cancel).pack(side=tk.LEFT, padx=15)

    def _show_kbd_capture_dialog(self, input_key, setter):
        d = tk.Toplevel(self.root)
        d.title("Capture Key")
        d.geometry("400x140")
        d.transient(self.root)
        d.grab_set()

        tk.Label(d, text=f"Press a key for:", font=("", 10)).pack(pady=(15, 2))
        tk.Label(d, text=f"{input_key}  {GAME_INPUT_LABELS.get(input_key, '')}",
                 font=("", 10, "bold")).pack()

        result = [None]
        status_var = tk.StringVar(value="Press any key...")
        tk.Label(d, textvariable=status_var, font=("", 12, "bold"),
                 fg="#0078d7").pack(pady=8)

        def on_result(res, err):
            if res:
                result[0] = res
                status_var.set(f"Detected: {res}")

        capture_keyboard_on_widget(d, on_result)

        def accept():
            if result[0]:
                setter(input_key, result[0])
            d.destroy()

        def cancel():
            d.destroy()

        bf = ttk.Frame(d)
        bf.pack(pady=10)
        ttk.Button(bf, text="Accept", command=accept).pack(side=tk.LEFT, padx=15)
        ttk.Button(bf, text="Cancel", command=cancel).pack(side=tk.LEFT, padx=15)

    def _show_mouse_capture_dialog(self, input_key, setter):
        d = tk.Toplevel(self.root)
        d.title("Capture Mouse Button")
        d.geometry("400x140")
        d.transient(self.root)
        d.grab_set()

        tk.Label(d, text=f"Click a mouse button for:", font=("", 10)).pack(pady=(15, 2))
        tk.Label(d, text=f"{input_key}  {GAME_INPUT_LABELS.get(input_key, '')}",
                 font=("", 10, "bold")).pack()

        result = [None]
        status_var = tk.StringVar(value="Click a button...")
        tk.Label(d, textvariable=status_var, font=("", 12, "bold"),
                 fg="#0078d7").pack(pady=8)

        def on_result(res, err):
            if res:
                result[0] = res
                status_var.set(f"Detected: {res}")

        capture_mouse_on_widget(d, on_result)

        def accept():
            if result[0]:
                setter(input_key, result[0])
            d.destroy()

        def cancel():
            d.destroy()

        bf = ttk.Frame(d)
        bf.pack(pady=10)
        ttk.Button(bf, text="Accept", command=accept).pack(side=tk.LEFT, padx=15)
        ttk.Button(bf, text="Cancel", command=cancel).pack(side=tk.LEFT, padx=15)

    def _populate_from_ini(self):
        gen = self.ini.get_all_kv("general")
        for key in ("onelayout", "dpadtoanalog", "keyboardandmouse"):
            if key in self._gen_vars:
                self._gen_vars[key].set(gen.get(key, "FALSE"))
        try:
            self._speed_var.set(int(gen.get("mousespeed", "5")))
        except ValueError:
            self._speed_var.set(5)

        self._refresh_profile_list()
        self._kbd_table.load_section(self.ini, "keyboardandmouse")
        self._mouse_table.load_section(self.ini, "keyboardandmouse")
        self._refresh_device_list()

    def open_file(self, filepath=None):
        if self.modified:
            ans = messagebox.askyesnocancel("Unsaved Changes",
                                            "Save changes before opening?")
            if ans is True:
                self.save_file()
            elif ans is None:
                return

        if not filepath:
            filepath = filedialog.askopenfilename(
                title="Open dixi.ini",
                filetypes=[("INI files", "*.ini"), ("All files", "*.*")]
            )
        if not filepath:
            return

        try:
            self.ini.read(filepath)
            self.current_file = filepath
            self.modified = False
            self._populate_from_ini()
            self.root.title(f"MGS PAD Editor  {os.path.basename(filepath)}")
            self.set_status(f"Loaded: {filepath}")
        except Exception as e:
            messagebox.showerror("Error", f"Could not open file:\n{e}")

    def save_file(self):
        if not self.current_file:
            self.save_as_file()
            return
        try:
            self.ini.write(self.current_file)
            self.modified = False
            self.set_status(f"Saved: {self.current_file}")
        except Exception as e:
            messagebox.showerror("Error", f"Could not save:\n{e}")

    def save_as_file(self):
        fp = filedialog.asksaveasfilename(
            title="Save dixi.ini",
            defaultextension=".ini",
            filetypes=[("INI files", "*.ini"), ("All files", "*.*")]
        )
        if not fp:
            return
        self.current_file = fp
        self.save_file()
        self.root.title(f"MGS PAD Editor  {os.path.basename(fp)}")

    def _on_exit(self):
        if self.modified:
            ans = messagebox.askyesnocancel("Unsaved Changes",
                                            "Save changes before exiting?")
            if ans is True:
                self.save_file()
            elif ans is None:
                return
        self.root.destroy()

    def _show_about(self):
        pg = "+" if PYGAME_AVAILABLE else "-"
        messagebox.showinfo("About",
            f"MGS PAD Editor v1.0\n"
            f"Editor de controles para Metal Gear Solid (GOG)\n"
            f"pygame: {pg} disponible\n\n"
            f"Edita dixi.ini y asigna controles\n"
            f"por pulsacion en tu gamepad.")


def main():
    root = tk.Tk()
    app = MgsPadEditor(root)

    default_ini = os.path.join(os.path.dirname(os.path.abspath(__file__)), "dixi.ini")
    if os.path.exists(default_ini):
        app.open_file(default_ini)

    root.mainloop()


if __name__ == "__main__":
    main()
