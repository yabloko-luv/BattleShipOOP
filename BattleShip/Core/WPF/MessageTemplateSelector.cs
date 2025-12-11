
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace WPF_App.Core.WPF
{
    public class MessageTemplateSelector : DataTemplateSelector // селектор шаблонов сообщений
    {
        public DataTemplate PlayerMessageTemplate { get; set; } // шаблон для игрока
        public DataTemplate OpponentMessageTemplate { get; set; } // шаблон для противника
        public DataTemplate ServerMessageTemplate { get; set; } // шаблон для сервера

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is MVVM.Model.Message message) // проверяем что это сообщение
            {
                // Если isMine => MyMessageTemplate
                // Если isServer => ServerMessageTemplate
                // Иначе => OtherMessageTemplate

                if (message.IsMine) // мое сообщение
                {
                    return PlayerMessageTemplate;
                }
                else if (message.IsServer) // от сервера
                {
                    return ServerMessageTemplate;
                }
                else // от противника
                {
                    return OpponentMessageTemplate;
                }
            }
            return base.SelectTemplate(item, container); // базовый шаблон
        }
    }
}
