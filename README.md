# 🎮 MGS PAD Editor (v2.0 Tactical Edition)

> **Configurador Táctico de Mandos para Metal Gear Solid 1 (PC / GOG)**  
> *Desarrollado por Luis Jimenez Aka "Hostility"*

[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-00F0FF.svg?style=flat-square)]()
[![Framework](https://img.shields.io/badge/.NET-8.0%20WPF-00FF66.svg?style=flat-square)]()
[![License](https://img.shields.io/badge/License-MIT-FFE600.svg?style=flat-square)](LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-h057co-white.svg?style=flat-square)](https://github.com/h057co)

---

## ⚡ Descripción

**MGS PAD Editor** es una herramienta moderna desarrollada en C# y WPF con interfaz de **Digital Brutalismo Táctico**, creada para resolver de forma definitiva la configuración y mapeo de mandos modernos en **Metal Gear Solid 1 para PC** (ediciones clásicas y GOG).

El juego original (`mgsi.exe`) posee limitaciones complejas: conflictos entre el wrapper DirectInput (`dixi.ini`) y su archivo de configuración binario interno (`mgs.cfg`), inversión de índices de botones y falta de soporte nativo para controles ergonómicos como el **DualSense de PS5** o **Xbox Series/One**. **MGS PAD Editor** automatiza todo este proceso en un único ejecutable autónomo.

---

## 🚀 Características Principales

* 🎮 **Vista Visual Interactiva DualSense (PS5):**
  * Representación vectorial táctica del mando de PlayStation 5.
  * Mapeo ergonómico directo de los 14 botones y ejes con captura automática.
* 🎯 **Telemetría y Probador en Vivo (Telemetry Scope):**
  * Radar XY para monitorear en tiempo real los sticks analógicos izquierdo y derecho.
  * Barras de calibración en tiempo real para gatillos L2 y R2.
* 🛠 **Sincronización y Reparación Hexadecimal de `mgs.cfg`:**
  * Motor nativo de ingeniería inversa que repara la tabla binaria interna de 14 acciones en `mgs.cfg` (offset `0x25287c`), garantizando que el juego reconozca el 100% de los botones sin conflictos.
* ⭐ **Preset Clásico PS1:**
  * Configuración idéntica a la experiencia original de PlayStation (Cuadrado = Disparo/Confirmar, Cruz = Agacharse/Cancelar, Círculo = Disparo secundario, Triángulo = Vista en primera persona, R1 = Arma, L1 = Objeto, R2 = Munición anterior, L2 = Objeto anterior).
* 📁 **Gestión Externa de Carpetas:**
  * No necesitas copiar el ejecutable dentro de la carpeta del juego. Puedes seleccionar la ubicación de MGS o auto-detectarla desde el registro de GOG.
* ⌨️ **Soporte Híbrido Teclado y Ratón:**
  * Configuración completa de la sección `[keyboardandmouse]`.
* 📦 **Ejecutable Único (Single-File):**
  * Todo el runtime de .NET 8 y librerías nativas vienen empaquetadas en un único `.exe`.

---

## 📥 Descarga y Uso

1. Descarga el ejecutable autónomo **`MgsPadEditor.exe`** desde la sección [Releases](https://github.com/h057co/MGS-PAD-Editor/releases).
2. Ejecuta `MgsPadEditor.exe` (no requiere instalación).
3. Selecciona la carpeta donde tienes instalado Metal Gear Solid (o pulsa `DETECTAR GOG`).
4. Selecciona tu mando en el menú desplegable (o pulsa `AUTO-EMPAREJAR`).
5. Pulsa en el menú superior **`[ PRESETS RÁPIDOS ] → ⭐ MGS 1 Clásico PS1 (DualSense PS5)`** (o personaliza tus botones con el botón `CAPTURAR`).
6. Pulsa **`💾 GUARDAR`**. La aplicación sincronizará automáticamente `dixi.ini` y reparará `mgs.cfg`.
7. ¡Ejecuta el juego y disfruta!

---

## 🛠 Requisitos

* **Sistema Operativo:** Windows 10 o Windows 11 (64-bit).
* **Juego:** Metal Gear Solid 1 PC (GOG o CD original con el wrapper `dixi.ini`).
* **Mando:** Cualquier mando compatible con Windows (PS5 DualSense, PS4 DualShock, Xbox Series/One/360, mandos DirectInput genéricos).

---

## 🏗 Compilación desde Código Fuente

Requiere [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):

```powershell
# Clonar el repositorio
git clone https://github.com/h057co/MGS-PAD-Editor.git
cd MGS-PAD-Editor

# Compilar y ejecutar pruebas
dotnet test

# Generar el ejecutable único Release
dotnet publish src/MgsPadEditor/MgsPadEditor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

---

## ⚖️ Licencia y Autoría

* **Autor:** Luis Jimenez Aka **"Hostility"** ([GitHub @h057co](https://github.com/h057co))
* **Licencia:** [MIT License](LICENSE) — Software libre y gratuito para toda la comunidad.
* *Metal Gear Solid es marca registrada de Konami Digital Entertainment.*

