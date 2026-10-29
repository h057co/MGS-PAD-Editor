using System.Collections.Generic;

namespace MgsPadEditor.Models
{
    public class TargetOption
    {
        public string Value { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public override string ToString() => DisplayText;
    }

    public static class MappingDefinitions
    {
        // -------------------------------------------------------------
        // DESCRIPCIÓN DE ACCIONES LÓGICAS EN METAL GEAR SOLID (GOG / PC)
        // -------------------------------------------------------------
        public static readonly Dictionary<string, string> GameActionDescriptions = new()
        {
            ["pad_button0"] = "Triángulo (△) — Vista Primera Persona",
            ["pad_button1"] = "Cuadrado (□) — Disparo / Ataque / Estrangular",
            ["pad_button2"] = "Círculo (○) — Acción / Golpe Combo / Confirmar",
            ["pad_button3"] = "Cruz (✕) — Agacharse / Arrastrarse / Cancelar",
            ["pad_button4"] = "L1 — Equipar / Desequipar Ítem Rápido",
            ["pad_button5"] = "L2 — Menú Inventario de Ítems (Mantener L2)",
            ["pad_button6"] = "R1 — Equipar / Desequipar Arma Rápida",
            ["pad_button7"] = "R2 — Menú Inventario de Armas (Mantener R2)",
            ["pad_button8"] = "Select / Share — Códec / Radio",
            ["pad_button9"] = "Start / Options — Menú de Pausa",
            ["pad_button10"] = "L3 (Click Stick Izquierdo)",
            ["pad_button11"] = "R3 (Click Stick Derecho)",
            ["pad_button12"] = "Botón PS (Pausa)",
            ["pad_button13"] = "Touchpad (Códec Rápido)",
            ["pad_button14"] = "Botón Extra 14",
            ["pad_button15"] = "Botón Extra 15",
            ["pad_button16"] = "Botón Extra 16",
            ["pad_button17"] = "Botón Extra 17",
            ["pad_xaxisplus"] = "Stick Izq. → (Mover a la Derecha)",
            ["pad_xaxisminus"] = "Stick Izq. ← (Mover a la Izquierda)",
            ["pad_yaxisplus"] = "Stick Izq. ↓ (Mover hacia Atrás)",
            ["pad_yaxisminus"] = "Stick Izq. ↑ (Mover hacia Adelante)",
            ["pad_rxaxisplus"] = "Stick Der. → (Desactivado en PS1)",
            ["pad_rxaxisminus"] = "Stick Der. ← (Desactivado en PS1)",
            ["pad_ryaxisplus"] = "Stick Der. ↓ (Desactivado en PS1)",
            ["pad_ryaxisminus"] = "Stick Der. ↑ (Desactivado en PS1)",
            ["pad_zaxisplus"] = "Gatillo L2 Analógico (Menú de Ítems)",
            ["pad_zaxisminus"] = "Gatillo L2 Analógico (Desactivado)",
            ["pad_rzaxisplus"] = "Gatillo R2 Analógico (Menú de Armas)",
            ["pad_rzaxisminus"] = "Gatillo R2 Analógico (Desactivado)",
            ["pad_dpadup"] = "Cruceta Arriba (D-Pad Up)",
            ["pad_dpadright"] = "Cruceta Derecha (D-Pad Right)",
            ["pad_dpaddown"] = "Cruceta Abajo (D-Pad Down)",
            ["pad_dpadleft"] = "Cruceta Izquierda (D-Pad Left)",
        };

        // -------------------------------------------------------------
        // ORDEN DE VISUALIZACIÓN RECOMENDADO
        // -------------------------------------------------------------
        public static readonly List<string> PreferredInputOrder = new()
        {
            // Cruceta
            "pad_dpadup", "pad_dpadright", "pad_dpaddown", "pad_dpadleft",
            // Botones principales (A/B/X/Y o Cruz/Círculo/Cuadrado/Triángulo)
            "pad_button0", "pad_button1", "pad_button2", "pad_button3",
            // Gatillos y Bumpers
            "pad_button4", "pad_button5", "pad_button6", "pad_button7",
            // Select / Start
            "pad_button8", "pad_button9",
            // Sticks clicks
            "pad_button10", "pad_button11",
            // Botones guía / auxiliares
            "pad_button12", "pad_button13", "pad_button14", "pad_button15", "pad_button16", "pad_button17",
            // Ejes analógicos
            "pad_yaxisminus", "pad_yaxisplus", "pad_xaxisminus", "pad_xaxisplus",
            "pad_ryaxisminus", "pad_ryaxisplus", "pad_rxaxisminus", "pad_rxaxisplus",
            "pad_zaxisminus", "pad_zaxisplus", "pad_rzaxisminus", "pad_rzaxisplus"
        };

        // -------------------------------------------------------------
        // OPCIONES DE DESTINO PARA MANDOS (Target Game Inputs)
        // -------------------------------------------------------------
        public static List<TargetOption> GetPadTargetOptions()
        {
            var list = new List<TargetOption>
            {
                new() { Value = "dev_null", DisplayText = "Desactivado (dev_null)", Category = "General" }
            };

            foreach (var key in PreferredInputOrder)
            {
                string desc = GameActionDescriptions.GetValueOrDefault(key, key);
                list.Add(new()
                {
                    Value = key,
                    DisplayText = $"{key}  ({desc})",
                    Category = key.StartsWith("pad_button") ? "Botones" : key.StartsWith("pad_dpad") ? "Cruceta" : "Ejes"
                });
            }

            return list;
        }

        // -------------------------------------------------------------
        // OPCIONES DE DESTINO PARA TECLADO Y RATÓN
        // -------------------------------------------------------------
        public static List<TargetOption> GetKeyboardAndMouseTargetOptions()
        {
            var list = new List<TargetOption>
            {
                new() { Value = "dev_null", DisplayText = "Desactivado (dev_null)", Category = "General" },

                // Ratón
                new() { Value = "ms_button0", DisplayText = "Clic Izquierdo (ms_button0)", Category = "Ratón" },
                new() { Value = "ms_button1", DisplayText = "Clic Derecho (ms_button1)", Category = "Ratón" },
                new() { Value = "ms_button2", DisplayText = "Clic Central / Rueda (ms_button2)", Category = "Ratón" },
                new() { Value = "ms_xaxisplus", DisplayText = "Mover Ratón Derecha (+X)", Category = "Ratón" },
                new() { Value = "ms_xaxisminus", DisplayText = "Mover Ratón Izquierda (-X)", Category = "Ratón" },
                new() { Value = "ms_yaxisplus", DisplayText = "Mover Ratón Abajo (+Y)", Category = "Ratón" },
                new() { Value = "ms_yaxisminus", DisplayText = "Mover Ratón Arriba (-Y)", Category = "Ratón" },
                new() { Value = "ms_zaxisplus", DisplayText = "Rueda Ratón Arriba (+Z)", Category = "Ratón" },
                new() { Value = "ms_zaxisminus", DisplayText = "Rueda Ratón Abajo (-Z)", Category = "Ratón" },

                // Teclas de Movimiento
                new() { Value = "kb_w", DisplayText = "Tecla W", Category = "Teclado" },
                new() { Value = "kb_s", DisplayText = "Tecla S", Category = "Teclado" },
                new() { Value = "kb_a", DisplayText = "Tecla A", Category = "Teclado" },
                new() { Value = "kb_d", DisplayText = "Tecla D", Category = "Teclado" },
                new() { Value = "kb_up", DisplayText = "Flecha Arriba", Category = "Teclado" },
                new() { Value = "kb_down", DisplayText = "Flecha Abajo", Category = "Teclado" },
                new() { Value = "kb_left", DisplayText = "Flecha Izquierda", Category = "Teclado" },
                new() { Value = "kb_right", DisplayText = "Flecha Derecha", Category = "Teclado" },

                // Teclas de Acción y Modificadores
                new() { Value = "kb_space", DisplayText = "Espacio (Space)", Category = "Teclado" },
                new() { Value = "kb_return", DisplayText = "Enter / Retorno", Category = "Teclado" },
                new() { Value = "kb_escape", DisplayText = "Escape (Esc)", Category = "Teclado" },
                new() { Value = "kb_tab", DisplayText = "Tabulación (Tab)", Category = "Teclado" },
                new() { Value = "kb_lshift", DisplayText = "Shift Izquierdo", Category = "Teclado" },
                new() { Value = "kb_lcontrol", DisplayText = "Control Izquierdo", Category = "Teclado" },
                new() { Value = "kb_rcontrol", DisplayText = "Control Derecho", Category = "Teclado" },
                new() { Value = "kb_lalt", DisplayText = "Alt Izquierdo", Category = "Teclado" },
                new() { Value = "kb_ralt", DisplayText = "Alt Derecho", Category = "Teclado" },
                new() { Value = "kb_e", DisplayText = "Tecla E", Category = "Teclado" },
                new() { Value = "kb_l", DisplayText = "Tecla L", Category = "Teclado" },
                new() { Value = "kb_z", DisplayText = "Tecla Z", Category = "Teclado" },
                new() { Value = "kb_q", DisplayText = "Tecla Q", Category = "Teclado" },
                new() { Value = "kb_r", DisplayText = "Tecla R", Category = "Teclado" },
                new() { Value = "kb_f", DisplayText = "Tecla F", Category = "Teclado" },
                new() { Value = "kb_1", DisplayText = "Número 1", Category = "Teclado" },
                new() { Value = "kb_2", DisplayText = "Número 2", Category = "Teclado" },
                new() { Value = "kb_3", DisplayText = "Número 3", Category = "Teclado" },
                new() { Value = "kb_4", DisplayText = "Número 4", Category = "Teclado" },
                new() { Value = "kb_5", DisplayText = "Número 5", Category = "Teclado" },
                new() { Value = "kb_f1", DisplayText = "Tecla F1", Category = "Teclado" },
                new() { Value = "kb_f2", DisplayText = "Tecla F2", Category = "Teclado" },
                new() { Value = "kb_f3", DisplayText = "Tecla F3", Category = "Teclado" },
                new() { Value = "kb_f4", DisplayText = "Tecla F4", Category = "Teclado" },
                new() { Value = "kb_f5", DisplayText = "Tecla F5", Category = "Teclado" },
                new() { Value = "kb_f6", DisplayText = "Tecla F6", Category = "Teclado" },
                new() { Value = "kb_f7", DisplayText = "Tecla F7", Category = "Teclado" },
                new() { Value = "kb_f8", DisplayText = "Tecla F8", Category = "Teclado" },
                new() { Value = "kb_f9", DisplayText = "Tecla F9", Category = "Teclado" },
                new() { Value = "kb_f10", DisplayText = "Tecla F10", Category = "Teclado" },
                new() { Value = "kb_f11", DisplayText = "Tecla F11", Category = "Teclado" },
                new() { Value = "kb_f12", DisplayText = "Tecla F12", Category = "Teclado" }
            };

            return list;
        }

        // -------------------------------------------------------------
        // PRESETS POR DEFECTO PARA POPULARES MANDOS
        // -------------------------------------------------------------
        public static Dictionary<string, string> GetPresetXbox()
        {
            return new Dictionary<string, string>
            {
                ["pad_zaxisplus"] = "pad_button6",   // LT -> L2
                ["pad_zaxisminus"] = "pad_button7",  // RT -> R2
                ["pad_button6"] = "pad_button8",     // Back -> Select
                ["pad_button7"] = "pad_button9",     // Start -> Start
                ["pad_button8"] = "pad_button10",    // L3 -> L3
                ["pad_button9"] = "pad_button11",    // R3 -> R3
                ["pad_button10"] = "pad_button13",
                ["pad_button11"] = "pad_button12",
            };
        }

        public static Dictionary<string, string> GetPresetDualShock4()
        {
            return new Dictionary<string, string>
            {
                ["pad_zaxisplus"] = "pad_rxaxisplus",
                ["pad_zaxisminus"] = "pad_rxaxisminus",
                ["pad_rxaxisplus"] = "dev_null",
                ["pad_rxaxisminus"] = "dev_null",
                ["pad_ryaxisplus"] = "dev_null",
                ["pad_ryaxisminus"] = "dev_null",
                ["pad_rzaxisplus"] = "pad_ryaxisplus",
                ["pad_rzaxisminus"] = "pad_ryaxisminus",
                ["pad_button0"] = "pad_button2",     // Square -> Menu
                ["pad_button1"] = "pad_button0",     // Cross -> Confirm
                ["pad_button2"] = "pad_button1",     // Circle -> Cancel
                ["pad_button8"] = "pad_button12",    // Share -> Guide
                ["pad_button9"] = "pad_button8",     // Options -> Select
                ["pad_button12"] = "pad_button9"     // PS -> Start
            };
        }

        public static Dictionary<string, string> GetPresetDualSense()
        {
            return GetPresetDualSensePs1Classic();
        }

        public static Dictionary<string, string> GetPresetDualSensePs1Classic()
        {
            return new Dictionary<string, string>
            {
                // Botones principales idénticos a Metal Gear Solid de PS1
                // En DirectInput: Button 0 = □ Cuadrado, Button 1 = ✕ Cruz, Button 2 = ○ Círculo, Button 3 = △ Triángulo
                // En mgsi.exe:    pad_button0 = △ FPV, pad_button1 = □ Arma, pad_button2 = ○ Golpe, pad_button3 = ✕ Agacharse
                ["pad_button0"] = "pad_button1",      // Cuadrado (□) -> Disparo / Arma / Estrangular
                ["pad_button1"] = "pad_button3",      // Cruz (✕)     -> Agacharse / Arrastrarse / Cancelar
                ["pad_button2"] = "pad_button2",      // Círculo (○)  -> Acción / Golpe Combo / Confirmar
                ["pad_button3"] = "pad_button0",      // Triángulo (△) -> Vista Primera Persona

                // Bumpers y Gatillos idénticos a PS1
                // En DirectInput: Button 4 = L1, Button 5 = R1, Button 6 = L2, Button 7 = R2
                // En mgsi.exe:    pad_button4 = L1 Rápido, pad_button5 = L2 Menú Ítems, pad_button6 = R1 Rápido, pad_button7 = R2 Menú Armas
                ["pad_button4"] = "pad_button4",      // L1           -> Desequipar / Equipar Ítem Rápido
                ["pad_button5"] = "pad_button6",      // R1           -> Desequipar / Equipar Arma Rápida
                ["pad_button6"] = "pad_button5",      // L2 (Digital) -> Menú Inventario de Ítems (Mantener L2)
                ["pad_button7"] = "pad_button7",      // R2 (Digital) -> Menú Inventario de Armas (Mantener R2)

                // Gatillos Analógicos del DualSense vinculados a Menús de Ítems y Armas (L2 = RotationX / Rx, R2 = RotationY / Ry)
                ["pad_rxaxisplus"] = "pad_button5",   // L2 (Presión Analógica) -> Menú Inventario de Ítems
                ["pad_rxaxisminus"] = "dev_null",
                ["pad_ryaxisplus"] = "pad_button7",   // R2 (Presión Analógica) -> Menú Inventario de Armas
                ["pad_ryaxisminus"] = "dev_null",

                // Botones de Sistema y Códec
                ["pad_button8"] = "pad_button8",      // Create/Share -> Códec / Radio (Select de PS1)
                ["pad_button9"] = "pad_button9",      // Options      -> Menú de Pausa (Start de PS1)
                ["pad_button12"] = "pad_button9",     // Botón PS     -> Menú de Pausa
                ["pad_button13"] = "pad_button8",     // Touchpad     -> Códec / Radio Rápido

                // Sticks (Stick Derecho en DualSense = Ejes Z y Rz. En MGS1 PS1 clásico se desactivan para evitar giros involuntarios o activación de menús)
                ["pad_button10"] = "pad_button10",    // L3 Click
                ["pad_button11"] = "pad_button11",    // R3 Click
                ["pad_zaxisplus"] = "dev_null",       // Desactivar stick derecho para evitar menús o giros en MGS1
                ["pad_zaxisminus"] = "dev_null",
                ["pad_rzaxisplus"] = "dev_null",
                ["pad_rzaxisminus"] = "dev_null",

                // Cruceta
                ["pad_dpadup"] = "pad_dpadup",
                ["pad_dpadright"] = "pad_dpadright",
                ["pad_dpaddown"] = "pad_dpaddown",
                ["pad_dpadleft"] = "pad_dpadleft",
            };
        }
    }
}
