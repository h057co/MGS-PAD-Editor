using System;
using System.IO;

namespace MgsPadEditor.Services
{
    /// <summary>
    /// Servicio para diagnosticar y reparar el archivo de configuración interna de Metal Gear Solid (mgs.cfg).
    /// En mgsi.exe, mgs.cfg contiene el mapeo secundario de acciones del juego a botones de DirectInput.
    /// Si el usuario reconfiguró controles dentro del menú de MGS, mgs.cfg introduce conflictos con dixi.ini.
    /// Este servicio restablece mgs.cfg a su mapeo limpio 1:1 estándar para que dixi.ini funcione al 100%.
    /// </summary>
    public static class MgsCfgService
    {
        // Acciones estándar de Metal Gear Solid (mgsi.exe):
        // 0: First person view (Triángulo) -> Botón 0
        // 1: Weapon / Grab (Cuadrado)       -> Botón 1
        // 2: Punch / Confirm (Círculo)      -> Botón 2
        // 3: Crouch / Cancel (Cruz)         -> Botón 3
        // 4: Item equip (L1)                -> Botón 4
        // 5: Item menu (L2)                 -> Botón 5
        // 6: Weapon equip (R1)              -> Botón 6
        // 7: Weapon menu (R2)               -> Botón 7
        // 8: Codec (Select)                 -> Botón 8
        // 9: Left (D-Pad)                   -> Botón 54 (POV Izquierda)
        // 10: Right (D-Pad)                 -> Botón 50 (POV Derecha)
        // 11: Up (D-Pad)                    -> Botón 48 (POV Arriba)
        // 12: Down (D-Pad)                  -> Botón 52 (POV Abajo)
        // 13: Menu / Soft pause (Start)     -> Botón 9
        private static readonly uint[] CleanActionButtons = new uint[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 54, 50, 48, 52, 9
        };

        private const int ActionOffset = 30;
        private const int ActionCount = 14;

        public static bool Exists(string gameFolder)
        {
            if (string.IsNullOrWhiteSpace(gameFolder)) return false;
            string path = Path.Combine(gameFolder, "mgs.cfg");
            return File.Exists(path);
        }

        public static bool IsConfigClean(string gameFolder)
        {
            if (!Exists(gameFolder)) return false;

            try
            {
                string path = Path.Combine(gameFolder, "mgs.cfg");
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < ActionOffset + (ActionCount * 4)) return false;

                for (int i = 0; i < ActionCount; i++)
                {
                    uint val = BitConverter.ToUInt32(bytes, ActionOffset + (i * 4));
                    if (val != CleanActionButtons[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RepairMgsCfg(string gameFolder, out string message)
        {
            if (!Exists(gameFolder))
            {
                message = "No se encontró el archivo mgs.cfg en la carpeta del juego.";
                return false;
            }

            try
            {
                string path = Path.Combine(gameFolder, "mgs.cfg");
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length < ActionOffset + (ActionCount * 4))
                {
                    message = "El archivo mgs.cfg tiene un tamaño inesperado o está corrupto.";
                    return false;
                }

                // Crear copia de seguridad si no existe
                string bakPath = Path.Combine(gameFolder, "mgs.cfg.bak");
                if (!File.Exists(bakPath))
                {
                    File.Copy(path, bakPath);
                }

                bool alreadyClean = true;
                for (int i = 0; i < ActionCount; i++)
                {
                    uint current = BitConverter.ToUInt32(bytes, ActionOffset + (i * 4));
                    if (current != CleanActionButtons[i])
                    {
                        alreadyClean = false;
                        byte[] cleanBytes = BitConverter.GetBytes(CleanActionButtons[i]);
                        Buffer.BlockCopy(cleanBytes, 0, bytes, ActionOffset + (i * 4), 4);
                    }
                }

                if (!alreadyClean)
                {
                    File.WriteAllBytes(path, bytes);
                    message = "mgs.cfg reparado y sincronizado con el estándar 1:1 de MGS.";
                    return true;
                }
                else
                {
                    message = "mgs.cfg ya tiene la configuración estándar óptima 1:1.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = $"Error al procesar mgs.cfg: {ex.Message}";
                return false;
            }
        }
    }
}

