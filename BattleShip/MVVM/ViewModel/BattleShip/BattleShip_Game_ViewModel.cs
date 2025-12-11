using System;
using System.Windows;
using System.Windows.Input;
using WPF_App.Core;
using BattleShipServer;

namespace WPF_App.MVVM.ViewModel
{
    internal class BattleShip_Game_ViewModel : ObservableObject
    {
        public event Action<int, int> OpponentHitted; // попадание по противнику
        public event Action<int, int, bool, int> OpponentSunk; // корабль противника потоплен
        public event Action<int, int> OpponentMissed; // промах по противнику
        public event Action<int, int> PlayerHitted; // попадание по игроку
        public event Action<int, int, bool, int> PlayerSunk; // корабль игрока потоплен
        public event Action<int, int> PlayerMissed; // промах по игроку

        private bool _isMyTurn;
        public bool IsMyTurn // мой ли ход
        {
            get => _isMyTurn;
            set { _isMyTurn = value; OnPropertyChanged(); }
        }

        private bool _isWinned;
        public bool IsWinned // выиграл ли
        {
            get => _isWinned;
            set { _isWinned = value; OnPropertyChanged(); }
        }

        private bool _isLost;
        public bool IsLost // проиграл ли
        {
            get => _isLost;
            set { _isLost = value; OnPropertyChanged(); }
        }

        private BattleShipServer.Client _client; // клиент
        private Player _player; // игрок

        public ICommand NewGameCommand { get; set; } // команда новой игры

        public BattleShip_Game_ViewModel()
        {
            _player = new Player("Player"); // создаем игрока
            NewGameCommand = new RelayCommand(
                _ => RequestNewGame(), // запрос новой игры
                _ => true); // всегда доступна
        }

        public void SetClient(BattleShipServer.Client client) // установка клиента
        {
            _client = client;
            SetupClientEvents(); // настройка событий клиента
        }

        private void SetupClientEvents() // настройка событий
        {
            if (_client == null) return;

            // Отписываемся от старых событий
            _client.GameStart -= Client_GameStart;
            _client.NewTurn -= Client_NewTurn;
            _client.Winned -= Client_Winned;
            _client.Lost -= Client_Lost;
            _client.Hit -= Client_Hit;
            _client.Sunk -= Client_Sunk;
            _client.Miss -= Client_Miss;
            _client.NewGame -= Client_NewGame;
            _client.MessageReceived -= Client_MessageReceived;
            _client.ConnectionLost -= Client_ConnectionLost;

            // Подписываемся на события
            _client.GameStart += Client_GameStart;
            _client.NewTurn += Client_NewTurn;
            _client.Winned += Client_Winned;
            _client.Lost += Client_Lost;
            _client.Hit += Client_Hit;
            _client.Sunk += Client_Sunk;
            _client.Miss += Client_Miss;
            _client.NewGame += Client_NewGame;
            _client.MessageReceived += Client_MessageReceived;
            _client.ConnectionLost += Client_ConnectionLost;
        }

        private void Client_ConnectionLost() // потеря соединения
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                if (!IsWinned && !IsLost) // если игра не завершена
                {
                    IsWinned = true; // Победа при отключении противника
                }
            });
        }

        private void Client_MessageReceived(string message, MessageSenderType senderType) // получено сообщение
        {
            // Просто логируем, не показываем уведомления
            Console.WriteLine($"[{senderType}] {message}"); // вывод в консоль
        }

        private void Client_NewGame() // новая игра
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                IsWinned = false; // сбрасываем победу
                IsLost = false; // сбрасываем поражение
                IsMyTurn = false; // сбрасываем ход

                // Сбрасываем состояние игрока для новой игры
                var player = GetPlayer(); // получаем игрока
                if (player != null)
                {
                    player.Reset(); // сбрасываем
                }

                // Если нужно, можно добавить логику перехода к экрану расстановки кораблей
                Console.WriteLine("Начинается новая игра!"); // сообщение в консоль
            });
        }

        private void Client_Miss(int x, int y) // промах
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                if (IsMyTurn) // если мой ход
                {
                    OpponentMissed?.Invoke(x, y); // промах по противнику
                }
                else // если ход противника
                {
                    PlayerMissed?.Invoke(x, y); // промах по игроку
                }
            });
        }

        private void Client_Sunk(int x, int y, bool isHorizontal, int size) // корабль потоплен
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                if (IsMyTurn) // если мой ход
                {
                    OpponentSunk?.Invoke(x, y, isHorizontal, size); // потоплен корабль противника
                }
                else // если ход противника
                {
                    PlayerSunk?.Invoke(x, y, isHorizontal, size); // потоплен корабль игрока
                }
            });
        }

        private void Client_Hit(int x, int y) // попадание
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                if (IsMyTurn) // если мой ход
                {
                    OpponentHitted?.Invoke(x, y); // попадание по противнику
                }
                else // если ход противника
                {
                    PlayerHitted?.Invoke(x, y); // попадание по игроку
                }
            });
        }

        private void Client_Winned() // победа
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                IsWinned = true; // устанавливаем победу
            });
        }

        private void Client_Lost() // поражение
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                IsLost = true; // устанавливаем поражение
            });
        }

        private void Client_NewTurn(bool myTurn) // новый ход
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                IsMyTurn = myTurn; // устанавливаем чей ход
            });
        }

        private void Client_GameStart() // начало игры
        {
            // Не показываем уведомление, просто логируем
            Console.WriteLine("Game started!"); // сообщение в консоль
        }

        public void OpponentFieldCellClicked(int x, int y) // клик по полю противника
        {
            if (IsMyTurn && !IsWinned && !IsLost && _client != null) // если мой ход и игра не завершена
            {
                _client.FireShot(x, y); // выстрел
            }
        }

        private void RequestNewGame() // запрос новой игры
        {
            _client?.RequestNewGame(); // отправляем запрос
        }

        public void SetPlayer(Player player) // установка игрока
        {
            _player = player; // сохраняем игрока
        }

        public Player GetPlayer() // получение игрока
        {
            return _player; // возвращаем игрока
        }
    }
}