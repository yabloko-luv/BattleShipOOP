
﻿using System;
using System.Windows;

namespace WPF_App.Core
{
    public class AppTheme
    {
        public static ResourceDictionary CurrentTheme { get; set; } // текущая тема

        public static ResourceDictionary ThemeDictionary // словарь тем
        {
            // Можно получить по имени с помощью логики запросов
            get { return App.Current.Resources.MergedDictionaries[0]; } // первый словарь в коллекции
        }

        public static void ChangeTheme(Uri themeUri) // изменение темы
        {
            if (CurrentTheme != null) // если есть текущая тема
            {
                ThemeDictionary.MergedDictionaries.Remove(CurrentTheme); // удаляем ее
            }

            var theme = new ResourceDictionary() { Source = themeUri }; // создаем новую тему
            ThemeDictionary.MergedDictionaries.Add(theme); // добавляем в словарь

            CurrentTheme = theme; // устанавливаем как текущую
        }
    }

    public class Skin : ObservableObject // скин приложения
    {
        private string _name;
        public string Name // название скина
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public string ResourceDictionaryPath { get; set; } // путь к словарю ресурсов

        public Skin(string name, string resourceDictionary)
        {
            this.Name = name; // название
            this.ResourceDictionaryPath = resourceDictionary; // путь к ресурсам
        }
    }
}
