
﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF_App.Core
{
    public class ObservableObject : INotifyPropertyChanged // объект с уведомлением об изменении свойств
    {
        public event PropertyChangedEventHandler PropertyChanged; // событие изменения свойства

        protected void OnPropertyChanged([CallerMemberName] string name = null) // уведомление об изменении
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); // вызов события
        }
    }
}
