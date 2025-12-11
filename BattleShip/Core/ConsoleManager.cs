
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WPF_App.Core
{
    [SuppressUnmanagedCodeSecurity]
    public static class ConsoleManager
    {
        private const string Kernel32_DllName = "kernel32.dll"; // имя DLL ядра

        [DllImport(Kernel32_DllName)]
        private static extern bool AllocConsole(); // выделение консоли

        [DllImport(Kernel32_DllName)]
        private static extern bool FreeConsole(); // освобождение консоли

        [DllImport(Kernel32_DllName)]
        private static extern IntPtr GetConsoleWindow(); // получение окна консоли

        [DllImport(Kernel32_DllName)]
        private static extern int GetConsoleOutputCP(); // получение кодовой страницы вывода

        public static bool HasConsole // есть ли консоль
        {
            get { return GetConsoleWindow() != IntPtr.Zero; }
        }

        /// <summary>
        /// Создает новый экземпляр консоли если процесс не привязан к консоли
        /// </summary>
        public static void Show()
        {
            //#if DEBUG
            if (!HasConsole) // если нет консоли
            {
                AllocConsole(); // выделяем консоль
                InvalidateOutAndError(); // обновляем потоки
            }
            //#endif
        }

        /// <summary>
        /// Если процесс имеет привязанную консоль, она будет отключена и скрыта. Запись в System.Console все еще возможна, но вывод не будет виден.
        /// </summary>
        public static void Hide()
        {
            //#if DEBUG
            if (HasConsole) // если есть консоль
            {
                SetOutAndErrorNull(); // обнуляем потоки
                FreeConsole(); // освобождаем консоль
            }
            //#endif
        }

        public static void Toggle() // переключение консоли
        {
            if (HasConsole)
            {
                Hide(); // скрываем
            }
            else
            {
                Show(); // показываем
            }
        }

        static void InvalidateOutAndError() // обновление потоков вывода
        {
            Type type = typeof(System.Console); // тип Console

            System.Reflection.FieldInfo _out = type.GetField("_out", // поле _out
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.FieldInfo _error = type.GetField("_error", // поле _error
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            System.Reflection.MethodInfo _InitializeStdOutError = type.GetMethod("InitializeStdOutError", // метод инициализации
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            Debug.Assert(_out != null); // проверка что поле существует
            Debug.Assert(_error != null); // проверка что поле существует

            Debug.Assert(_InitializeStdOutError != null); // проверка что метод существует

            _out.SetValue(null, null); // устанавливаем null
            _error.SetValue(null, null); // устанавливаем null

            _InitializeStdOutError.Invoke(null, new object[] { true }); // вызываем инициализацию
        }

        static void SetOutAndErrorNull() // установка потоков в null
        {
            Console.SetOut(TextWriter.Null); // устанавливаем вывод в null
            Console.SetError(TextWriter.Null); // устанавливаем ошибки в null
        }
    }
}
