namespace BattleShipServer
{
    public class Cell // сама клетка
    {
        public int X { get; set; } // координаты
        public int Y { get; set; }
        public CellStatus Status { get; set; }

        public Cell(int x, int y) // конструктор
        {
            X = x;
            Y = y;
            Status = CellStatus.Empty;
        }
    }
}
