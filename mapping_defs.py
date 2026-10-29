# mapping_defs.py
# Catálogo de inputs del juego, targets disponibles y sort order

GAME_INPUT_LABELS = {
    "pad_button0": "Cross / A / B (Confirm)",
    "pad_button1": "Circle / B / A (Cancel)",
    "pad_button2": "Square / X / Y (Menu/Weapon)",
    "pad_button3": "Triangle / Y / X (Camera/Change View)",
    "pad_button4": "L1 (First Person / Item Hold)",
    "pad_button5": "R1 (Action / Equip Weapon)",
    "pad_button6": "L2 (Weapon Ready / Aim)",
    "pad_button7": "R2 (Attack / Fire)",
    "pad_button8": "Select (Codec / Radio)",
    "pad_button9": "Start (Pause Menu)",
    "pad_button10": "L3 (Left Stick Click)",
    "pad_button11": "R3 (Right Stick Click)",
    "pad_button12": "Guide / Home / PS Button",
    "pad_button13": "Touchpad / Share",
    "pad_button14": "Button 14",
    "pad_button15": "Button 15",
    "pad_button16": "Button 16",
    "pad_button17": "Button 17",
    "pad_xaxisplus": "Left Stick → (Move Right)",
    "pad_xaxisminus": "Left Stick ← (Move Left)",
    "pad_yaxisplus": "Left Stick ↓ (Move Back)",
    "pad_yaxisminus": "Left Stick ↑ (Move Forward)",
    "pad_rxaxisplus": "Right Stick → (Look Right)",
    "pad_rxaxisminus": "Right Stick ← (Look Left)",
    "pad_ryaxisplus": "Right Stick ↓ (Look Down)",
    "pad_ryaxisminus": "Right Stick ↑ (Look Up)",
    "pad_zaxisplus": "L2 / Z+ (Analog Pressure)",
    "pad_zaxisminus": "L2 / Z-",
    "pad_rzaxisplus": "R2 / RZ+ (Analog Pressure)",
    "pad_rzaxisminus": "R2 / RZ-",
    "pad_dpadup": "D-Pad Up",
    "pad_dpadright": "D-Pad Right",
    "pad_dpaddown": "D-Pad Down",
    "pad_dpadleft": "D-Pad Left",
}

ALL_PAD_INPUTS = list(GAME_INPUT_LABELS.keys())

PAD_BUTTONS = [f"pad_button{i}" for i in range(18)]

PAD_TARGETS = [
    "dev_null",
    *PAD_BUTTONS,
    "pad_xaxisplus", "pad_xaxisminus",
    "pad_yaxisplus", "pad_yaxisminus",
    "pad_rxaxisplus", "pad_rxaxisminus",
    "pad_ryaxisplus", "pad_ryaxisminus",
    "pad_zaxisplus", "pad_zaxisminus",
    "pad_rzaxisplus", "pad_rzaxisminus",
    "pad_dpadup", "pad_dpadright", "pad_dpaddown", "pad_dpadleft",
]

KB_TARGETS = ["dev_null",
    "kb_up", "kb_down", "kb_left", "kb_right",
    "kb_space", "kb_lcontrol", "kb_e", "kb_l", "kb_z",
    "kb_tab", "kb_escape", "kb_lshift",
    "kb_return", "kb_lalt", "kb_ralt", "kb_lwin", "kb_rwin",
    "kb_rcontrol", "kb_a", "kb_d", "kb_w", "kb_s",
    "kb_q", "kb_r", "kb_f", "kb_1", "kb_2", "kb_3", "kb_4", "kb_5",
    "kb_f1", "kb_f2", "kb_f3", "kb_f4", "kb_f5", "kb_f6",
    "kb_f7", "kb_f8", "kb_f9", "kb_f10", "kb_f11", "kb_f12",
]

MS_TARGETS = ["dev_null",
    "ms_xaxisplus", "ms_xaxisminus",
    "ms_yaxisplus", "ms_yaxisminus",
    "ms_zaxisplus", "ms_zaxisminus",
    "ms_button0", "ms_button1", "ms_button2",
]

SORT_ORDER = {
    "pad_dpadup": 0, "pad_dpadright": 1, "pad_dpaddown": 2, "pad_dpadleft": 3,
    "pad_button0": 4, "pad_button1": 5, "pad_button2": 6, "pad_button3": 7,
    "pad_button4": 8, "pad_button5": 9, "pad_button6": 10, "pad_button7": 11,
    "pad_button8": 12, "pad_button9": 13, "pad_button10": 14, "pad_button11": 15,
    "pad_button12": 16, "pad_button13": 17, "pad_button14": 18, "pad_button15": 19,
    "pad_button16": 20, "pad_button17": 21,
    "pad_yaxisminus": 22, "pad_yaxisplus": 23,
    "pad_xaxisminus": 24, "pad_xaxisplus": 25,
    "pad_ryaxisminus": 26, "pad_ryaxisplus": 27,
    "pad_rxaxisminus": 28, "pad_rxaxisplus": 29,
    "pad_zaxisminus": 30, "pad_zaxisplus": 31,
    "pad_rzaxisminus": 32, "pad_rzaxisplus": 33,
}

KB_KEY_MAP = {
    "Up": "kb_up", "Down": "kb_down", "Left": "kb_left", "Right": "kb_right",
    "space": "kb_space", "Control_L": "kb_lcontrol", "Control_R": "kb_rcontrol",
    "e": "kb_e", "l": "kb_l", "z": "kb_z",
    "Tab": "kb_tab", "Escape": "kb_escape",
    "Shift_L": "kb_lshift", "Shift_R": "kb_lshift",
    "Return": "kb_return", "KP_Enter": "kb_return",
    "Alt_L": "kb_lalt", "Alt_R": "kb_ralt",
    "Super_L": "kb_lwin", "Super_R": "kb_rwin",
    "a": "kb_a", "d": "kb_d", "w": "kb_w", "s": "kb_s",
    "q": "kb_q", "r": "kb_r", "f": "kb_f",
    "1": "kb_1", "2": "kb_2", "3": "kb_3", "4": "kb_4", "5": "kb_5",
    "F1": "kb_f1", "F2": "kb_f2", "F3": "kb_f3", "F4": "kb_f4",
    "F5": "kb_f5", "F6": "kb_f6", "F7": "kb_f7", "F8": "kb_f8",
    "F9": "kb_f9", "F10": "kb_f10", "F11": "kb_f11", "F12": "kb_f12",
}

MS_BUTTON_MAP = {
    "Button-1": "ms_button0",
    "Button-2": "ms_button1",
    "Button-3": "ms_button2",
}
