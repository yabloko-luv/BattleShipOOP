using BattleShipServer;
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

namespace WPF_App.MVVM.View
{
    /// <summary>
    /// Логика взаимодействия для BattleShip_ShipPlacement_View.xaml
    /// </summary>
    public partial class BattleShip_ShipPlacement_View : UserControl
    {
        private BattleShip_ShipPlacement_ViewModel viewModel => DataContext as BattleShip_ShipPlacement_ViewModel; // ссылка на ViewModel

        public BattleShip_ShipPlacement_View()
        {
            InitializeComponent(); // инициализация компонентов

            viewModel.ShipPlaced += ViewModel_ShipPlaced; // подписка на размещение корабля
            viewModel.ShipRemoved += ViewModel_ShipRemoved; // подписка на удаление корабля
        }

        private void ViewModel_ShipPlaced(int x, int y, bool isHorizontal, int size) // размещение корабля
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
                            var cell = MyFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == x + i && Grid.GetRow(o) == y); // находим клетку
                            cell.HasShip = true; // отмечаем что есть корабль
                            // Устанавливаем серый цвет для клеток с кораблем
                            cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78909C"));
                        }
                    }
                    else // если вертикальный
                    {
                        for (int i = 0; i < size; i++) // для каждой клетки корабля
                        {
                            var cell = MyFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == x && Grid.GetRow(o) == y + i); // находим клетку
                            cell.HasShip = true; // отмечаем что есть корабль
                            cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78909C"));
                        }
                    }
                }
                catch (Exception)
                {
                }
            });
        }
        private void ViewModel_ShipRemoved(Ship ship) // удаление корабля
        {
            App.Current.Dispatcher.Invoke(() => // в основном потоке
            {
                UnDrawShip(ship); // убираем визуально корабль

                // Получаем клетки корабля и отмечаем их как свободные
                foreach (var cell in ship.Cells) // для каждой клетки корабля
                {
                    var cellBorder = MyFieldGrid.Children.OfType<BorderCell>().First(o => Grid.GetColumn(o) == cell.X && Grid.GetRow(o) == cell.Y); // находим клетку
                    cellBorder.HasShip = false; // отмечаем что нет корабля
                    // Возвращаем стандартный цвет для пустой клетки
                    cellBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#455A64"));
                }
            });
        }

        private void UnDrawShip(Ship ship) // удаление визуального корабля
        {
            var borderShips = MyFieldGrid.Children.OfType<Border>(); // все границы кораблей

            var shipCells = ship.Cells; // клетки корабля
            if (shipCells.Count == 0) return; // если нет клеток

            int x = shipCells[0].X; // координата X первой клетки
            int y = shipCells[0].Y; // координата Y первой клетки

            // Находим границу где строка или столбец одна из клеток
            var border = borderShips.FirstOrDefault(o => o.Name == $"shipBorder{x}{y}"); // находим границу корабля

            if (border != null) // если нашли
            {
                // Анимация масштаба корабля от 1 до 0
                var scaleAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2)) // уменьшение за 0.2 секунды
                {
                    EasingFunction = new BackEase() { EasingMode = EasingMode.EaseIn } // эффект пружины
                };

                scaleAnimation.Completed += (s, e) => // по завершении анимации
                {
                    if (MyFieldGrid.Children.Contains(border)) // если граница еще есть
                    {
                        MyFieldGrid.Children.Remove(border); // удаляем границу
                    }
                };

                border.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation); // анимация по X
                border.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation); // анимация по Y
            }
        }

        private void MyFieldGrid_Loaded(object sender, RoutedEventArgs e) // загрузка сетки
        {
            var grid = sender as Grid; // сетка
            grid.MouseLeave += (_, e2) => // при выходе мыши из сетки
            {
                // Удаляем все временные границы
                var borders = grid.Children.OfType<BorderCell>().Where(o => o.Name.StartsWith("cursor")).ToArray(); // находим все курсоры
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

            for (int i = 0; i < 10; i++) // по строкам
            {
                for (int j = 0; j < 10; j++) // по столбцам
                {
                    var cell = new BorderCell(); // создаем клетку
                    // Устанавливаем стандартный цвет для пустых клеток
                    cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#455A64"));
                    cell.MouseEnter += Cell_MouseEnter; // событие входа мыши
                    cell.MouseLeave += Cell_MouseLeave; // событие выхода мыши
                    cell.CellClicked += MyFieldGrid_CellClicked; // событие клика
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

        private void Cell_MouseEnter(object sender, MouseEventArgs e) // при наведении мыши на клетку
        {
            // Добавляем временную границу для показа позиции курсора
            var cell = sender as BorderCell; // клетка

            int x = Grid.GetColumn(cell); // координата X
            int y = Grid.GetRow(cell); // координата Y

            var border = new BorderCell // создаем временную границу
            {
                Name = $"cursor{x}{y}", // уникальное имя
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C6BC0")), // Светло-синий для предпросмотра
                Margin = new Thickness(1), // отступ
                CornerRadius = new CornerRadius(3), // Простые скругленные углы
                IsHitTestVisible = false, // не реагирует на мышь
                Opacity = 0.7 // прозрачность
            };

            Grid.SetRow(border, y); // устанавливаем строку
            Grid.SetColumn(border, x); // устанавливаем столбец

            // Расширяем курсор до размера корабля
            if (viewModel.IsHorizontal) // если горизонтальный
            {
                Grid.SetColumnSpan(border, viewModel.ShipSize); // объединяем столбцы
            }
            else // если вертикальный
            {
                Grid.SetRowSpan(border, viewModel.ShipSize); // объединяем строки
            }

            // если корабль слишком большой, устанавливаем красный фон
            bool outOfBounds = false; // выходит за границы
            if (viewModel.IsHorizontal) // если горизонтальный
            {
                if (x + viewModel.ShipSize > 10) outOfBounds = true; // проверяем границы
            }
            else // если вертикальный
            {
                if (y + viewModel.ShipSize > 10) outOfBounds = true; // проверяем границы
            }

            if (outOfBounds) // если выходит за границы
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350")); // Красный для ошибки
                border.CornerRadius = new CornerRadius(3); // Простые углы
            }
            else // если в границах
            {
                // Проверяем можно ли разместить корабль на этой позиции
                bool canPlace = true; // можно разместить

                // Проверяем каждую клетку корабля и все соседние клетки
                for (int i = 0; i < viewModel.ShipSize; i++) // для каждой клетки корабля
                {
                    int shipX = viewModel.IsHorizontal ? x + i : x; // координата X клетки корабля
                    int shipY = viewModel.IsHorizontal ? y : y + i; // координата Y клетки корабля

                    // Проверяем саму клетку корабля
                    if (IsCellOccupied(shipX, shipY)) // если занята
                    {
                        canPlace = false; // нельзя разместить
                        break;
                    }

                    // Проверяем все соседние клетки (8 направлений)
                    for (int dx = -1; dx <= 1; dx++) // по X
                    {
                        for (int dy = -1; dy <= 1; dy++) // по Y
                        {
                            int checkX = shipX + dx; // координата X соседней клетки
                            int checkY = shipY + dy; // координата Y соседней клетки

                            // Проверяем только клетки в пределах поля
                            if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                            {
                                // Пропускаем саму клетку корабля
                                if (dx == 0 && dy == 0) continue;

                                // Проверяем есть ли корабль в соседней клетке
                                if (IsCellOccupied(checkX, checkY)) // если занята
                                {
                                    canPlace = false; // нельзя разместить
                                    break;
                                }
                            }
                        }
                        if (!canPlace) break;
                    }
                    if (!canPlace) break;
                }

                if (!canPlace) // если нельзя разместить
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350")); // Красный для конфликта
                }
            }

            MyFieldGrid.Children.Add(border); // добавляем в сетку
        }

        private bool IsCellOccupied(int x, int y) // проверка занята ли клетка
        {
            // Находим клетку по координатам и проверяем есть ли на ней корабль
            var cell = MyFieldGrid.Children.OfType<BorderCell>()
                .FirstOrDefault(o => Grid.GetColumn(o) == x && Grid.GetRow(o) == y); // находим клетку

            return cell != null && cell.HasShip; // возвращаем результат
        }

        private void Cell_MouseLeave(object sender, MouseEventArgs e) // при уходе мыши с клетки
        {
            //Получаем временную границу позиции сетки
            var cell = sender as BorderCell; // клетка

            int x = Grid.GetColumn(cell); // координата X
            int y = Grid.GetRow(cell); // координата Y

            var cursor = MyFieldGrid.Children.OfType<BorderCell>().FirstOrDefault(o => o.Name == $"cursor{x}{y}"); // находим курсор

            if (cursor != null) // если нашли
            {
                MyFieldGrid.Children.Remove(cursor); // удаляем
            }
        }

        private void MyFieldGrid_CellClicked(int arg1, int arg2) // клик по клетке
        {
            viewModel.MyFieldCellClicked(arg1, arg2); // передаем в ViewModel
        }

        private void FadeRadioButton_Checked(object sender, RoutedEventArgs e) // выбор радиокнопки
        {
            // Обработчик выбора ориентации корабля
        }
    }
}