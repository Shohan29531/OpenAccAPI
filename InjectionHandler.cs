using UnityEngine;
using IllusionPlugin;

namespace CustomPlugin
{
    public class InjectionHandler : IPlugin
    {
        public string Name { get { return "OpenAccGenericPlugin"; } }
        public string Version { get { return "1.0"; } }

        public void OnApplicationStart()
        {
            Debug.Log("InjectionHandler.Init");
            GameObject DummyGameObject = new 
                GameObject("DummyGameObject");
            DummyGameObject.AddComponent<OpenAccGenericPlugin>();
            Object.DontDestroyOnLoad(DummyGameObject);
        }

        public void OnApplicationQuit()
        {
            Debug.Log("InjectionHandler.Exit");
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