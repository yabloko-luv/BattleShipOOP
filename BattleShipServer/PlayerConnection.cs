
﻿using BattleShipServer;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class PlayerConnection
{
    public TcpClient TcpClient { get; private set; } // TCP клиент игрока
    public Player Player { get; private set; } // данные игрока
    public bool IsReleased { get; set; } // освобождено ли соединение

    private NetworkStream Stream { get; set; } // сетевой поток
    private StreamReader Reader { get; set; } // читатель потока
    private StreamWriter Writer { get; set; } // писатель потока

    public PlayerConnection(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
        Player = new Player(Guid.NewGuid().ToString()); // создаем игрока с уникальным ID
        Stream = tcpClient.GetStream(); // получаем поток
        Reader = new StreamReader(Stream, Encoding.UTF8); // читатель UTF-8
        Writer = new StreamWriter(Stream, Encoding.UTF8) { AutoFlush = true }; // писатель с автосбросом
    }

    public async Task<string> ReadLineAsync()
    {
        return await Reader.ReadLineAsync(); // чтение строки асинхронно
    }
    public async Task<string> ReadLineWithTimeoutAsync(int timeoutMilliseconds)
    {
        var readLineTask = Reader.ReadLineAsync(); // задача чтения
        var timeoutTask = Task.Delay(timeoutMilliseconds); // задача таймаута

        var completedTask = await Task.WhenAny(readLineTask, timeoutTask); // ждем первую завершенную

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("ReadLineAsync timed out."); // таймаут
        }

        return await readLineTask; // возвращаем результат чтения
    }

    public void SendMessage(string message)
    {
        Writer.WriteLine(message); // отправка сообщения
    }

    public void Close()
    {
        IsReleased = true; // отмечаем как освобожденное
        Reader.Dispose(); // освобождаем читатель
        Writer.Dispose(); // освобождаем писатель
        TcpClient.Close(); // закрываем клиент
    }
}
