using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipServer
{
    public class Server
    {
        private Dictionary<Player, bool> _playersReadyForNewGame = new Dictionary<Player, bool>(); // готовность к новой игре
        private const int Port = 5000; // порт сервера
        private List<PlayerConnection> playerConnections = new List<PlayerConnection>(); // подключенные игроки
        private BattleShipGame game; // текущая игра

        public async Task StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, Port); // слушатель на всех интерфейсах
            listener.Start(); // запускаем слушатель

            while (true)
            {
                // Ждем подключения двух игроков
                if (playerConnections.Count < 2)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(); // принимаем клиента
                    var playerConnection = new PlayerConnection(client); // создаем соединение
                    playerConnections.Add(playerConnection); // добавляем в список
                    _ = HandleClientAsync(playerConnection); // запускаем обработчик

                    playerConnection.SendMessage("[Message] (Server) Игрок подключен. Ожидание второго игрока..."); // сообщение о подключении
                }

                // Начинаем игру если два игрока подключены
                if (playerConnections.Count == 2 && game == null)
                {
                    // Создаем новую игру
                    game = new BattleShipGame(playerConnections[0].Player, playerConnections[1].Player);
                    game.gameState = GameState.PlacingShips; // состояние расстановки

                    BroadcastMessage("[ShipPlacing]"); // команда расстановки
                    BroadcastMessage("[Message] (Server) Оба игрока подключены. Расставьте корабли."); // инструкция

                    // Сбрасываем состояние игроков
                    playerConnections[0].Player.State = PlayerState.PlacingShips;
                    playerConnections[1].Player.State = PlayerState.PlacingShips;
                }

                await Task.Delay(100); // небольшая пауза
            }
        }

        private async Task HandleClientAsync(PlayerConnection playerConnection)
        {
            while (true)
            {
                try
                {
                    // Проверяем подключение
                    if (playerConnection.TcpClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (playerConnection.TcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            Console.WriteLine($"Игрок {playerConnection.Player.Name} отключился."); // сообщение об отключении
                            HandlePlayerDisconnection(playerConnection); // обработка отключения
                            break;
                        }
                    }

                    string message = await playerConnection.ReadLineAsync(); // читаем сообщение
                    await GameLoop(playerConnection, message); // передаем в игровой цикл
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}"); // ошибка
                    HandlePlayerDisconnection(playerConnection); // обработка отключения
                    break;
                }
            }
        }

        private void HandlePlayerDisconnection(PlayerConnection disconnectedPlayer)
        {
            playerConnections.Remove(disconnectedPlayer); // удаляем из списка
            _playersReadyForNewGame.Remove(disconnectedPlayer.Player); // удаляем из готовых
            disconnectedPlayer.Close(); // закрываем соединение

            if (game != null && game.gameState != GameState.Ending)
            {
                var remainingPlayer = playerConnections.FirstOrDefault(); // оставшийся игрок
                if (remainingPlayer != null)
                {
                    remainingPlayer.SendMessage("[Winned]"); // отправляем победу
                    remainingPlayer.SendMessage("[Message] (Server) Вы победили! Противник отключился."); // сообщение
                    remainingPlayer.Player.State = PlayerState.Idle; // состояние простоя
                }
                game.gameState = GameState.Ending; // конец игры
                game = null; // очищаем игру
            }

            Console.WriteLine($"Осталось игроков: {playerConnections.Count}"); // вывод количества
        }

        private async Task GameLoop(PlayerConnection playerConnection, string action)
        {
            if (string.IsNullOrEmpty(action)) return; // пустое действие

            // Обработка сообщений чата
            if (action.Contains("[Message]"))
            {
                string message = action.Substring(9); // получаем текст
                playerConnections.ForEach(p => SendMessage(p, $"[Message] ({(p == playerConnection ? "Player" : "Opponent")}) {message}")); // рассылка
                return;
            }

            switch (game.gameState)
            {
                case GameState.PlacingShips:
                    await HandleShipPlacement(playerConnection, action); // расстановка кораблей
                    break;

                case GameState.Playing:
                    await HandleGameplay(playerConnection, action); // игровой процесс
                    break;

                case GameState.Ending:
                    if (action.Contains("[NewGame]")) // запрос новой игры
                    {
                        _playersReadyForNewGame[playerConnection.Player] = true; // игрок готов
                        playerConnection.Player.Reset(); // сброс состояния
                        BroadcastMessage($"игрок предлагает реванш"); // сообщение о реванше
                        playerConnection.SendMessage("[NewGame]"); // команда новой игры
                        if (_playersReadyForNewGame.Count == 2 &&
            playerConnections.All(p => _playersReadyForNewGame.ContainsKey(p.Player) && _playersReadyForNewGame[p.Player]))
                        {
                            StartNewGame(); // начинаем новую игру
                        }
                        else
                        {
                            playerConnection.SendMessage("[Message] (Server) Ожидаем второго игрока..."); // ожидание
                        }
                    }
                    break;
            }
        }
        private void StartNewGame()
        {
            // Сбрасываем флаги готовности
            _playersReadyForNewGame.Clear();

            // Сбрасываем состояние игроков
            foreach (var pc in playerConnections)
            {
                pc.Player.Reset(); // сброс игрока
                pc.Player.State = PlayerState.PlacingShips; // состояние расстановки
            }

            // Создаем новую игру
            game = new BattleShipGame(playerConnections[0].Player, playerConnections[1].Player);
            game.gameState = GameState.PlacingShips; // состояние расстановки

            // Отправляем сообщения игрокам
            BroadcastMessage("[NewGame]"); // новая игра
            BroadcastMessage("[ShipPlacing]"); // расстановка кораблей
            BroadcastMessage("[Message] (Server) Новая игра начинается! Расставьте корабли."); // инструкция

            Console.WriteLine("Новая игра начата!"); // вывод в консоль
        }
        private async Task HandleShipPlacement(PlayerConnection playerConnection, string action)
        {
            var player = playerConnection.Player;

            if (player.State == PlayerState.ShipsPlaced) return; // корабли уже расставлены

            if (action.Contains("[PlaceShip]")) // размещение корабля
            {
                string[] parts = action.Split(' ');
                int x = int.Parse(parts[1]); // координата X
                int y = int.Parse(parts[2]); // координата Y
                bool isHorizontal = parts[3] == "H"; // горизонтально
                int size = int.Parse(parts[4]); // размер корабля

                if (game.PlaceShip(player, x, y, isHorizontal, size)) // пытаемся разместить
                {
                    SendMessage(playerConnection, $"[ShipPlaced] {x} {y} {(isHorizontal ? "H" : "V")} {size}"); // успешно
                }
                else
                {
                    SendMessage(playerConnection, "[InvalidShipPlacement]"); // ошибка размещения
                }
            }

            // Проверяем, все ли корабли расставлены
            if (player.AreAllShipsPlaced())
            {
                player.State = PlayerState.ShipsPlaced; // состояние расставлено
                SendMessage(playerConnection, "[Message] (Server) Все корабли расставлены! Ожидание второго игрока..."); // сообщение

                // Проверяем, готовы ли оба игрока
                if (playerConnections.All(p => p.Player.State == PlayerState.ShipsPlaced))
                {
                    StartGame(); // начинаем игру
                }
            }
        }

        private void StartGame()
        {
            game.gameState = GameState.Playing; // состояние игры

            // Случайно выбираем, кто ходит первым
            var random = new Random();
            game.CurrentPlayer = random.Next(2) == 0 ?
                playerConnections[0].Player :
                playerConnections[1].Player;

            BroadcastMessage("[GameStart]"); // игра началась
            BroadcastMessage($"[Message] (Server) Игра началась! Первым ходит {game.CurrentPlayer.Name}."); // сообщение

            // Устанавливаем состояния игроков
            foreach (var pc in playerConnections)
            {
                bool isMyTurn = pc.Player == game.CurrentPlayer; // мой ли ход
                pc.Player.State = isMyTurn ? PlayerState.TakingTurn : PlayerState.WaitingForTurn; // состояние
                pc.SendMessage($"[NewTurn] {isMyTurn}"); // сообщение о ходе
            }
        }

        private async Task HandleGameplay(PlayerConnection playerConnection, string action)
        {
            if (playerConnection.Player.State != PlayerState.TakingTurn) return; // не ваш ход

            if (action.Contains("[FireShot]")) // выстрел
            {
                string[] parts = action.Split(' ');
                int x = int.Parse(parts[1]); // координата X
                int y = int.Parse(parts[2]); // координата Y

                if (game.FireShot(x, y)) // попадание
                {
                    BroadcastMessage($"[Hit] {x} {y}"); // сообщение о попадании

                    var cell = game.Opponent.Grid.GetCell(x, y); // получаем клетку
                    var opponentShip = game.Opponent.Ships.Find(s => s.Cells.Contains(cell)); // находим корабль

                    if (opponentShip.IsSunk()) // корабль потоплен
                    {
                        var shipFirstCell = opponentShip.Cells[0]; // первая клетка
                        var isHorizontal = opponentShip.Cells[1].Y == shipFirstCell.Y; // горизонтальный ли
                        BroadcastMessage($"[Sunk] {shipFirstCell.X} {shipFirstCell.Y} {(isHorizontal ? "H" : "V")} {opponentShip.Size}"); // сообщение о потоплении

                        if (game.Opponent.IsAllShipsSunk()) // все корабли потоплены
                        {
                            var winnerConnection = playerConnections.Find(p => p.Player == game.CurrentPlayer); // победитель
                            var loserConnection = playerConnections.Find(p => p.Player != game.CurrentPlayer); // проигравший

                            winnerConnection.SendMessage("[Winned]"); // победа
                            loserConnection.SendMessage("[Lost]"); // поражение

                            BroadcastMessage($"[Message] (Server) {game.CurrentPlayer.Name} победил!"); // сообщение о победе

                            game.gameState = GameState.Ending; // конец игры
                            return;
                        }
                    }
                }
                else // промах
                {
                    BroadcastMessage($"[Miss] {x} {y}"); // сообщение о промахе
                    game.SwitchTurn(); // смена хода

                    // Обновляем состояния игроков
                    foreach (var pc in playerConnections)
                    {
                        bool isMyTurn = pc.Player == game.CurrentPlayer; // мой ли ход
                        pc.Player.State = isMyTurn ? PlayerState.TakingTurn : PlayerState.WaitingForTurn; // состояние
                        pc.SendMessage($"[NewTurn] {isMyTurn}"); // сообщение о ходе
                    }
                }
            }
        }

        private void BroadcastMessage(string message)
        {
            foreach (var playerConnection in playerConnections)
            {
                SendMessage(playerConnection, message); // отправка всем
            }
        }

        private void SendMessage(PlayerConnection playerConnection, string message)
        {
            try
            {
                if (playerConnection.TcpClient.Connected) // проверка подключения
                {
                    var writer = new StreamWriter(playerConnection.TcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true }; // писатель
                    writer.WriteLine(message); // отправка сообщения
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось отправить сообщение игроку {playerConnection.Player.Name}: {ex.Message}"); // ошибка отправки
            }
        }
    }
}