using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WPF_App.Core;

namespace WPF_App.MVVM.ViewModel
{
    internal class BattleShip_Connection_ViewModel : ObservableObject
    {
        public event Action<BattleShipServer.Client> ConnectedToServer; // событие подключения к серверу

        private bool _connecting;
        public bool Connecting // идет ли подключение
        {
            get => _connecting;
            set { _connecting = value; OnPropertyChanged(); }
        }

        private bool _connected;
        public bool Connected // подключен ли
        {
            get => _connected;
            set { _connected = value; OnPropertyChanged(); }
        }

        private string _myIp;
        public string MyIp // мой IP адрес
        {
            get => _myIp;
            set { _myIp = value; OnPropertyChanged(); }
        }

        private string _opponentIp;
        public string OpponentIp // IP противника
        {
            get => _opponentIp;
            set { _opponentIp = value; OnPropertyChanged(); }
        }

        public ICommand ConnectCommand { get; set; } // команда подключения

        private BattleShipServer.Client client; // клиент игры

        public BattleShip_Connection_ViewModel()
        {
            client = new BattleShipServer.Client(); // создаем клиент

            MyIp = Network.GetLocalIPAddress(); // получаем локальный IP
            OpponentIp = MyIp; // по умолчанию тот же IP

            ConnectCommand = new RelayCommand(
                async _ => await ConnectToServer(), // действие подключения
                _ => !string.IsNullOrEmpty(OpponentIp) && !Connecting && !Connected); // условие выполнения
        }

        private async Task ConnectToServer() // подключение к серверу
        {
            try
            {
                Connecting = true; // началось подключение
                await client.StartClient(OpponentIp); // запускаем клиент
                this.Connected = true; // подключено
                Connecting = false; // подключение завершено

                ConnectedToServer?.Invoke(client); // вызываем событие
            }
            catch (System.Exception ex) // ошибка подключения
            {
                Connecting = false; // сбрасываем флаг подключения
            }
        }
    }
}