using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MgsPadEditor.Models;
using MgsPadEditor.Services;
using Microsoft.Win32;

namespace MgsPadEditor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IniFile _ini = new();
        private readonly DirectInputService _inputService = new();

        private string? _currentFilePath;
        private bool _isModified;
        private string _statusMessage = "Listo";
        private string _searchFilter = string.Empty;

        // General Tab
        private bool _oneLayout = true;
        private bool _dpadToAnalog = true;
        private bool _keyboardAndMouse = false;
        private int _mouseSpeed = 5;

        // Controller Tab
        private string? _selectedProfile;
        private string _profileGuid = string.Empty;
        private GamepadDevice? _selectedDevice;
        private string? _gameDirectory;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action<MappingRowViewModel, bool>? RequestCaptureModal { get; set; }
        public Action<GamepadDevice>? RequestTestModal { get; set; }

        public IniFile Ini => _ini;
        public DirectInputService InputService => _inputService;

        public string? GameDirectory
        {
            get => _gameDirectory;
            set
            {
                if (_gameDirectory != value)
                {
                    _gameDirectory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasGameDirectory));
                    OnPropertyChanged(nameof(GameDirectoryDisplay));
                    OnPropertyChanged(nameof(IsGameIniFound));
                }
            }
        }

        public bool HasGameDirectory => !string.IsNullOrWhiteSpace(_gameDirectory) && Directory.Exists(_gameDirectory);

        public string GameDirectoryDisplay => HasGameDirectory ? _gameDirectory! : "No configurada (haz clic en Cambiar Carpeta...)";

        public bool IsGameIniFound => HasGameDirectory && File.Exists(Path.Combine(_gameDirectory!, "dixi.ini"));

        private bool _isVisualMode = true;
        public bool IsVisualMode
        {
            get => _isVisualMode;
            set
            {
                if (_isVisualMode != value)
                {
                    _isVisualMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTableMode));
                }
            }
        }

        public bool IsTableMode
        {
            get => !_isVisualMode;
            set => IsVisualMode = !value;
        }

        public ObservableCollection<string> Profiles { get; } = new();
        public ObservableCollection<MappingRowViewModel> ControllerRows { get; } = new();
        public ObservableCollection<MappingRowViewModel> KeyboardMouseRows { get; } = new();
        public ObservableCollection<GamepadDevice> Devices { get; } = new();

        public string WindowTitle
        {
            get
            {
                string fileName = string.IsNullOrEmpty(_currentFilePath) ? "Sin archivo" : Path.GetFileName(_currentFilePath);
                string mod = _isModified ? " *" : "";
                return $"MGS PAD Editor v2.0 (.NET) — {fileName}{mod}";
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value;
                    OnPropertyChanged();
                    ApplySearchFilter();
                }
            }
        }

        // --- GENERAL SETTINGS ---
        public bool OneLayout
        {
            get => _oneLayout;
            set
            {
                if (_oneLayout != value)
                {
                    _oneLayout = value;
                    _ini.SetGeneralValue("onelayout", value ? "TRUE" : "FALSE");
                    IsModified = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool DpadToAnalog
        {
            get => _dpadToAnalog;
            set
            {
                if (_dpadToAnalog != value)
                {
                    _dpadToAnalog = value;
                    _ini.SetGeneralValue("dpadtoanalog", value ? "TRUE" : "FALSE");
                    IsModified = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool KeyboardAndMouse
        {
            get => _keyboardAndMouse;
            set
            {
                if (_keyboardAndMouse != value)
                {
                    _keyboardAndMouse = value;
                    _ini.SetGeneralValue("keyboardandmouse", value ? "TRUE" : "FALSE");
                    IsModified = true;
                    OnPropertyChanged();
                }
            }
        }

        public int MouseSpeed
        {
            get => _mouseSpeed;
            set
            {
                if (_mouseSpeed != value)
                {
                    _mouseSpeed = Math.Clamp(value, 0, 10);
                    _ini.SetGeneralValue("mousespeed", _mouseSpeed.ToString());
                    IsModified = true;
                    OnPropertyChanged();
                }
            }
        }

        // --- PROFILE & CONTROLLER ---
        public string? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged();
                    LoadProfileData(value);
                }
            }
        }

        public string ProfileGuid
        {
            get => _profileGuid;
            set
            {
                if (_profileGuid != value)
                {
                    _profileGuid = value;
                    OnPropertyChanged();
                    UpdateProfileGuid(value);
                }
            }
        }

        public GamepadDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();
                    StatusMessage = value != null ? $"Mando activo: {value.Name}" : "Ningún mando seleccionado";
                }
            }
        }

        // --- COMMANDS ---
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand AddProfileCommand { get; }
        public ICommand RemoveProfileCommand { get; }
        public ICommand CloneProfileCommand { get; }
        public ICommand AutoMatchCommand { get; }
        public ICommand RefreshDevicesCommand { get; }
        public ICommand TestDeviceCommand { get; }
        public ICommand RepairMgsCfgCommand { get; }
        public ICommand PresetXboxCommand { get; }
        public ICommand PresetDualShock4Command { get; }
        public ICommand PresetDualSenseCommand { get; }
        public ICommand ApplyDualSensePs1ProfileCommand { get; }
        public ICommand SelectGameFolderCommand { get; }
        public ICommand OpenGameFolderInExplorerCommand { get; }
        public ICommand AutoDetectGameCommand { get; }

        public MainViewModel()
        {
            OpenCommand = new RelayCommand(_ => OpenFile());
            SaveCommand = new RelayCommand(_ => SaveFile());
            SaveAsCommand = new RelayCommand(_ => SaveAsFile());
            AddProfileCommand = new RelayCommand(AddProfilePrompt);
            RemoveProfileCommand = new RelayCommand(RemoveCurrentProfile, () => !string.IsNullOrEmpty(SelectedProfile));
            CloneProfileCommand = new RelayCommand(CloneCurrentProfile, () => !string.IsNullOrEmpty(SelectedProfile));
            AutoMatchCommand = new RelayCommand(_ => AutoMatchConnectedDevice());
            RefreshDevicesCommand = new RelayCommand(_ => RefreshConnectedDevices());
            TestDeviceCommand = new RelayCommand(_ => OpenTestWindow(), () => SelectedDevice != null);
            PresetXboxCommand = new RelayCommand(_ => ApplyPreset(MappingDefinitions.GetPresetXbox(), "Xbox"));
            PresetDualShock4Command = new RelayCommand(_ => ApplyPreset(MappingDefinitions.GetPresetDualShock4(), "DualShock 4"));
            PresetDualSenseCommand = new RelayCommand(_ => ApplyPreset(MappingDefinitions.GetPresetDualSensePs1Classic(), "MGS 1 Clásico PS1 (DualSense)"));
            ApplyDualSensePs1ProfileCommand = new RelayCommand(_ => ApplyDualSensePs1Profile());

            SelectGameFolderCommand = new RelayCommand(_ => SelectGameFolder());
            OpenGameFolderInExplorerCommand = new RelayCommand(_ => OpenGameFolderInExplorer(), () => HasGameDirectory);
            AutoDetectGameCommand = new RelayCommand(_ => AutoDetectGame());
            RepairMgsCfgCommand = new RelayCommand(_ => RepairMgsCfg(), () => HasGameDirectory);

            RefreshConnectedDevices();

            // 1. Try saved settings or auto-detected game path
            var settings = AppSettingsService.Load();
            if (!string.IsNullOrWhiteSpace(settings.GameDirectory) && Directory.Exists(settings.GameDirectory))
            {
                _gameDirectory = settings.GameDirectory;
                string gameIni = Path.Combine(_gameDirectory, "dixi.ini");
                if (File.Exists(gameIni))
                {
                    LoadFile(gameIni);
                }
            }

            // 2. Fallback to local / parent search if no file loaded yet
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                string? defaultIni = FindDefaultDixiIni();
                if (defaultIni != null && File.Exists(defaultIni))
                {
                    LoadFile(defaultIni);
                }
            }
        }

        private static string? FindDefaultDixiIni()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(current, "dixi.ini");
                if (File.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            string cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "dixi.ini");
            if (File.Exists(cwdCandidate)) return cwdCandidate;
            return null;
        }

        public void LoadFile(string path)
        {
            try
            {
                _ini.Read(path);
                _currentFilePath = path;

                // Load General settings
                _oneLayout = string.Equals(_ini.GetGeneralValue("onelayout"), "TRUE", StringComparison.OrdinalIgnoreCase);
                _dpadToAnalog = string.Equals(_ini.GetGeneralValue("dpadtoanalog"), "TRUE", StringComparison.OrdinalIgnoreCase);
                _keyboardAndMouse = string.Equals(_ini.GetGeneralValue("keyboardandmouse"), "TRUE", StringComparison.OrdinalIgnoreCase);
                if (int.TryParse(_ini.GetGeneralValue("mousespeed"), out int speed))
                {
                    _mouseSpeed = speed;
                }
                else
                {
                    _mouseSpeed = 5;
                }

                OnPropertyChanged(nameof(OneLayout));
                OnPropertyChanged(nameof(DpadToAnalog));
                OnPropertyChanged(nameof(KeyboardAndMouse));
                OnPropertyChanged(nameof(MouseSpeed));

                // Load Profiles
                Profiles.Clear();
                foreach (var sec in _ini.Sections)
                {
                    if (!string.Equals(sec.Name, "general", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(sec.Name, "keyboardandmouse", StringComparison.OrdinalIgnoreCase))
                    {
                        Profiles.Add(sec.DisplayName);
                    }
                }

                // Load Keyboard and Mouse
                LoadKeyboardAndMouseData();

                IsModified = false;
                StatusMessage = $"Cargado correctamente: {Path.GetFileName(path)}";

                if (Profiles.Count > 0)
                {
                    SelectedProfile = Profiles[0];
                }
                else
                {
                    SelectedProfile = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir el archivo INI:\n{ex.Message}", "Error de Apertura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProfileData(string? profileDisplayName)
        {
            ControllerRows.Clear();
            if (string.IsNullOrEmpty(profileDisplayName))
            {
                ProfileGuid = string.Empty;
                return;
            }

            var section = _ini.GetSectionByDisplayName(profileDisplayName);
            if (section == null) return;

            _profileGuid = section.GetValue("guid") ?? string.Empty;
            OnPropertyChanged(nameof(ProfileGuid));

            var currentKvs = section.GetAllKeyValues();
            var targetOptions = MappingDefinitions.GetPadTargetOptions();

            // Populate all standard game inputs
            foreach (var inputKey in MappingDefinitions.PreferredInputOrder)
            {
                string desc = MappingDefinitions.GameActionDescriptions.GetValueOrDefault(inputKey, "");
                string currentVal = currentKvs.GetValueOrDefault(inputKey, "");

                var row = new MappingRowViewModel(
                    inputKey,
                    desc,
                    currentVal,
                    targetOptions,
                    onCapture: r => RequestCaptureModal?.Invoke(r, false)
                );

                row.OnValueChanged = r =>
                {
                    if (string.IsNullOrEmpty(r.TargetValue) || r.TargetValue == "dev_null")
                    {
                        if (r.TargetValue == "dev_null")
                            section.SetValue(r.InputKey, "dev_null");
                        else
                            section.RemoveValue(r.InputKey);
                    }
                    else
                    {
                        section.SetValue(r.InputKey, r.TargetValue);
                    }
                    IsModified = true;
                    StatusMessage = $"Mapeo actualizado: {r.InputKey} = {r.TargetValue}";
                };

                ControllerRows.Add(row);
            }

            ApplySearchFilter();
            NotifyVisualRowProperties();
        }

        public MappingRowViewModel? GetControllerRow(string key) =>
            ControllerRows.FirstOrDefault(r => string.Equals(r.InputKey, key, StringComparison.OrdinalIgnoreCase));

        public MappingRowViewModel? RowSquare => GetControllerRow("pad_button0");
        public MappingRowViewModel? RowCross => GetControllerRow("pad_button1");
        public MappingRowViewModel? RowCircle => GetControllerRow("pad_button2");
        public MappingRowViewModel? RowTriangle => GetControllerRow("pad_button3");
        public MappingRowViewModel? RowL1 => GetControllerRow("pad_button4");
        public MappingRowViewModel? RowR1 => GetControllerRow("pad_button5");
        public MappingRowViewModel? RowL2 => GetControllerRow("pad_button6") ?? GetControllerRow("pad_rxaxisplus") ?? GetControllerRow("pad_zaxisplus");
        public MappingRowViewModel? RowR2 => GetControllerRow("pad_button7") ?? GetControllerRow("pad_ryaxisplus") ?? GetControllerRow("pad_rzaxisplus");
        public MappingRowViewModel? RowCreate => GetControllerRow("pad_button8");
        public MappingRowViewModel? RowOptions => GetControllerRow("pad_button9");
        public MappingRowViewModel? RowL3 => GetControllerRow("pad_button10");
        public MappingRowViewModel? RowR3 => GetControllerRow("pad_button11");
        public MappingRowViewModel? RowPS => GetControllerRow("pad_button12");
        public MappingRowViewModel? RowTouchpad => GetControllerRow("pad_button13");
        public MappingRowViewModel? RowDpadUp => GetControllerRow("pad_dpadup");
        public MappingRowViewModel? RowDpadDown => GetControllerRow("pad_dpaddown");
        public MappingRowViewModel? RowDpadLeft => GetControllerRow("pad_dpadleft");
        public MappingRowViewModel? RowDpadRight => GetControllerRow("pad_dpadright");

        public void NotifyVisualRowProperties()
        {
            OnPropertyChanged(nameof(RowCross));
            OnPropertyChanged(nameof(RowCircle));
            OnPropertyChanged(nameof(RowSquare));
            OnPropertyChanged(nameof(RowTriangle));
            OnPropertyChanged(nameof(RowL1));
            OnPropertyChanged(nameof(RowR1));
            OnPropertyChanged(nameof(RowL2));
            OnPropertyChanged(nameof(RowR2));
            OnPropertyChanged(nameof(RowCreate));
            OnPropertyChanged(nameof(RowOptions));
            OnPropertyChanged(nameof(RowL3));
            OnPropertyChanged(nameof(RowR3));
            OnPropertyChanged(nameof(RowPS));
            OnPropertyChanged(nameof(RowTouchpad));
            OnPropertyChanged(nameof(RowDpadUp));
            OnPropertyChanged(nameof(RowDpadDown));
            OnPropertyChanged(nameof(RowDpadLeft));
            OnPropertyChanged(nameof(RowDpadRight));
        }

        private void LoadKeyboardAndMouseData()
        {
            KeyboardMouseRows.Clear();
            var section = _ini.GetSectionByName("keyboardandmouse");
            if (section == null)
            {
                section = _ini.AddSection("keyboardandmouse");
            }

            var currentKvs = section.GetAllKeyValues();
            var targetOptions = MappingDefinitions.GetKeyboardAndMouseTargetOptions();

            foreach (var inputKey in MappingDefinitions.PreferredInputOrder)
            {
                string desc = MappingDefinitions.GameActionDescriptions.GetValueOrDefault(inputKey, "");
                string currentVal = currentKvs.GetValueOrDefault(inputKey, "");

                var row = new MappingRowViewModel(
                    inputKey,
                    desc,
                    currentVal,
                    targetOptions,
                    onCapture: r => RequestCaptureModal?.Invoke(r, true)
                );

                row.OnValueChanged = r =>
                {
                    if (string.IsNullOrEmpty(r.TargetValue) || r.TargetValue == "dev_null")
                    {
                        if (r.TargetValue == "dev_null")
                            section.SetValue(r.InputKey, "dev_null");
                        else
                            section.RemoveValue(r.InputKey);
                    }
                    else
                    {
                        section.SetValue(r.InputKey, r.TargetValue);
                    }
                    IsModified = true;
                    StatusMessage = $"Teclado/Ratón actualizado: {r.InputKey} = {r.TargetValue}";
                };

                KeyboardMouseRows.Add(row);
            }
        }

        private void UpdateProfileGuid(string newGuid)
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;
            var section = _ini.GetSectionByDisplayName(SelectedProfile);
            if (section != null)
            {
                if (string.IsNullOrWhiteSpace(newGuid))
                    section.RemoveValue("guid");
                else
                    section.SetValue("guid", newGuid.Trim());

                IsModified = true;
                StatusMessage = $"GUID actualizada para [{SelectedProfile}]";
            }
        }

        public void ApplySearchFilter()
        {
            string filter = _searchFilter.Trim().ToLowerInvariant();
            foreach (var row in ControllerRows)
            {
                row.IsVisible = string.IsNullOrEmpty(filter) ||
                                row.InputKey.ToLowerInvariant().Contains(filter) ||
                                row.Description.ToLowerInvariant().Contains(filter) ||
                                row.TargetValue.ToLowerInvariant().Contains(filter);
            }

            foreach (var row in KeyboardMouseRows)
            {
                row.IsVisible = string.IsNullOrEmpty(filter) ||
                                row.InputKey.ToLowerInvariant().Contains(filter) ||
                                row.Description.ToLowerInvariant().Contains(filter) ||
                                row.TargetValue.ToLowerInvariant().Contains(filter);
            }
        }

        public void RefreshConnectedDevices()
        {
            Devices.Clear();
            var list = _inputService.EnumerateDevices();
            foreach (var d in list)
            {
                Devices.Add(d);
            }

            if (Devices.Count > 0)
            {
                SelectedDevice = Devices[0];
                StatusMessage = $"Se detectaron {Devices.Count} mando(s) conectado(s)";
            }
            else
            {
                SelectedDevice = null;
                StatusMessage = "No se detectaron mandos conectados";
            }
        }

        public void AutoMatchConnectedDevice()
        {
            if (SelectedDevice == null)
            {
                MessageBox.Show("Conecta o selecciona un mando primero y pulsa 'Actualizar Mandos'.", "Auto-match", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string targetGuid = SelectedDevice.DixiGuid.ToUpperInvariant();
            (int devVid, int devPid) = (SelectedDevice.VendorId, SelectedDevice.ProductId);

            // 1. Exact GUID Match
            foreach (var sec in _ini.Sections)
            {
                string? secGuid = sec.GetValue("guid")?.ToUpperInvariant();
                if (!string.IsNullOrEmpty(secGuid) && secGuid == targetGuid)
                {
                    SelectedProfile = sec.DisplayName;
                    StatusMessage = $"¡Emparejado por GUID!: {SelectedDevice.Name} -> [{sec.DisplayName}]";
                    return;
                }
            }

            // 2. Match by VID & PID in GUID
            if (devVid > 0 && devPid > 0)
            {
                foreach (var sec in _ini.Sections)
                {
                    string? secGuid = sec.GetValue("guid");
                    if (!string.IsNullOrEmpty(secGuid))
                    {
                        var (vid, pid) = DirectInputService.ExtractVidPid(Guid.TryParse(secGuid, out var g) ? g : Guid.Empty);
                        if (vid == devVid && pid == devPid)
                        {
                            SelectedProfile = sec.DisplayName;
                            StatusMessage = $"¡Emparejado por VID/PID!: {SelectedDevice.Name} -> [{sec.DisplayName}]";
                            return;
                        }
                    }
                }
            }

            // 3. Name similarity match
            string cleanDevName = SelectedDevice.Name.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
            foreach (var sec in _ini.Sections)
            {
                string cleanSecName = sec.Name.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
                if (cleanDevName.Contains(cleanSecName) || cleanSecName.Contains(cleanDevName))
                {
                    SelectedProfile = sec.DisplayName;
                    StatusMessage = $"¡Emparejado por nombre!: {SelectedDevice.Name} -> [{sec.DisplayName}]";
                    return;
                }
            }

            MessageBox.Show($"No se encontró ningún perfil que coincida con:\n{SelectedDevice.Name}\nGUID: {targetGuid}\n\nPuedes agregar un perfil nuevo para este mando pulsando '+ Nuevo Perfil'.", "Auto-match", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ApplyPreset(Dictionary<string, string> preset, string presetName)
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;

            var ans = MessageBox.Show($"¿Deseas aplicar el preset '{presetName}' al perfil actual [{SelectedProfile}]?\nEsto reemplazará los mapeos existentes.", "Aplicar Preset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ans != MessageBoxResult.Yes) return;

            var section = _ini.GetSectionByDisplayName(SelectedProfile);
            if (section == null) return;

            foreach (var kv in preset)
            {
                section.SetValue(kv.Key, kv.Value);
            }

            LoadProfileData(SelectedProfile);
            IsModified = true;
            StatusMessage = $"Preset '{presetName}' aplicado a [{SelectedProfile}]";
        }

        public void ApplyDualSensePs1Profile()
        {
            // Ensure dpadtoanalog is FALSE for proper dual stick & dpad operation
            DpadToAnalog = false;

            // Check if sony_dualsense or similar exists, otherwise create it
            var sec = _ini.GetSectionByName("sony_dualsense") ?? _ini.GetSectionByDisplayName("sony_dualsense");
            if (sec == null)
            {
                sec = _ini.AddSection("sony_dualsense", "0CE6054C-0000-0000-0000-504944564944");
                Profiles.Add(sec.DisplayName);
            }

            SelectedProfile = sec.DisplayName;
            ApplyPreset(MappingDefinitions.GetPresetDualSensePs1Classic(), "MGS 1 Clásico PS1 (DualSense)");
        }

        private void AddProfilePrompt()
        {
            var dialog = new Views.AddProfileDialog
            {
                Owner = Application.Current.MainWindow,
                GuidInput = SelectedDevice != null ? SelectedDevice.DixiGuid : ""
            };

            if (dialog.ShowDialog() == true)
            {
                string name = dialog.ProfileName.Trim();
                if (string.IsNullOrEmpty(name)) return;

                var sec = _ini.AddSection(name, dialog.GuidInput.Trim());
                Profiles.Add(sec.DisplayName);
                SelectedProfile = sec.DisplayName;
                IsModified = true;
                StatusMessage = $"Perfil [{sec.DisplayName}] creado con éxito";
            }
        }

        private void RemoveCurrentProfile()
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;

            var ans = MessageBox.Show($"¿Estás seguro de que deseas eliminar el perfil [{SelectedProfile}]?", "Eliminar Perfil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (ans != MessageBoxResult.Yes) return;

            string toRemove = SelectedProfile;
            _ini.RemoveSection(toRemove);
            Profiles.Remove(toRemove);

            SelectedProfile = Profiles.FirstOrDefault();
            IsModified = true;
            StatusMessage = $"Perfil [{toRemove}] eliminado";
        }

        private void CloneCurrentProfile()
        {
            if (string.IsNullOrEmpty(SelectedProfile)) return;
            var currentSec = _ini.GetSectionByDisplayName(SelectedProfile);
            if (currentSec == null) return;

            string newName = $"{currentSec.Name}_copia";
            var newSec = _ini.AddSection(newName, currentSec.GetValue("guid"));

            foreach (var kv in currentSec.GetAllKeyValues())
            {
                if (kv.Key != "guid")
                {
                    newSec.SetValue(kv.Key, kv.Value);
                }
            }

            Profiles.Add(newSec.DisplayName);
            SelectedProfile = newSec.DisplayName;
            IsModified = true;
            StatusMessage = $"Perfil duplicado como [{newSec.DisplayName}]";
        }

        public void SelectGameFolder()
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta de instalación de Metal Gear Solid",
                InitialDirectory = HasGameDirectory ? _gameDirectory : (Directory.Exists(@"K:\Juegos") ? @"K:\Juegos" : null)
            };

            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.FolderName))
            {
                SetGameDirectory(dlg.FolderName);
            }
        }

        public void SetGameDirectory(string folder)
        {
            if (!Directory.Exists(folder)) return;

            GameDirectory = folder;
            var settings = AppSettingsService.Load();
            settings.GameDirectory = folder;
            AppSettingsService.Save(settings);

            string iniPath = Path.Combine(folder, "dixi.ini");
            if (File.Exists(iniPath))
            {
                if (PromptSaveIfModified() == false) return;
                LoadFile(iniPath);
                StatusMessage = $"Carpeta vinculada: {folder} (dixi.ini cargado)";
            }
            else
            {
                var res = MessageBox.Show(
                    $"No se encontró el archivo 'dixi.ini' en:\n{folder}\n\n¿Deseas crear un archivo dixi.ini nuevo en esta carpeta con la configuración por defecto?",
                    "dixi.ini no encontrado",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    string localTemplate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dixi.ini");
                    if (File.Exists(localTemplate))
                    {
                        File.Copy(localTemplate, iniPath, true);
                    }
                    else
                    {
                        _ini.Write(iniPath);
                    }
                    LoadFile(iniPath);
                    StatusMessage = $"Creado dixi.ini en: {folder}";
                }
            }
        }

        public void OpenGameFolderInExplorer()
        {
            if (HasGameDirectory)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = _gameDirectory!,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el explorador:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void AutoDetectGame()
        {
            string? detected = AppSettingsService.DetectGogGamePath();
            if (!string.IsNullOrEmpty(detected) && Directory.Exists(detected))
            {
                SetGameDirectory(detected);
                MessageBox.Show($"¡Juego detectado exitosamente en el registro de Windows!\n\nRuta: {detected}", "Juego Detectado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No se encontró la instalación de Metal Gear Solid en el registro.\nPor favor, pulsa 'Cambiar Carpeta' y selecciónala manualmente.", "Auto-detección", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void OpenFile()
        {
            if (PromptSaveIfModified() == false) return;

            var dlg = new OpenFileDialog
            {
                Title = "Abrir dixi.ini",
                Filter = "Archivos de Configuración (*.ini)|*.ini|Todos los archivos (*.*)|*.*",
                DefaultExt = ".ini"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadFile(dlg.FileName);
            }
        }

        public void RepairMgsCfg()
        {
            if (!HasGameDirectory)
            {
                MessageBox.Show("Selecciona primero la carpeta donde está instalado Metal Gear Solid.", "Carpeta no seleccionada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MgsCfgService.RepairMgsCfg(_gameDirectory!, out string msg))
            {
                StatusMessage = msg;
                MessageBox.Show(
                    "¡Configuración mgs.cfg sincronizada exitosamente!\n\n" +
                    "Se han restablecido los botones internos de Metal Gear Solid a su estándar 1:1.\n" +
                    "Ahora las asignaciones de dixi.ini tendrán efecto directo en el juego sin conflictos secundarios.",
                    "Sincronización Completada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(msg, "Información de mgs.cfg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsFile();
                return;
            }

            try
            {
                _ini.Write(_currentFilePath);
                IsModified = false;

                string extra = "";
                if (HasGameDirectory && MgsCfgService.Exists(_gameDirectory!))
                {
                    if (MgsCfgService.RepairMgsCfg(_gameDirectory!, out _))
                    {
                        extra = " + mgs.cfg sincronizado";
                    }
                }

                StatusMessage = $"Guardado correctamente en: {_currentFilePath} (Copia .bak creada){extra}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveAsFile()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Guardar dixi.ini Como...",
                Filter = "Archivos de Configuración (*.ini)|*.ini|Todos los archivos (*.*)|*.*",
                DefaultExt = ".ini",
                FileName = string.IsNullOrEmpty(_currentFilePath) ? "dixi.ini" : Path.GetFileName(_currentFilePath)
            };

            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                SaveFile();
            }
        }

        public bool PromptSaveIfModified()
        {
            if (!IsModified) return true;

            var res = MessageBox.Show("Tienes cambios sin guardar. ¿Deseas guardarlos antes de continuar?", "Cambios sin guardar", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                SaveFile();
                return true;
            }
            if (res == MessageBoxResult.No)
            {
                return true;
            }
            return false; // Cancel
        }

        private void OpenTestWindow()
        {
            if (SelectedDevice != null)
            {
                RequestTestModal?.Invoke(SelectedDevice);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
