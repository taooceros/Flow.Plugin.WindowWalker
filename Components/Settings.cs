using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Flow.Plugin.WindowWalker.Components
{
    public class Settings : INotifyPropertyChanged
    {
        private KeyModel quickAccessKeyword = (true, false, true, false, Keys.D);
        private bool enableQuickAccessHotKey = true;
        
        public bool EnableQuickAccessHotKey
        {
            get => enableQuickAccessHotKey;
            set
            {
                enableQuickAccessHotKey = value;
                OnPropertyChanged();
            }
        }
        public KeyModel QuickAccessHotKey
        {
            get => quickAccessKeyword;
            set
            {
                quickAccessKeyword = value;
                OnPropertyChanged();
            }
        }

        public bool SearchWindowsAcrossAllVDesktop { get; set; } = false;
        
        public bool SearchCloakedWindows { get; set; } = false;
        
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

}
