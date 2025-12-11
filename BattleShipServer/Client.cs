using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipServer
{
    public class Client
    {
        public event Action GameStart; // начало игры
        public event Action Winned; // победа
        public event Action Lost; // поражение
        public event Action NewGame; // новая игра
        public event Action<bool> NewTurn; // новый ход
        public event Action ShipPlacing; // расстановка кораблей
        public event Action<int, int, bool, int> ShipPlaced; // корабль размещен
        public event Action InvalidShipPlacement; // неверное размещение
        public event Action<int, int> Hit; // попадание
        public event Action<int, int, bool, int> Sunk; // корабль потоплен
        public event Action<int, int> Miss; // промах
        public event Action<string, MessageSenderType> MessageReceived; // сообщение получено
        public event Action ConnectionLost; // соединение потеряно

        private TcpClient _client; // TCP клиент
        private StreamReader _reader; // читатель потока
        private StreamWriter _writer; // писатель потока

        private const int Port = 5000; // порт сервера

        public async Task StartClient(string ip)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, Port); // подключение к серверу
                Console.WriteLine("Connected to the server!"); // подключено

                _reader = new StreamReader(_client.GetStream(), Encoding.UTF8); // читатель UTF-8
                _writer = new StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true }; // писатель с автосбросом

                _ = Task.Run(() => ListenForMessages()); // запуск прослушивания сообщений
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}"); // ошибка подключения
                throw;
            }
        }

        private async Task ListenForMessages()
        {
            while (true)
            {
                try
                {
                    string message = await _reader.ReadLineAsync(); // чтение сообщения

                    if (message == null)
                    {
                        Console.WriteLine("Connection lost - received null message"); // потеря соединения
                        ConnectionLost?.Invoke(); // событие потери
                        break;
                    }

                    ProcessMessages(message); // обработка сообщения
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}"); // ошибка чтения
                    Console.WriteLine("Disconnected from the server."); // отключено от сервера
                    ConnectionLost?.Invoke(); // событие потери
                    break;
                }
            }
        }

        public void ProcessMessages(string action)
        {
            if (action == null)
            {
                ConnectionLost?.Invoke(); // потеря соединения
                return;
            }

            if (action.Contains("[Message]")) // сообщение чата
            {
                bool isMine = action.Replace("[Message] ", "").StartsWith("(Player)"); // мое сообщение
                bool isServer = action.Replace("[Message] ", "").StartsWith("(Server)"); // от сервера
                string message = action.Substring(action.IndexOf(')') + 2); // текст сообщения
                MessageSenderType senderType = isMine ? MessageSenderType.Player : isServer ? MessageSenderType.Server : MessageSenderType.Opponent; // тип отправителя
                MessageReceived?.Invoke(message, senderType); // событие получения
                return;
            }

            string[] parts = action.Split(' '); // разделение на части
            string command = parts[0]; // команда

            if (command.Contains("[ShipPlacing]")) // начало расстановки
            {
                ShipPlacing?.Invoke();
            }
            else if (command.Contains("[ShipPlaced]")) // корабль размещен
            {
                int x = int.Parse(parts[1]); // координата X
                int y = int.Parse(parts[2]); // координата Y
                bool isHorizontal = parts[3] == "H"; // горизонтально
                int size = int.Parse(parts[4]); // размер корабля
                ShipPlaced?.Invoke(x, y, isHorizontal, size);
            }
            else if (command.Contains("[InvalidShipPlacement]")) // неверное размещение
            {
                InvalidShipPlacement?.Invoke();
            }
            else if (command.Contains("[Hit]")) // попадание
            {
                int x = int.Parse(parts[1]); // координата X
                int y = int.Parse(parts[2]); // координата Y
                Hit?.Invoke(x, y);
            }
            else if (command.Contains("[Sunk]")) // корабль потоплен
            {
                int x = int.Parse(parts[1]); // координата X
                int y = int.Parse(parts[2]); // координата Y
                bool isHorizontal = parts[3] == "H"; // горизонтально
                int size = int.Parse(parts[4]); // размер корабля
                Sunk?.Invoke(x, y, isHorizontal, size);
            }
            else if (command.Contains("[Miss]")) // промах
            {
                int x = int.Parse(parts[1]); // координата X
                int y = int.Parse(parts[2]); // координата Y
                Miss?.Invoke(x, y);
            }
            else if (command.Contains("[GameStart]")) // игра началась
            {
                GameStart?.Invoke();
            }
            else if (command.Contains("[NewTurn]")) // новый ход
            {
                bool.TryParse(parts[1], out bool myTurn); // мой ли ход
                NewTurn?.Invoke(myTurn);
            }
            else if (command.Contains("[Winned]")) // победа
            {
                Winned?.Invoke();
            }
            else if (command.Contains("[Lost]")) // поражение
            {
                Lost?.Invoke();
            }
            else if (command.Contains("[NewGame]")) // новая игра
            {
                NewGame?.Invoke();
            }
        }

        public void PlaceShip(int x, int y, bool isHorizontal, int size)
        {
            SendMessage($"[PlaceShip] {x} {y} {(isHorizontal ? "H" : "V")} {size}"); // разместить корабль
        }

        public void FireShot(int x, int y)
        {
            SendMessage($"[FireShot] {x} {y}"); // выстрелить
        }

        public void RequestNewGame()
        {
            SendMessage("[NewGame]"); // запросить новую игру
        }

        public void SendChatMessage(string message)
        {
            SendMessage($"[Message] {message}"); // отправить сообщение чата
        }

        private void SendMessage(string message)
        {
            try
            {
                _writer?.WriteLine(message); // отправка сообщения
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message: {ex.Message}"); // ошибка отправки
                ConnectionLost?.Invoke(); // событие потери
            }
        }

        public void Close()
        {
            try
            {
                _reader?.Dispose(); // освобождаем читатель
                _writer?.Dispose(); // освобождаем писатель
                _client?.Close(); // закрываем клиент
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing connection: {ex.Message}"); // ошибка закрытия
            }
        }
    }
}