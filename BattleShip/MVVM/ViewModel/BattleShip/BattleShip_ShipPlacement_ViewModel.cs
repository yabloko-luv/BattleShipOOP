using BattleShipServer;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Shapes;
using WPF_App.Core;

namespace WPF_App.MVVM.ViewModel
{
    internal class BattleShip_ShipPlacement_ViewModel : ObservableObject
    {
        public event Action<int, int, bool, int> ShipPlaced; // корабль размещен
        public event Action<Ship> ShipRemoved; // корабль удален
        public event Action PlayerReady; // игрок готов

        private bool _isHorizontal;
        public bool IsHorizontal // горизонтально ли размещение
        {
            get => _isHorizontal;
            set { _isHorizontal = value; OnPropertyChanged(); }
        }

        private int _shipSize;
        public int ShipSize // размер корабля
        {
            get => _shipSize;
            set { _shipSize = value; OnPropertyChanged(); }
        }

        private int _TwoShipCount;
        public int TwoShipCount // количество двухпалубных кораблей
        {
            get => _TwoShipCount;
            set { _TwoShipCount = value; OnPropertyChanged(); }
        }

        private int _ThreeShipCount;
        public int ThreeShipCount // количество трехпалубных кораблей
        {
            get => _ThreeShipCount;
            set { _ThreeShipCount = value; OnPropertyChanged(); }
        }

        private int _FourShipCount;
        public int FourShipCount // количество четырехпалубных кораблей
        {
            get => _FourShipCount;
            set { _FourShipCount = value; OnPropertyChanged(); }
        }

        private int _FiveShipCount;
        public int FiveShipCount // количество пятипалубных кораблей
        {
            get => _FiveShipCount;
            set { _FiveShipCount = value; OnPropertyChanged(); }
        }

        public ICommand ChangeShipSizeCommand { get; set; } // команда смены размера корабля
        public ICommand ConfirmShipPlacementCommand { get; set; } // команда подтверждения размещения
        public ICommand RandomizedShipPlacementCommand { get; set; } // команда случайного размещения

        private Client _client; // клиент
        private Player _player; // игрок
        private bool _isRandomizing; // идет ли рандомизация

        public BattleShip_ShipPlacement_ViewModel()
        {
            ShipSize = 2; // начальный размер
            IsHorizontal = true; // горизонтальная ориентация

            ChangeShipSizeCommand = new RelayCommand(
                param =>
                {
                    var str = param.ToString(); // параметр в виде строки
                    ShipSize = int.Parse(str[0].ToString()); // первый символ - размер
                    IsHorizontal = str[1] == 'H'; // второй символ - ориентация
                },
                _ => true);

            ConfirmShipPlacementCommand = new RelayCommand(
                _ => ConfirmShips(), // подтверждение размещения
                _ => _player?.AreAllShipsPlaced() == true); // условие: все корабли размещены
            RandomizedShipPlacementCommand = new RelayCommand(
                _ => RandomizedShipPlacement(), // случайное размещение
                _ => !_isRandomizing); // условие: не идет рандомизация
        }

        public async void RandomizedShipPlacement() // случайное размещение кораблей
        {
            _isRandomizing = true; // начинаем рандомизацию
            ((RelayCommand)RandomizedShipPlacementCommand).RaiseCanExecuteChanged(); // обновляем доступность команды

            try
            {
                // Очищаем все корабли
                foreach (var ship in _player.Ships.ToArray()) // для каждого корабля
                {
                    ShipRemoved?.Invoke(ship); // уведомляем об удалении
                    _player.RemoveShip(ship); // удаляем из игрока
                }

                await System.Threading.Tasks.Task.Delay(100); // небольшая задержка

                // Размещаем корабли случайным образом
                Random random = new Random(); // генератор случайных чисел

                // Получаем список всех кораблей которые нужно разместить
                var shipsToPlace = _player.ShipRules.ToList(); // правила размещения кораблей

                // Сортируем по убыванию размера (сначала большие корабли)
                shipsToPlace.Sort((a, b) => b.CompareTo(a)); // сортировка

                int totalAttempts = 0; // общее количество попыток
                const int MAX_TOTAL_ATTEMPTS = 1000; // максимальное общее количество попыток

                // Основной цикл размещения кораблей
                foreach (var shipSize in shipsToPlace) // для каждого размера корабля
                {
                    bool shipPlaced = false; // размещен ли корабль
                    int shipAttempts = 0; // попытки для этого корабля
                    const int MAX_SHIP_ATTEMPTS = 200; // максимальное количество попыток для корабля

                    while (!shipPlaced && shipAttempts < MAX_SHIP_ATTEMPTS && totalAttempts < MAX_TOTAL_ATTEMPTS)
                    {
                        shipAttempts++;
                        totalAttempts++;

                        // Генерируем случайные параметры
                        bool isHorizontal = random.Next(2) == 0; // случайная ориентация

                        // Рассчитываем максимальные координаты с учетом размера корабля
                        int maxX = isHorizontal ? 10 - shipSize : 9; // максимальная X координата
                        int maxY = isHorizontal ? 9 : 10 - shipSize; // максимальная Y координата

                        // Если корабль не помещается в выбранной ориентации
                        if (maxX < 0 || maxY < 0 || maxX > 9 || maxY > 9)
                        {
                            continue; // Пропускаем эту попытку
                        }

                        int x = random.Next(0, maxX + 1); // случайная X координата
                        int y = random.Next(0, maxY + 1); // случайная Y координата

                        // Проверяем можно ли разместить корабль
                        if (CanPlaceShip(x, y, isHorizontal, shipSize))
                        {
                            Ship ship = new Ship(shipSize); // создаем корабль
                            if (_player.PlaceShip(ship, x, y, isHorizontal)) // размещаем
                            {
                                ShipPlaced?.Invoke(x, y, isHorizontal, shipSize); // уведомляем
                                shipPlaced = true; // отмечаем как размещенный
                            }
                        }
                    }

                    // Если не удалось разместить этот корабль
                    if (!shipPlaced)
                    {
                        // Начинаем все сначала
                        foreach (var ship in _player.Ships.ToArray()) // очищаем все
                        {
                            ShipRemoved?.Invoke(ship); // уведомляем
                            _player.RemoveShip(ship); // удаляем
                        }

                        // Выходим из цикла - будем начинать заново
                        break;
                    }
                }

                // Если после размещения всех кораблей количество не соответствует
                if (!_player.AreAllShipsPlaced()) // проверяем все ли размещены
                {
                    // Пробуем еще раз с самого начала
                    foreach (var ship in _player.Ships.ToArray()) // очищаем
                    {
                        ShipRemoved?.Invoke(ship); // уведомляем
                        _player.RemoveShip(ship); // удаляем
                    }

                    // Используем альтернативный метод размещения
                    TryAlternativePlacement(); // альтернативный метод
                }

                UpdateShipCounts(); // обновляем счетчики
                await System.Threading.Tasks.Task.Delay(100); // задержка
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при случайной расстановке: {ex.Message}"); // сообщение об ошибке

                // В случае ошибки очищаем поле
                foreach (var ship in _player.Ships.ToArray()) // для каждого корабля
                {
                    ShipRemoved?.Invoke(ship); // уведомляем
                    _player.RemoveShip(ship); // удаляем
                }
            }
            finally
            {
                _isRandomizing = false; // завершаем рандомизацию
                ((RelayCommand)RandomizedShipPlacementCommand).RaiseCanExecuteChanged(); // обновляем команду
            }
        }

        // Альтернативный метод размещения (более простой)
        private void TryAlternativePlacement()
        {
            try
            {
                Random random = new Random(); // генератор случайных чисел
                var shipsToPlace = _player.ShipRules.OrderByDescending(s => s).ToList(); // сортируем по убыванию

                // Пробуем несколько раз разместить все корабли
                for (int attempt = 0; attempt < 10; attempt++) // 10 попыток
                {
                    bool success = true; // успех

                    foreach (var shipSize in shipsToPlace) // для каждого размера
                    {
                        bool placed = false; // размещен ли

                        for (int i = 0; i < 50; i++) // 50 попыток для каждого корабля
                        {
                            bool isHorizontal = random.Next(2) == 0; // случайная ориентация
                            int maxX = isHorizontal ? 10 - shipSize : 9; // максимальная X
                            int maxY = isHorizontal ? 9 : 10 - shipSize; // максимальная Y

                            if (maxX < 0 || maxY < 0) continue; // если не помещается

                            int x = random.Next(0, maxX + 1); // случайная X
                            int y = random.Next(0, maxY + 1); // случайная Y

                            if (CanPlaceShip(x, y, isHorizontal, shipSize)) // если можно разместить
                            {
                                Ship ship = new Ship(shipSize); // создаем корабль
                                if (_player.PlaceShip(ship, x, y, isHorizontal)) // размещаем
                                {
                                    ShipPlaced?.Invoke(x, y, isHorizontal, shipSize); // уведомляем
                                    placed = true; // отмечаем как размещенный
                                    break; // выходим из цикла
                                }
                            }
                        }

                        if (!placed) // если не удалось разместить
                        {
                            success = false; // неудача
                            break; // выходим
                        }
                    }

                    if (success) // если успешно
                    {
                        return; // Успешно разместили все корабли
                    }
                    else
                    {
                        // Очищаем и пробуем снова
                        foreach (var ship in _player.Ships.ToArray()) // очищаем
                        {
                            ShipRemoved?.Invoke(ship); // уведомляем
                            _player.RemoveShip(ship); // удаляем
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки
            }
        }

        internal void SetClient(BattleShipServer.Client client) // установка клиента
        {
            _client = client; // сохраняем клиент
        }

        internal void SetPlayer(Player player) // установка игрока
        {
            this._player = player; // сохраняем игрока
        }

        private bool CanPlaceShip(int x, int y, bool isHorizontal, int size) // можно ли разместить корабль
        {
            // Проверяем выход за границы
            if (isHorizontal)
            {
                if (x + size > 10) return false; // выходит за правую границу
            }
            else
            {
                if (y + size > 10) return false; // выходит за нижнюю границу
            }

            // Проверяем каждую клетку корабля и все соседние клетки вокруг
            for (int i = 0; i < size; i++) // для каждой клетки корабля
            {
                int shipX = isHorizontal ? x + i : x; // X координата клетки
                int shipY = isHorizontal ? y : y + i; // Y координата клетки

                // Проверяем саму клетку корабля
                if (_player.Grid.GetCell(shipX, shipY).Status == CellStatus.Ship) // если уже есть корабль
                {
                    return false; // нельзя разместить
                }

                // Проверяем все 8 соседних клеток вокруг этой клетки корабля
                for (int dx = -1; dx <= 1; dx++) // по X
                {
                    for (int dy = -1; dy <= 1; dy++) // по Y
                    {
                        int checkX = shipX + dx; // X соседней клетки
                        int checkY = shipY + dy; // Y соседней клетки

                        // Проверяем только клетки в пределах поля
                        if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                        {
                            // Пропускаем проверку самой клетки корабля
                            if (dx == 0 && dy == 0) continue;

                            if (_player.Grid.GetCell(checkX, checkY).Status == CellStatus.Ship) // если есть корабль
                            {
                                return false; // нельзя разместить
                            }
                        }
                    }
                }
            }

            return true; // можно разместить
        }

        public void MyFieldCellClicked(int x, int y) // клик по клетке поля
        {
            // Если на клетке есть корабль, удаляем его
            if (_player.Grid.GetCell(x, y).Status == CellStatus.Ship) // если клетка с кораблем
            {
                Ship ship = _player.Ships.FirstOrDefault(s => s.Cells.Any(cell => cell.X == x && cell.Y == y)); // находим корабль
                if (ship != null) // если нашли
                {
                    ShipRemoved?.Invoke(ship); // уведомляем об удалении
                    _player.RemoveShip(ship); // удаляем из игрока
                    UpdateShipCounts(); // обновляем счетчики
                }
                return;
            }

            // Проверяем может ли игрок еще размещать корабли такого размера
            var shipRules = _player.ShipRules; // правила размещения
            int numberOfShipAllowed = shipRules.Count(s => s == ShipSize); // сколько можно разместить такого размера
            if (_player.Ships.Count(ship => ship.Size == ShipSize) < numberOfShipAllowed) // если еще можно
            {
                if (_player.AreAllShipsPlaced()) return; // если все корабли уже размещены

                int size = ShipSize; // размер корабля
                Ship ship = new Ship(size); // создаем корабль
                bool isHorizontal = IsHorizontal; // ориентация

                // Проверяем можно ли разместить корабль
                if (CanPlaceShip(x, y, isHorizontal, size) && _player.PlaceShip(ship, x, y, isHorizontal)) // если можно и размещаем
                {
                    ShipPlaced?.Invoke(x, y, isHorizontal, size); // уведомляем о размещении
                }
            }

            UpdateShipCounts(); // обновляем счетчики
        }

        private void UpdateShipCounts() // обновление счетчиков кораблей
        {
            TwoShipCount = _player.Ships.Count(ship => ship.Size == 2); // двухпалубные
            ThreeShipCount = _player.Ships.Count(ship => ship.Size == 3); // трехпалубные
            FourShipCount = _player.Ships.Count(ship => ship.Size == 4); // четырехпалубные
            FiveShipCount = _player.Ships.Count(ship => ship.Size == 5); // пятипалубные

            ((RelayCommand)ConfirmShipPlacementCommand).RaiseCanExecuteChanged(); // обновляем команду подтверждения
        }

        public void ConfirmShips() // подтверждение размещения кораблей
        {
            if (_player.AreAllShipsPlaced()) // если все корабли размещены
            {
                foreach (var ship in _player.Ships) // для каждого корабля
                {
                    var startX = ship.Cells.Min(cell => cell.X); // начальная X координата
                    var endX = ship.Cells.Max(cell => cell.X); // конечная X координата
                    var startY = ship.Cells.Min(cell => cell.Y); // начальная Y координата

                    var isHorizontal = startX != endX; // горизонтальный ли

                    _client.PlaceShip(startX, startY, isHorizontal, ship.Size); // отправляем на сервер
                }

                PlayerReady?.Invoke(); // уведомляем что игрок готов
            }
        }
    }
}