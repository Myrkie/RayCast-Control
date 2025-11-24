using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClientPlugin.Settings.Tools;
using VRage.Input;
// ReSharper disable UnusedMember.Global


namespace ClientPlugin
{

    public class Config : INotifyPropertyChanged
    {
        #region Options

        public int DisappearTimeUrgentMs => oDisappearTimeUrgentMs;
        private int oDisappearTimeUrgentMs = 4000;
        public int DisappearTimeMinorMs => oDisappearTimeMinorMs;
        private int oDisappearTimeMinorMs = 2000;

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
            get;
            set => SetField(ref field, value);
        } = true;
        
        
        [Checkbox(description: "Auto sets first connected remote control to main remote control if one is not found.")]
        public bool SetMainRemote
        {
            get;
            set => SetField(ref field, value);
        } = true;

        [Separator("Custom Hotkeys")]
        [Keybind(description: "Take control of grid.")]
        public Binding TakeControl
        {
            get;
            set => SetField(ref field, value);
        } = new(MyKeys.B, false, true);

        [Keybind(description: "Cycle Grid power.")]
        public Binding CyclePower
        {
            get;
            set => SetField(ref field, value);
        } = new(MyKeys.R, false, true);

        [Keybind(description: "Power down Grid.")]
        public Binding ShutdownPower
        {
            get;
            set => SetField(ref field, value);
        } = new(MyKeys.R, false, true, true);

        [Keybind(description: "Access nearest grid Terminal.")]
        public Binding AccessTerminal
        {
            get;
            set => SetField(ref field, value);
        } = new(MyKeys.B, false, true, true);

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