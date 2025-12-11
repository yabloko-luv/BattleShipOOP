
﻿using WPF_App.Core;

namespace WPF_App.MVVM.Model
{
    public class Message : ObservableObject
    {
        private string _text;
        public string Text // текст сообщения
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        private string _userId;
        public string UserId // ID пользователя
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }

        private bool _isMine;
        public bool IsMine // мое ли сообщение
        {
            get => _isMine;
            set { _isMine = value; OnPropertyChanged(); }
        }

        private bool _isServer;
        public bool IsServer // от сервера ли сообщение
        {
            get => _isServer;
            set { _isServer = value; OnPropertyChanged(); }
        }

        public Message(string message)
        {
            Text = message; // создание с текстом
        }

        public Message(string text, string userId)
        {
            Text = text; // текст
            UserId = userId; // ID пользователя
        }

        public Message(string text, string userId, bool isMine)
        {
            Text = text; // текст
            UserId = userId; // ID пользователя
            IsMine = isMine; // мое ли сообщение
        }
    }
}