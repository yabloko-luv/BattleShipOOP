using System;
using System.Windows.Input;
using WPF_App.Core;

namespace WPF_App.MVVM.ViewModel
{
    internal class BattleShip_Loading_ViewModel : ObservableObject
    {
        private string _loadingStatus = "Connecting to server..."; // статус загрузки
        public string LoadingStatus
        {
            get => _loadingStatus;
            set { _loadingStatus = value; OnPropertyChanged(); }
        }

        private double _loadingProgress = 30; // прогресс загрузки
        public double LoadingProgress
        {
            get => _loadingProgress;
            set { _loadingProgress = value; OnPropertyChanged(); }
        }

        private bool _showCancelButton = true; // показывать ли кнопку отмены
        public bool ShowCancelButton
        {
            get => _showCancelButton;
            set { _showCancelButton = value; OnPropertyChanged(); }
        }

        public ICommand CancelCommand { get; set; } // команда отмены

        public BattleShip_Loading_ViewModel()
        {
            CancelCommand = new RelayCommand(
                _ => CancelLoading(), // действие отмены
                _ => true); // всегда доступна
        }

        private void CancelLoading() // отмена загрузки
        {
            // Логика отмены загрузки
            LoadingStatus = "Cancelling..."; // статус отмены
            ShowCancelButton = false; // скрываем кнопку
        }

        public void UpdateStatus(string status, double progress) // обновление статуса
        {
            LoadingStatus = status; // новый статус
            LoadingProgress = progress; // новый прогресс
        }
    }
}