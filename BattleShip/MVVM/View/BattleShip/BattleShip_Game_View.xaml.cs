using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPF_App.MVVM.ViewModel;
using BattleShipServer;

namespace WPF_App.MVVM.View
{
    /// <summary>
    /// Логика взаимодействия для BattleShip_Game_ViewModel.xaml
    /// </summary>
    public partial class BattleShip_Game_View : UserControl
    {
        private BattleShip_Game_ViewModel viewModel => DataContext as BattleShip_Game_ViewModel; // ссылка на ViewModel

        public BattleShip_Game_View()
        {
            InitializeComponent(); // инициализация компонентов

            this.Loaded += BattleShip_Game_View_Loaded; // подписка на событие загрузки
        }

        private void BattleShip_Game_View_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписываемся на события после загрузки
            if (viewModel != null)
            {
                viewModel.OpponentHitted += ViewModel_OpponentHitted; // попадание по противнику
                viewModel.OpponentSunk += ViewModel_OpponentSunk; // потоплен корабль противника
                viewModel.OpponentMissed += ViewModel_OpponentMissed; // промах по противнику
                viewModel.PlayerHitted += ViewModel_PlayerHitted; // попадание по игроку
                viewModel.PlayerSunk += ViewModel_PlayerSunk; // потоплен корабль игрока
                viewModel.PlayerMissed += ViewModel_PlayerMissed; // промах по игроку
            }

            DrawPlayerGrid(); // отрисовка поля игрока
            DrawOpponentGrid(); // отрисовка поля противника

            var player = viewModel?.GetPlayer(); // получаем игрока
            if (player?.Ships != null) // если есть корабли
            {
                foreach (var ship in player.Ships) // для каждого корабля
                {
                    var cells = ship.Cells; // клетки корабля
                    int x = cells[0].X; // координата X первой клетки
                    int y = cells[0].Y; // координата Y первой клетки
                    bool isHorizontal = cells.All(c => c.Y == y); // горизонтальный ли корабль

                    PlaceShip(x, y, isHorizontal, ship.Size); // размещаем корабль
                }
            }
        }

        // Метод для установки клиента извне
        public void SetClient(BattleShipServer.Client client)
        {
            if (viewModel != null)
            {
                viewModel.SetClient(client); // передаем клиент в ViewModel
            }
        }

        // Метод для установки игрока извне
        public void SetPlayer(Player player)
        {
            if (viewModel != null)
            {
                viewModel.SetPlayer(player); // передаем игрока в ViewModel
            }
        }

        private void ViewModel_ShowToast(string message) // отображение уведомления
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                // Используем MessageBox вместо ToastManager
                MessageBox.Show(message, "Battleship", MessageBoxButton.OK, MessageBoxImage.Information); // отображение сообщения
            });
        }

        private void PlaceShip(int x, int y, bool isHorizontal, int size) // размещение корабля
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                try
                {
                    Console.WriteLine($"Drawn ship at {x}, {y} with size {size} and orientation {isHorizontal}"); // отладочная информация

                    //Получаем границу клетки
                    if (isHorizontal) // если горизонтальный
                    {
                        for (int i = 0; i < size; i++) // для каждой клетки корабля
                        {
                            var cell = PlayerFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == x + i && Grid.GetRow(o) == y); // находим клетку
                            cell.HasShip = true; // отмечаем что есть корабль
                            cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78909C")); // Серый для корабля
                        }
                    }
                    else // если вертикальный
                    {
                        for (int i = 0; i < size; i++) // для каждой клетки корабля
                        {
                            var cell = PlayerFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == x && Grid.GetRow(o) == y + i); // находим клетку
                            cell.HasShip = true; // отмечаем что есть корабль
                            cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78909C")); // Серый для корабль
                        }
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private void DrawOpponentShip(int x, int y, bool isHorizontal, int size) // отрисовка корабля противника
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                // Для противника - просто помечаем клетки как попадания
                if (isHorizontal) // если горизонтальный
                {
                    for (int i = 0; i < size; i++) // для каждой клетки корабля
                    {
                        var cell = OpponentFieldGrid.Children.OfType<BorderCell>()
                            .First(o => Grid.GetColumn(o) == x + i && Grid.GetRow(o) == y); // находим клетку
                        cell.IsHit = true; // отмечаем попадание
                        // Меняем цвет на темно-красный для уничтоженного корабля
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000"));
                    }
                }
                else // если вертикальный
                {
                    for (int i = 0; i < size; i++) // для каждой клетки корабля
                    {
                        var cell = OpponentFieldGrid.Children.OfType<BorderCell>()
                            .First(o => Grid.GetColumn(o) == x && Grid.GetRow(o) == y + i); // находим клетку
                        cell.IsHit = true; // отмечаем попадание
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000"));
                    }
                }
            });
        }

        private void ViewModel_OpponentHitted(int arg1, int arg2) // попадание по противнику
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                var opponentCell = OpponentFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == arg1 && Grid.GetRow(o) == arg2); // находим клетку
                opponentCell.IsHit = true; // отмечаем попадание
                opponentCell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350")); // Красный для попадания
            });
        }

        private void ViewModel_OpponentSunk(int arg1, int arg2, bool isHorizontal, int size) // потоплен корабль противника
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                // Используем новый метод DrawOpponentShip
                DrawOpponentShip(arg1, arg2, isHorizontal, size); // отрисовываем потопленный корабль
            });
        }

        private void ViewModel_OpponentMissed(int arg1, int arg2) // промах по противнику
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                var myCell = OpponentFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == arg1 && Grid.GetRow(o) == arg2); // находим клетку
                myCell.IsMissed = true; // отмечаем промах
                myCell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0BEC5")); // Светло-серый для промаха
            });
        }

        private void ViewModel_PlayerHitted(int arg1, int arg2) // попадание по игроку
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                var myCell = PlayerFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == arg1 && Grid.GetRow(o) == arg2); // находим клетку
                myCell.IsHit = true; // отмечаем попадание
                myCell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350")); // Красный для попадания
            });
        }

        private void ViewModel_PlayerSunk(int arg1, int arg2, bool isHorizontal, int size) // потоплен корабль игрока
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                // Помечаем все клетки корабля как уничтоженные
                if (isHorizontal) // если горизонтальный
                {
                    for (int i = 0; i < size; i++) // для каждой клетки корабля
                    {
                        var cell = PlayerFieldGrid.Children.OfType<BorderCell>()
                            .First(o => Grid.GetColumn(o) == arg1 + i && Grid.GetRow(o) == arg2); // находим клетку
                        cell.IsHit = true; // отмечаем попадание
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000")); // Темно-красный для уничтоженного
                    }
                }
                else // если вертикальный
                {
                    for (int i = 0; i < size; i++) // для каждой клетки корабля
                    {
                        var cell = PlayerFieldGrid.Children.OfType<BorderCell>()
                            .First(o => Grid.GetColumn(o) == arg1 && Grid.GetRow(o) == arg2 + i); // находим клетку
                        cell.IsHit = true; // отмечаем попадание
                        cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000")); // Темно-красный для уничтоженного
                    }
                }
            });
        }

        private void ViewModel_PlayerMissed(int arg1, int arg2) // промах по игроку
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                var opponentCell = PlayerFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == arg1 && Grid.GetRow(o) == arg2); // находим клетку
                opponentCell.IsMissed = true; // отмечаем промах
                opponentCell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0BEC5")); // Светло-серый для промаха
            });
        }

        private void DrawPlayerGrid() // отрисовка поля игрока
        {
            var grid = PlayerFieldGrid;

            // Создаем сетку 10x10
            for (int i = 0; i < 10; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition()); // добавляем строки
                grid.ColumnDefinitions.Add(new ColumnDefinition()); // добавляем столбцы
            }

            for (int i = 0; i < 10; i++) // по строкам
            {
                for (int j = 0; j < 10; j++) // по столбцам
                {
                    var cell = new BorderCell(); // создаем клетку
                    cell.SetPosition(i, j); // устанавливаем позицию
                    grid.Children.Add(cell); // добавляем в сетку
                }
            }

            // Добавляем номера строк и столбцов к сетке
            // первая строка и первый столбец с отрицательным отступом для отображения вне сетки
            for (int i = 0; i < 10; i++)
            {
                var rowNumber = new TextBlock // номер строки
                {
                    Text = (i + 1).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(-75, 0, 0, 0), // отступ слева
                };
                Grid.SetRow(rowNumber, i); // устанавливаем строку
                Grid.SetColumn(rowNumber, 0); // устанавливаем столбец
                grid.Children.Add(rowNumber); // добавляем в сетку

                //Столбцы - буквы
                var columnLetter = new TextBlock // буква столбца
                {
                    Text = ((char)(65 + i)).ToString(), // A, B, C...
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, -75, 0, 0) // отступ сверху
                };
                Grid.SetRow(columnLetter, 0); // устанавливаем строку
                Grid.SetColumn(columnLetter, i); // устанавливаем столбец
                grid.Children.Add(columnLetter); // добавляем в сетку
            }
        }

        private void DrawOpponentGrid() // отрисовка поля противника
        {
            var grid = OpponentFieldGrid;
            grid.MouseLeave += (_, e2) => // при выходе мыши из поля
            {
                // Удаляем все временные границы
                var borders = grid.Children.OfType<BorderCell>().Where(o => o.Name.StartsWith("cursor")).ToArray(); // находим курсоры
                foreach (var border in borders) // для каждого курсора
                {
                    grid.Children.Remove(border); // удаляем
                }
            };

            // Создаем сетку 10x10
            for (int i = 0; i < 10; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition()); // добавляем строки
                grid.ColumnDefinitions.Add(new ColumnDefinition()); // добавляем столбцы
            }

            // Добавляем номера строк и столбцов к сетке
            // первая строка и первый столбец с отрицательным отступом для отображения вне сетки
            for (int i = 0; i < 10; i++)
            {
                var rowNumber = new TextBlock // номер строки
                {
                    Text = (i + 1).ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(-75, 0, 0, 0), // отступ слева
                };
                Grid.SetRow(rowNumber, i); // устанавливаем строку
                Grid.SetColumn(rowNumber, 0); // устанавливаем столбец
                grid.Children.Add(rowNumber); // добавляем в сетку

                //Столбцы - буквы
                var columnLetter = new TextBlock // буква столбца
                {
                    Text = ((char)(65 + i)).ToString(), // A, B, C...
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, -75, 0, 0) // отступ сверху
                };
                Grid.SetRow(columnLetter, 0); // устанавливаем строку
                Grid.SetColumn(columnLetter, i); // устанавливаем столбец
                grid.Children.Add(columnLetter); // добавляем в сетку
            }

            for (int i = 0; i < 10; i++) // по строкам
            {
                for (int j = 0; j < 10; j++) // по столбцам
                {
                    var cell = new BorderCell(); // создаем клетку
                    cell.MouseEnter += Cell_MouseEnter; // событие входа мыши
                    cell.MouseLeave += Cell_MouseLeave; // событие выхода мыши
                    cell.CellClicked += OtherFieldGrid_CellClicked; // событие клика
                    cell.SetPosition(i, j); // устанавливаем позицию
                    grid.Children.Add(cell); // добавляем в сетку
                }
            }
        }

        private void Cell_MouseEnter(object sender, MouseEventArgs e) // при наведении мыши на клетку
        {
            // Добавляем временную границу для показа позиции курсора
            var cell = sender as BorderCell; // клетка

            int x = Grid.GetColumn(cell); // координата X
            int y = Grid.GetRow(cell); // координата Y

            var border = new BorderCell // создаем временную границу
            {
                Name = $"cursor{x}{y}", // уникальное имя
                Background = Brushes.Blue, // синий цвет
                IsHitTestVisible = false // не реагирует на мышь
            };

            Grid.SetRow(border, y); // устанавливаем строку
            Grid.SetColumn(border, x); // устанавливаем столбец

            // Анимация прозрачности
            var opacityAnimation = new DoubleAnimation(0, 0.3, TimeSpan.FromSeconds(0.2)); // от 0 до 0.3 за 0.2 секунды
            border.BeginAnimation(Border.OpacityProperty, opacityAnimation); // запускаем анимацию

            OpponentFieldGrid.Children.Add(border); // добавляем в сетку
        }

        private void Cell_MouseLeave(object sender, MouseEventArgs e) // при уходе мыши с клетки
        {
            //Получаем временную границу позиции сетки
            var cell = sender as BorderCell; // клетка

            int x = Grid.GetColumn(cell); // координата X
            int y = Grid.GetRow(cell); // координата Y

            var cursor = OpponentFieldGrid.Children.OfType<BorderCell>().FirstOrDefault(o => o.Name == $"cursor{x}{y}"); // находим курсор

            // Анимация прозрачности
            var opacityAnimation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2)); // до 0 за 0.2 секунды
            opacityAnimation.Completed += (s, _) => // по завершении анимации
            {
                OpponentFieldGrid.Children.Remove(cursor); // удаляем курсор
            };

            cursor.BeginAnimation(Border.OpacityProperty, opacityAnimation); // запускаем анимацию
        }

        private void OtherFieldGrid_CellClicked(int arg1, int arg2) // клик по клетке противника
        {
            viewModel?.OpponentFieldCellClicked(arg1, arg2); // передаем в ViewModel
        }
    }
}