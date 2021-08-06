using Microsoft.Plugin.WindowWalker.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Microsoft.Plugin.WindowWalker.Components
{
    public class Settings : INotifyPropertyChanged
    {
        private KeyModel quickAccessKeyword = (true, false, true, false, Keys.D);

        public KeyModel QuickAccessKeyword
        {
            get => quickAccessKeyword;
            set
            {
                quickAccessKeyword = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public struct KeyModel
    {
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        public bool Win { get; set; }
        public Keys Key { get; set; }

        public KeyModel(bool ctrl, bool shift, bool alt, bool win, Keys key)
        {
            Ctrl = ctrl;
            Shift = shift;
            Alt = alt;
            Win = win;
            Key = key;
        }

        public KeyModel(Key key)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            this.Key = (Keys)virtualKey;
            Shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            Ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            Alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
            Win = Keyboard.IsKeyDown(System.Windows.Input.Key.LWin) || Keyboard.IsKeyDown(System.Windows.Input.Key.RWin);
        }

        public override bool Equals(object obj)
        {
            return obj is KeyModel other &&
                   Ctrl == other.Ctrl &&
                   Shift == other.Shift &&
                   Alt == other.Alt &&
                   Key == other.Key;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ctrl, Shift, Alt, Key);
        }

        public override string ToString()
        {
            var result = new List<string>();
            if (Ctrl)
                result.Add(Resources.Ctrl);
            if (Shift)
                result.Add(Resources.Shift);
            if (Alt)
                result.Add(Resources.Alt);
            if (Win)
                result.Add(Resources.Win);

            result.Add(Enum.GetName(Key));
            return string.Join('+', result);
        }

        public void Deconstruct(out bool ctrl, out bool shift, out bool alt, out Keys key)
        {
            ctrl = Ctrl;
            shift = Shift;
            alt = Alt;
            key = Key;
        }

        public static implicit operator (bool Ctrl, bool Shift, bool Alt, bool Win, Keys Key)(KeyModel value)
        {
            return (value.Ctrl, value.Shift, value.Alt, value.Win, value.Key);
        }

        public static implicit operator KeyModel((bool Ctrl, bool Shift, bool Alt, bool Win, Keys Key) value)
        {
            return new KeyModel(value.Ctrl, value.Shift, value.Alt, value.Win, value.Key);
        }

        public static bool operator ==(KeyModel left, KeyModel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(KeyModel left, KeyModel right)
        {
            return !(left == right);
        }
    }
}
