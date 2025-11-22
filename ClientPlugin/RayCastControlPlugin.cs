using System;
using System.Reflection;
using VRage.Plugins;

namespace ClientPlugin
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RayCastControlPlugin : IPlugin
    {
        // ReSharper disable once UnassignedField.Global
        public static Action<string, NLog.LogLevel> PulsarLog;
        
        private static readonly string PluginName = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(
            Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute))).Title;
        
        public static void WriteToPulsarLog(string logMsg, NLog.LogLevel logLevel)
        {
            PulsarLog?.Invoke($"[{PluginName}] {logMsg}", logLevel);
        }

        public void Init(object gameInstance)
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }
    }
}