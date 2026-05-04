using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using VRageMath;

namespace ClientPlugin.Settings.Elements
{
    internal class DescriptionAttribute : Attribute, IElement
    {
        public readonly string Description;

        public DescriptionAttribute(string description = null)
        {
            Description = description;
        }

        public List<Control> GetControls(string name, Func<object> propertyGetter, Action<object> propertySetter)
        {
            if (string.IsNullOrWhiteSpace(Description))
                return [];

            var label = new MyGuiControlLabel(text: Description)
            {
                ColorMask = Color.LightGray,
            };
            label.Autowrap(0.35f);

            label.Size = new Vector2(label.Size.X, label.Size.Y + 0.025f);

            return
            [
                new Control(label,
                    minWidth: Control.LabelMinWidth,
                    fillFactor: 1f)
            ];
        }

        public List<Type> SupportedTypes { get; } = new List<Type>
        {
            typeof(object)
        };
    }
}