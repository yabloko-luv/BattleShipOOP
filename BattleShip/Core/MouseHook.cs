
﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickGuide.Core
{
    public static class MouseHook
    {
        public static event EventHandler MouseMove = delegate { }; // событие движения мыши

        public static event EventHandler MouseDown = delegate { }; // событие нажатия мыши

        public static void Start() => _hookID = SetHook(_proc); // запуск хука

        public static void stop() => UnhookWindowsHookEx(_hookID); // остановка хука

        private static LowLevelMouseProc _proc = HookCallback; // процедура обработки
        private static IntPtr _hookID = IntPtr.Zero; // ID хука

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess()) // текущий процесс
            using (ProcessModule curModule = curProcess.MainModule) // главный модуль
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, // устанавливаем хук
                  GetModuleHandle(curModule.ModuleName), 0); // дескриптор модуля
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam); // делегат низкоуровневой процедуры

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam) // нажатие левой кнопки
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)); // структура события
                MouseDown(null, new EventArgs()); // вызываем событие
            }
            if (nCode >= 0 && MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam) // движение мыши
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)); // структура события
                MouseMove(null, new EventArgs()); // вызываем событие
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam); // передаем дальше
        }

        private const int WH_MOUSE_LL = 14; // код низкоуровневого хука мыши

        private enum MouseMessages // сообщения мыши
        {
            WM_LBUTTONDOWN = 0x0201, // нажатие левой кнопки
            WM_LBUTTONUP = 0x0202, // отпускание левой кнопки
            WM_MOUSEMOVE = 0x0200, // движение мыши
            WM_MOUSEWHEEL = 0x020A, // колесо мыши
            WM_RBUTTONDOWN = 0x0204, // нажатие правой кнопки
            WM_RBUTTONUP = 0x0205 // отпускание правой кнопки
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT // точка
        {
            public int x; // координата X
            public int y; // координата Y
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT // структура хука мыши
        {
            public POINT pt; // точка
            public uint mouseData, flags, time; // данные мыши, флаги, время
            public IntPtr dwExtraInfo; // дополнительная информация
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
          LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId); // установка хука

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk); // удаление хука

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
          IntPtr wParam, IntPtr lParam); // передача хука дальше

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName); // получение дескриптора модуля
    }
}
