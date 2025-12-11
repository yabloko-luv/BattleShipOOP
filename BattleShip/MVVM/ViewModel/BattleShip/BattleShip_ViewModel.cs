using BattleShipServer;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Input;
using WPF_App.Core;
using WPF_App.MVVM.Model;
using WPF_App.MVVM.View;

namespace WPF_App.MVVM.ViewModel
{
    internal class BattleShip_ViewModel : ObservableObject
    {
        private object _currentView;
        public object CurrentView // текущее представление
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private bool isConnected;
        public bool IsConnected // подключен ли к серверу
        {
            get => isConnected;
            set { isConnected = value; OnPropertyChanged(); }
        }

        private string _messageToSend;
        public string MessageToSend // сообщение для отправки
        {
            get => _messageToSend;
            set { _messageToSend = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Message> ChatMessages { get; set; } // коллекция сообщений чата

        private Player _player; // игрок
        private Client _client; // клиент

        public ICommand SendMessageCommand { get; } // команда отправки сообщения

        public BattleShip_ViewModel()
        {
            var connectionView = new BattleShip_Connection_View(); // представление подключения
            var vm = connectionView.DataContext as BattleShip_Connection_ViewModel; // получаем ViewModel
            vm.ConnectedToServer += ConnectionView_ConnectedToServer; // подписываемся на событие подключения

            _player = new Player("Me"); // создаем игрока

            this.CurrentView = connectionView; // устанавливаем представление подключения

            ChatMessages = new ObservableCollection<Model.Message>(); // инициализируем коллекцию сообщений

            StartServer(); // запускаем сервер

            SendMessageCommand = new RelayCommand(
                _ => SendChatMessage(), // отправка сообщения
                _ => IsConnected && !string.IsNullOrEmpty(MessageToSend)); // условие: подключен и есть текст
        }


        private void StartServer() // запуск сервера
        {
            Server server = new Server(); // создаем сервер
            try
            {
                _ = server.StartServer(); // запускаем сервер
            }
            catch (Exception)
            {
                Console.WriteLine("Server already running"); // сервер уже запущен
            }
        }

        private void SendChatMessage() // отправка сообщения в чат
        {
            if (string.IsNullOrEmpty(MessageToSend)) return; // если сообщение пустое

            // Отправляем на сервер
            _client.SendChatMessage(MessageToSend); // отправка через клиент
            MessageToSend = string.Empty; // очищаем поле ввода
        }

        private void ConnectionView_ConnectedToServer(Client client) // подключение к серверу
        {
            _client = client; // сохраняем клиент
            _client.ShipPlacing += Client_ShipPlacing; // подписываемся на расстановку кораблей
            _client.MessageReceived += Client_MessageReceived; // подписываемся на получение сообщений
            _client.NewGame += Client_NewGame; // подписываемся на новую игру
            IsConnected = true; // устанавливаем флаг подключения

            this.CurrentView = new BattleShip_Loading_View(); // переключаем на представление загрузки
        }

        private void Client_MessageReceived(string arg1, MessageSenderType arg2) // получено сообщение
        {
            Message message = new Message(arg1) // создаем сообщение
            {
                IsMine = arg2 == MessageSenderType.Player, // мое ли сообщение
                IsServer = arg2 == MessageSenderType.Server // от сервера ли
            };

            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                ChatMessages.Add(message); // добавляем в коллекцию
            });
        }

        private void Client_ShipPlacing() // начало расстановки кораблей
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                var shipPlacementView = new BattleShip_ShipPlacement_View(); // создаем представление расстановки
                var vm = shipPlacementView.DataContext as BattleShip_ShipPlacement_ViewModel; // получаем ViewModel
                vm.PlayerReady += ShipPlacement_PlayerReady; // подписываемся на готовность игрока
                vm.SetClient(_client); // передаем клиент
                vm.SetPlayer(_player); // передаем игрока

                this.CurrentView = shipPlacementView; // переключаем представление
            });
        }

        private void Client_NewGame() // новая игра
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                _player.Reset(); // сбрасываем игрока
                this.CurrentView = new BattleShip_Loading_View(); // переключаем на загрузку
            });
        }


        private void ShipPlacement_PlayerReady() // игрок готов
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                var gameView = new BattleShip_Game_View(); // создаем представление игры
                var vm = gameView.DataContext as BattleShip_Game_ViewModel; // получаем ViewModel
                vm.SetClient(_client); // передаем клиент
                vm.SetPlayer(_player); // передаем игрока

                this.CurrentView = gameView; // переключаем представление
            });
        }
    }
}