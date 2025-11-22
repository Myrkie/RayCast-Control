using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClientPlugin.Settings.Tools;
using VRage.Input;


namespace ClientPlugin
{

    public class Config : INotifyPropertyChanged
    {
        #region Options

        public int DisappearTimeUrgentMs => oDisappearTimeUrgentMs;
        private int oDisappearTimeUrgentMs = 4000;
        public int DisappearTimeMinorMs => oDisappearTimeMinorMs;
        private int oDisappearTimeMinorMs = 2000;
        
        private bool oRecursiveRemote = true;
        
        private Binding takeControl = new(MyKeys.B, false, true);
        private Binding cyclePower = new(MyKeys.R, false, true);
        private Binding shutdownPower = new(MyKeys.R, false, true, true);
        private Binding accessTerminal = new(MyKeys.B, false, true, true);

        #endregion

        #region User interface

        public readonly string Title = "Raycast Control";

        [Separator("HUD Message Display Time")]
        
        [Slider(0.1f, 5.0f, 0.1f, description: "Time (s) urgent messages remain on screen (2.0 - 5.0)")]
        public float DisappearTimeUrgent
        {
            get => oDisappearTimeUrgentMs / 1000f;
            set => SetField(ref oDisappearTimeUrgentMs, (int)(value * 1000));
        }

        [Slider(0.1f, 5.0f, 0.1f, description: "Time (s) minor messages remain on screen (2.0 - 5.0)")]
        public float DisappearTimeMinor
        {
            get => oDisappearTimeMinorMs / 1000f;
            set => SetField(ref oDisappearTimeMinorMs, (int)(value * 1000));
        }

        [Separator("General Settings")]

        [Checkbox(description: "Enable recursive mode when searching for remotes on subgrids.")]
        public bool RecursiveRemote
        {
            get => oRecursiveRemote;
            set => SetField(ref oRecursiveRemote, value);
        }

        [Separator("Custom Hotkeys")]

        [Keybind(description: "Take control of grid.")]
        public Binding TakeControl
        {
            get => takeControl;
            set => SetField(ref takeControl, value);
        }

        [Keybind(description: "Cycle Grid power.")]
        public Binding CyclePower
        {
            get => cyclePower;
            set => SetField(ref cyclePower, value);
        }

        [Keybind(description: "Power down Grid.")]
        public Binding ShutdownPower
        {
            get => shutdownPower;
            set => SetField(ref shutdownPower, value);
        }

        [Keybind(description: "Access nearest grid Terminal.")]
        public Binding AccessTerminal
        {
            get => accessTerminal;
            set => SetField(ref accessTerminal, value);
        }

        #endregion

        #region Property change notification boilerplate

        public static readonly Config Default = new();
        public static readonly Config Current = ConfigStorage.Load();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}