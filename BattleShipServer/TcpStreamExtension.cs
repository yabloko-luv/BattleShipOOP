using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipServer
{
    internal static class TcpStreamExtension
    {
        public static async Task<int> ReadAsyncWithTimeout(this NetworkStream stream, byte[] buffer, int offset, int count)
        {
            if (stream.CanRead) // проверяем можно ли читать
            {
                Task<int> readTask = stream.ReadAsync(buffer, offset, count); // задача чтения
                Task delayTask = Task.Delay(stream.ReadTimeout); // задача задержки
                Task task = await Task.WhenAny(readTask, delayTask); // ждем первую завершенную

                if (task == readTask)
                    return await readTask; // возвращаем прочитанные байты

            }
            return 0; // возвращаем 0 при таймауте
        }
    }
}