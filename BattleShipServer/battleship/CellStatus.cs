namespace BattleShipServer
{
    public enum CellStatus // статус клетки 
    {
        Empty, // пустая
        Ship, // корабль на ней
        Hit, // подбитый корабль
        Missed // промах
    }
}
