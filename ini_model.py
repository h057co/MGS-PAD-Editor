# ini_model.py
# Custom INI parser/writer for dixi.ini — preserves blanks, comments, duplicate sections

from collections import OrderedDict


class IniFile:
    def __init__(self):
        self.sections = []
        self.filepath = None

    def read(self, filepath):
        self.filepath = filepath
        self.sections = []
        with open(filepath, "r", encoding="utf-8-sig") as f:
            raw_lines = f.readlines()
        current_name = None
        current_lines = []
        for line in raw_lines:
            stripped = line.strip()
            if not stripped:
                current_lines.append(("blank", line))
            elif stripped.startswith(";") or stripped.startswith("#"):
                current_lines.append(("comment", line))
            elif stripped.startswith("[") and stripped.endswith("]"):
                if current_name is not None:
                    self.sections.append((current_name, current_lines))
                current_name = stripped[1:-1]
                current_lines = [("header", line)]
            elif "=" in stripped and current_name is not None:
                key, val = stripped.split("=", 1)
                key = key.rstrip()
                val = val.lstrip()
                current_lines.append(("kv", key, val))
            elif current_name is not None:
                current_lines.append(("other", line))
            else:
                current_lines.append(("preamble", line))
        if current_name is not None:
            self.sections.append((current_name, current_lines))
        return self

    def write(self, filepath=None):
        path = filepath or self.filepath
        header_written = set()
        with open(path, "w", encoding="utf-8") as f:
            for name, lines in self.sections:
                for item in lines:
                    if item[0] == "blank":
                        f.write(item[1])
                    elif item[0] == "comment":
                        f.write(item[1])
                    elif item[0] == "header":
                        f.write(f"[{name}]\n")
                    elif item[0] == "kv":
                        key, val = item[1], item[2]
                        f.write(f"{key:<20s}= {val}\n")
                    elif item[0] == "other":
                        f.write(item[1])
                    elif item[0] == "preamble":
                        f.write(item[1])
        return self

    def get_section_index(self, name):
        for idx, (n, _) in enumerate(self.sections):
            if n == name:
                return idx
        return -1

    def get_section_names(self):
        names = []
        seen = {}
        for name, _ in self.sections:
            seen[name] = seen.get(name, 0) + 1
            count = seen[name]
            if count > 1:
                names.append(f"{name} #{count}")
            else:
                names.append(name)
        return names

    def get_section_by_display_name(self, display_name):
        seen = {}
        for name, data in self.sections:
            seen[name] = seen.get(name, 0) + 1
            count = seen[name]
            current_display = name if count == 1 else f"{name} #{count}"
            if current_display == display_name:
                return data
        return None

    def get_value(self, section_name, key):
        for name, lines in self.sections:
            if name == section_name:
                for item in lines:
                    if item[0] == "kv" and item[1] == key:
                        return item[2]
        return None

    def set_value(self, section_name, key, value):
        for idx, (name, lines) in enumerate(self.sections):
            if name == section_name:
                for i, item in enumerate(lines):
                    if item[0] == "kv" and item[1] == key:
                        lines[i] = ("kv", key, value)
                        return
                lines.append(("kv", key, value))
                return
        self.sections.append(
            (section_name, [("header", f"[{section_name}]\n"), ("kv", key, value)])
        )

    def remove_value(self, section_name, key):
        for name, lines in self.sections:
            if name == section_name:
                lines[:] = [
                    item for item in lines if not (item[0] == "kv" and item[1] == key)
                ]
                return

    def get_all_kv(self, section_name):
        result = OrderedDict()
        for name, lines in self.sections:
            if name == section_name:
                for item in lines:
                    if item[0] == "kv":
                        result[item[1]] = item[2]
                break
        return result

    def get_lines_by_display_name(self, display_name):
        seen = {}
        for name, lines in self.sections:
            seen[name] = seen.get(name, 0) + 1
            count = seen[name]
            current = name if count == 1 else f"{name} #{count}"
            if current == display_name:
                return lines
        return None

    def get_lines_by_name(self, section_name):
        for name, lines in self.sections:
            if name == section_name:
                return lines
        return None
