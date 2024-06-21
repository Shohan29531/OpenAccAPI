using UnityEngine;
using IllusionPlugin;

namespace CustomPlugin
{
    public class Plugin : IPlugin
    {
        public string Name { get { return "PluginForGameX"; } }
        public string Version { get { return "1.0"; } }

        public void OnApplicationStart()
        {
            Debug.Log("Plugin.Init");
            GameObject pluginForGameX = new GameObject("PluginForGameX");
            pluginForGameX.AddComponent<PluginForGameX>();
            Object.DontDestroyOnLoad(pluginForGameX);
        }

        public void OnApplicationQuit()
        {
            // Implementation for OnApplicationQuit
            Debug.Log("Plugin.Exit");
        }

        public void OnLevelWasLoaded(int level)
        {
            // Implementation for OnLevelWasLoaded
        }

        public void OnLevelWasInitialized(int level)
        {
            // Implementation for OnLevelWasInitialized
        }

        public void OnUpdate()
        {
            // Implementation for OnUpdate
        }

        public void OnFixedUpdate()
        {
            // Implementation for OnFixedUpdate
        }
    }
}