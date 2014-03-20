using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Networking.Connectivity;

namespace NowReadable.Utilities.Commands
{
    public class SimpleCommand : ICommand
    {
        public SimpleCommand(Action<object> handler)
        {
            _handler = handler;
            _isEnabled = true;
        }

        public void FireCanExecuteChanged(object sender, EventArgs args)
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(sender, args);
            }
        }

        public virtual bool CanExecute(object parameter)
        {
            return IsEnabled;
        }

        public virtual void Execute(object parameter)
        {
            _handler(parameter);
        }
        
        private bool _isEnabled;
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    FireCanExecuteChanged(this, EventArgs.Empty);
                }
            }
        }


        public event EventHandler CanExecuteChanged;
        private Action<object> _handler;
    }

    public class NetworkEnabledCommand : SimpleCommand
    {
        public NetworkEnabledCommand(Action<object> handler) : base(handler)
        {
            DeviceNetworkInformation.NetworkAvailabilityChanged += DeviceNetworkInformation_NetworkAvailabilityChanged;
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        void DeviceNetworkInformation_NetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            FireCanExecuteChanged(this, EventArgs.Empty);
        }

        void NetworkInformation_NetworkStatusChanged(object sender)
        {
            FireCanExecuteChanged(this, EventArgs.Empty);
        }

        public override bool CanExecute(object parameter)
        {
            return NetworkInterface.GetIsNetworkAvailable() && base.CanExecute(parameter);
        }

        public override void Execute(object parameter)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                base.Execute(parameter);
            }
            else
            {
                MessageBox.Show("Cannot perform this action while offline.");
            }
        }
    }
}
