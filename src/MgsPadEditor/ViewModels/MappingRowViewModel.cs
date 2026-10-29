using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MgsPadEditor.Models;

namespace MgsPadEditor.ViewModels
{
    public class MappingRowViewModel : INotifyPropertyChanged
    {
        private string _inputKey = string.Empty;
        private string _description = string.Empty;
        private string _targetValue = string.Empty;
        private bool _isVisible = true;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action<MappingRowViewModel>? OnValueChanged { get; set; }

        public string InputKey
        {
            get => _inputKey;
            set { _inputKey = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string TargetValue
        {
            get => _targetValue;
            set
            {
                if (_targetValue != value)
                {
                    _targetValue = value;
                    OnPropertyChanged();
                    OnValueChanged?.Invoke(this);
                }
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public List<TargetOption> AvailableTargets { get; set; } = new();

        public ICommand CaptureCommand { get; set; }

        public MappingRowViewModel(string inputKey, string description, string targetValue, List<TargetOption> availableTargets, Action<MappingRowViewModel> onCapture)
        {
            _inputKey = inputKey;
            _description = description;
            _targetValue = targetValue;
            AvailableTargets = availableTargets;
            CaptureCommand = new RelayCommand(_ => onCapture(this));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
