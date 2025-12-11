
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShipServer
{
    public enum MessageSenderType
    {
        Server, // от сервера
        Player, // от игрока
        Opponent // от противника
    }
}