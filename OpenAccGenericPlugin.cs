using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Text;


namespace CustomPlugin
{
    public class OpenAccGenericPlugin : MonoBehaviour, OpenAccAPI
    {
        private float _NextActionTimeLeftController = 0.0f;
        private float _PeriodLeftController = 0.25f;

        private float _NextActionTimeRightController = 0.0f;
        private float _PeriodRightController = 0.25f;

        private bool _IsAButtonPressed = false;
        private bool _IsBButtonPressed = false;
        private bool _IsGripTriggerPressed = false;
        private bool _IsIndexTriggerPressed = false;
        private bool _IsJoystickButtonPressed = false;
        private bool _JoystickMovedRightOrDown = false;
        private bool _JoystickMovedLeftOrUp = false;

        private CustomTTSEngine TTSEngine;
        private GamePlayMetaDataLogger Logger;

        private List<MetaDataObject> CurrentSceneMetaDataObjects;

        private bool subscribed = false;
        private MetaDataObject CurrentItemOnFocus;
        private bool PrintInputModule = true;

        private float _nextActionTime = 0.0f;
        private float _period = 5.0f;

        private Dictionary<string, Vector3> lastKnownPositions = new Dictionary<string, Vector3>();

        private string csvFilePath;
        private int frameIndex = 0;

        private string staticJsonPath;
        private string frameJsonPath;
        private HashSet<string> recordedStaticObjects = new HashSet<string>();

        [Serializable]
        private class StaticObjectInfo
        {
            public string ObjectName;
            public string Scene;
            public string Parent;
            public int Level;
            public string Type;
            public string Tag;
            public string Layer;
            public bool Active;
            public string Position;
            public string Rotation;
            public string Scale;
            public bool InView;
            public string Components;
        }

        [Serializable]
        private class FrameUpdate
        {
            public int Frame;
            public Dictionary<string, string> Positions = new Dictionary<string, string>();
        }


        private static string GetHierarchyPath(Transform t)
        {
            var stack = new Stack<string>(); while (t != null) { stack.Push(t.name); t = t.parent; }
            return string.Join("/", stack); 
        }

        void OnEnable()
        {
            CurrentSceneMetaDataObjects = new List<MetaDataObject>();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            
            TTSEngine = new CustomTTSEngine();
            TTSEngine.InitializeSpeech();
            TTSEngine.Speak("Ally lab at Penn State University.");
            //Logger.Log("Application Version : " + Application.version);


            Logger = new GamePlayMetaDataLogger("OpenAccGenericPlugin.txt");
            Logger.ActivateLogger();
            InitCSVLogger();
            Logger.Log("Ally lab at Penn State University.");

            CurrentItemOnFocus = new MetaDataObject(null);
        }

        void Start()
        {
            Logger.Log("Inside Start.");
        }


        void OnDestroy()
        {
            Logger.Log("Inside Destroy.");
            TTSEngine.DestroySpeech();
            Logger.SaveLogtoFile();
        }

        void Update()
        {

            // Call every 5 seconds
            if (Time.time >= _nextActionTime)
            {
                _nextActionTime = Time.time + _period; // reset timer
                List<GameObject> allObjects = LogAllGameObjectsEverywhereWithHierarchy();

                Logger.Log($"Found {allObjects.Count} game objects in scene.");
                // You can process them here or pass them to another function
            }

            //if (EventSystem.current != null)
            //{
            //    if (PrintInputModule)
            //    {
            //        Logger.Log(EventSystem.current.ToString());
            //        PrintInputModule = false;
            //    }
            //    string eventSystemCurrentString = EventSystem.current.ToString();

            //    if (eventSystemCurrentString.Contains("XRUIInputModule"))
            //    {
            //        XRUIInputModule xruiInputModule = FindObjectOfType<XRUIInputModule>();

            //        if (xruiInputModule != null && subscribed == false)
            //        {
            //            xruiInputModule.pointerEnter += HandlePointerEnter;
            //            subscribed = true;
            //            Logger.Log("Subscribed.");
            //        }

            //    }
            //    else
            //    {
            //        ReadOutCurrentItemUsingToStringAnalysis();
            //    }

            //    if (CurrentItemOnFocus.item != null)
            //    {
            //        TTSEngine.Speak(CurrentItemOnFocus.name);
            //        Logger.Log(CurrentItemOnFocus.name);
            //    }

            //}
            //else
            //{
            //    Logger.Log("EventSystem.current is NULL");
            //}
        }

        private void InitCSVLogger()
        {

        }

        public List<GameObject> LogAllGameObjectsEverywhereWithHierarchy()
        {
            var allObjects = new List<GameObject>();
            var foundObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            var hierarchy = new Dictionary<GameObject, List<GameObject>>();
            var roots = new List<GameObject>();

            foreach (var go in foundObjects)
            {
                if (go.hideFlags != HideFlags.None) continue;
                allObjects.Add(go);

                var parent = go.transform.parent;
                if (parent == null) roots.Add(go);
                else
                {
                    if (!hierarchy.ContainsKey(parent.gameObject))
                        hierarchy[parent.gameObject] = new List<GameObject>();
                    hierarchy[parent.gameObject].Add(go);
                }
            }

            //foreach (var root in roots)
            //    PrintHierarchyRecursive(root, hierarchy, 0);

            UpdateJsonPositions(allObjects);
            return allObjects;
        }


        private void UpdateJsonPositions(List<GameObject> allObjects)
        {
            frameIndex++;
            var cam = Camera.main;

            // Group all objects by scene name
            var groupedByScene = allObjects
                .Where(go => go != null)
                .GroupBy(go => string.IsNullOrEmpty(go.scene.name) ? "NoScene" : go.scene.name);

            foreach (var sceneGroup in groupedByScene)
            {
                string sceneName = sceneGroup.Key;
                string staticCsvPath = $"{sceneName}_static.csv";
                string frameCsvPath = $"{sceneName}_frames.csv";

                bool staticHeaderWritten = File.Exists(staticCsvPath);
                bool frameHeaderWritten = File.Exists(frameCsvPath);

                var staticSb = new StringBuilder();
                var frameSb = new StringBuilder();

                // --- STATIC METADATA ---
                if (!staticHeaderWritten)
                {
                    staticSb.AppendLine("ObjectName,Scene,Parent,Level,Type,Layer,Active,Position,Rotation,Scale,InView,HasRenderer,HasCollider,Components");
                }

                foreach (var go in sceneGroup)
                {
                    string key = GetHierarchyPath(go.transform);
                    if (recordedStaticObjects.Contains(key)) continue;

                    string parent = go.transform.parent ? GetHierarchyPath(go.transform.parent) : "None";
                    int level = 0;
                    { var t = go.transform; while (t.parent) { level++; t = t.parent; } }
                    string type =
                        go.GetComponent<Camera>() ? "Camera" :
                        go.GetComponent<Light>() ? "Light" :
                        go.GetComponent<Rigidbody>() ? "PhysicsBody" :
                        go.GetComponent<MeshRenderer>() ? "Mesh" : "Empty";

                    string layer = LayerMask.LayerToName(go.layer);
                    bool active = go.activeInHierarchy;

                    Vector3 pos = go.transform.position;
                    Vector3 rot = go.transform.eulerAngles;
                    Vector3 scale = go.transform.localScale;

                    bool inView = false;
                    if (cam != null)
                    {
                        var v = cam.WorldToViewportPoint(pos);
                        inView = (v.z > 0 && v.x > 0 && v.x < 1 && v.y > 0 && v.y < 1);
                    }

                    bool hasRenderer = go.GetComponent<Renderer>() != null;
                    bool hasCollider = go.GetComponent<Collider>() != null;

                    string comps = string.Join("|", go.GetComponents<Component>().Select(c => c.GetType().Name));

                    string posStr = $"[{pos.x:F3},{pos.y:F3},{pos.z:F3}]";
                    string rotStr = $"[{rot.x:F3},{rot.y:F3},{rot.z:F3}]";
                    string scaleStr = $"[{scale.x:F3},{scale.y:F3},{scale.z:F3}]";

                    staticSb.AppendLine($"{Escape(key)},{Escape(sceneName)},{Escape(parent)},{level},{Escape(type)},{Escape(layer)},{active}," +
                                        $"{Escape(posStr)},{Escape(rotStr)},{Escape(scaleStr)},{inView},{hasRenderer},{hasCollider},{Escape(comps)}");

                    recordedStaticObjects.Add(key);
                }

                if (staticSb.Length > 0)
                    File.AppendAllText(staticCsvPath, staticSb.ToString(), Encoding.UTF8);


                // --- DYNAMIC FRAME POSITIONS ---
                if (!frameHeaderWritten)
                {
                    frameSb.AppendLine("Frame,ObjectName,Position,RotationEuler,RotationQuat,RendererCenter,RendererSize,ColliderCenter,ColliderSize");
                }

                foreach (var go in sceneGroup)
                {
                    if (go == null) continue;

                    string key = GetHierarchyPath(go.transform);

                    // Position + rotation
                    Vector3 pos = go.transform.position;
                    Quaternion rotQ = go.transform.rotation;
                    Vector3 rotE = go.transform.eulerAngles;

                    // Renderer bounds
                    Bounds? rendererBounds = null;
                    var renderer = go.GetComponent<Renderer>();
                    if (renderer != null) rendererBounds = renderer.bounds;

                    // Collider bounds
                    Bounds? colliderBounds = null;
                    var collider = go.GetComponent<Collider>();
                    if (collider != null) colliderBounds = collider.bounds;

                    string posStr = $"[{pos.x:F3},{pos.y:F3},{pos.z:F3}]";
                    string rotEStr = $"[{rotE.x:F3},{rotE.y:F3},{rotE.z:F3}]";
                    string rotQStr = $"[{rotQ.x:F3},{rotQ.y:F3},{rotQ.z:F3},{rotQ.w:F3}]";

                    string rendCenterStr = rendererBounds.HasValue ?
                        $"[{rendererBounds.Value.center.x:F3},{rendererBounds.Value.center.y:F3},{rendererBounds.Value.center.z:F3}]" : "[]";
                    string rendSizeStr = rendererBounds.HasValue ?
                        $"[{rendererBounds.Value.size.x:F3},{rendererBounds.Value.size.y:F3},{rendererBounds.Value.size.z:F3}]" : "[]";

                    string collCenterStr = colliderBounds.HasValue ?
                        $"[{colliderBounds.Value.center.x:F3},{colliderBounds.Value.center.y:F3},{colliderBounds.Value.center.z:F3}]" : "[]";
                    string collSizeStr = colliderBounds.HasValue ?
                        $"[{colliderBounds.Value.size.x:F3},{colliderBounds.Value.size.y:F3},{colliderBounds.Value.size.z:F3}]" : "[]";

                    frameSb.AppendLine($"{frameIndex},{Escape(key)},{Escape(posStr)},{Escape(rotEStr)},{Escape(rotQStr)}," +
                                       $"{Escape(rendCenterStr)},{Escape(rendSizeStr)},{Escape(collCenterStr)},{Escape(collSizeStr)}");
                }

                File.AppendAllText(frameCsvPath, frameSb.ToString(), Encoding.UTF8);


            }
        }

        // CSV escape helper
        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }



        // --- Minimal JSON helpers (no deps) ---
        private static void AppendKey(StringBuilder sb, string key)
        {
            sb.Append('\"').Append(JsonEscape(key)).Append('\"');
        }

        private static void AppendKeyValue(StringBuilder sb, string key, string value)
        {
            sb.Append('\"').Append(JsonEscape(key)).Append("\":\"").Append(JsonEscape(value)).Append('\"');
        }

        private static void AppendKeyValue(StringBuilder sb, string key, int value)
        {
            sb.Append('\"').Append(JsonEscape(key)).Append("\":").Append(value);
        }

        private static void AppendKeyValue(StringBuilder sb, string key, bool value)
        {
            sb.Append('\"').Append(JsonEscape(key)).Append("\":").Append(value ? "true" : "false");
        }

        private static void WriteVector3(StringBuilder sb, Vector3 v)
        {
            // JSON array [x,y,z] with invariant culture
            sb.Append('[')
              .Append(v.x.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(v.y.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(v.z.ToString(System.Globalization.CultureInfo.InvariantCulture))
              .Append(']');
        }

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }


        private void PrintHierarchyRecursive(GameObject obj, Dictionary<GameObject, List<GameObject>> hierarchy, int depth)
        {
            string indent = new string(' ', depth * 2);
            string sceneName = string.IsNullOrEmpty(obj.scene.name) ? "No Scene" : obj.scene.name;
            Vector3 pos = obj.transform.position;

            string type =
                obj.GetComponent<Camera>() ? "Camera" :
                obj.GetComponent<Light>() ? "Light" :
                obj.GetComponent<Rigidbody>() ? "PhysicsBody" :
                obj.GetComponent<MeshRenderer>() ? "Mesh" : "Empty";

            string comps = string.Join(", ", obj.GetComponents<Component>().Select(c => c.GetType().Name));
            string path = GetHierarchyPath(obj.transform);

            Logger.Log($"{indent}{path} (Scene: {sceneName})");
            Logger.Log($"{indent}  Type: {type} | Level: {depth} | Tag: {obj.tag} | Layer: {LayerMask.LayerToName(obj.layer)} | Active: {obj.activeInHierarchy}");
            Logger.Log($"{indent}  Components: {comps}");
            Logger.Log($"{indent}  Position: {pos}");

            if (lastKnownPositions.TryGetValue(path, out Vector3 lastPos) && pos != lastPos)
                Logger.Log($"{indent}  Moved -> {pos} (Δ = {(pos - lastPos).magnitude:F2})");

            lastKnownPositions[path] = pos;

            if (hierarchy.ContainsKey(obj))
                foreach (var child in hierarchy[obj])
                    PrintHierarchyRecursive(child, hierarchy, depth + 1);
        }





        public void ReadOutCurrentItemUsingToStringAnalysis()
        {
            string currentInputModuleString = EventSystem.current.currentInputModule.ToString();
            string target = "<b>pointerEnter</b>: ";

            int index = currentInputModuleString.IndexOf(target);

            if (index != -1)
            {

                int start = index + target.Length;
                string name = "";

                for (int i = start; ; i++)
                {
                    if (currentInputModuleString[i] != '(' && currentInputModuleString[i] != '<')
                        name += currentInputModuleString[i];
                    else
                        break;
                }

                if (name != "")
                {
                    Logger.Log("Entered:" + name);

                    GameObject foundObject = GameObject.Find(name);
                    if (foundObject != null)
                    {
                        Logger.Log("Found GameObject: " + foundObject.name);

                        MetaDataObject metaDataObject = CreateNewMetaDataObject(foundObject);

                        AddMetaDataObjectToList(metaDataObject);
                        CurrentItemOnFocus = metaDataObject;
                    }
                    TTSEngine.Speak(name);
                }
            }
        }



        public GameObject GetGameObjectByPoint2D(float x, float y)
        {
            return null;
        }
        public GameObject GetGameObjectByPoint3D(float x, float y, float z) 
        {
            return null;
        }

        public GameObject GetLastSelectedGameObject() 
        {
            return EventSystem.current.currentSelectedGameObject;
        }

        public GameObject GetGameObjectByName(string name)
        {
            return GameObject.Find(name);
        }

        public void CreateHapticImpulseRightController(int strength)
        {
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightHandDevices);

            rightHandDevices[0].SendHapticImpulse(0u, 1, strength);
        }

        public void CreateHapticImpulseLeftController(int strength)
        {
            var leftHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftHandDevices);

            leftHandDevices[0].SendHapticImpulse(0u, 1, strength);
        }

        public virtual void SimulateClickEvent()
        {

        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.Log("Scene Loaded: " + scene.name);
            Logger.Log("Scene Load Mode: " + mode);

            CurrentSceneMetaDataObjects.Clear();
            subscribed = false;

            LogAllGameObjectsEverywhereWithHierarchy();
        }

        public void OnActiveSceneChanged(Scene OldScene, Scene NewScene)
        {
            Logger.Log("Scene Loaded: " + NewScene.name);
  
            CurrentSceneMetaDataObjects.Clear();
            subscribed = false;

            LogAllGameObjectsEverywhereWithHierarchy();
        }



        public bool DetectJoystickRightOrDownMovement()
        {
            Vector2 joystickValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickValue))
                {
                    if (joystickValue.x > 0.5f || joystickValue.y < -0.5f)
                    {
                        if (!_JoystickMovedRightOrDown)
                        {
                            Debug.Log("Joystick moved to the right.");
                            Logger.Log("Joystick moved to the right.");
                            _JoystickMovedRightOrDown = true;
                            return true;
                        }
                    }
                    else if (_JoystickMovedRightOrDown)
                    {
                        _JoystickMovedRightOrDown = false;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectJoystickLeftOrUpMovement()
        {
            Vector2 joystickValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickValue))
                {
                    if (joystickValue.x < -0.5f || joystickValue.y > 0.5f)
                    {
                        if (!_JoystickMovedLeftOrUp)
                        {
                            Debug.Log("Joystick moved to the left.");
                            Logger.Log("Joystick moved to the left.");
                            _JoystickMovedLeftOrUp = true;
                            return true;
                        }
                    }
                    else if (_JoystickMovedLeftOrUp)
                    {
                        _JoystickMovedLeftOrUp = false;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectJoystickButtonPress()
        {
            bool buttonValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxisClick,
                    out buttonValue) && buttonValue)
                {
                    if (!_IsJoystickButtonPressed)
                    {
                        Debug.Log("Joystick Button is pressed.");
                        Logger.Log("Joystick Button is pressed.");
                        _IsJoystickButtonPressed = true;
                        return true;
                    }
                }
                else if (_IsJoystickButtonPressed)
                {
                    _IsJoystickButtonPressed = false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectController_A_ButtonPress()
        {
            bool triggerValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.secondaryButton,
                    out triggerValue) && triggerValue)
                {
                    if (!_IsAButtonPressed)
                    {
                        Debug.Log("A button is pressed.");
                        Logger.Log("A button is pressed.");
                        _IsAButtonPressed = true;
                        return true;
                    }
                }
                else if (_IsAButtonPressed)
                {
                    _IsAButtonPressed = false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectController_B_ButtonPress()
        {
            bool triggerValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primaryButton,
                    out triggerValue) && triggerValue)
                {
                    if (!_IsBButtonPressed)
                    {
                        Debug.Log("B button is pressed.");
                        Logger.Log("B button is pressed.");
                        _IsBButtonPressed = true;
                        return true;
                    }
                }
                else if (_IsBButtonPressed)
                {
                    _IsBButtonPressed = false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectGripTriggerPress()
        {
            bool triggerValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.gripButton,
                    out triggerValue) && triggerValue)
                {
                    if (!_IsGripTriggerPressed)
                    {
                        Debug.Log("Grip Trigger is pressed.");
                        Logger.Log("Grip Trigger is pressed.");
                        _IsGripTriggerPressed = true;
                        return true;
                    }
                }
                else if (_IsGripTriggerPressed)
                {
                    _IsGripTriggerPressed = false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectIndexTriggerPress()
        {
            bool triggerValue;
            var rightHandDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics
                (InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

            try
            {
                if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.triggerButton,
                    out triggerValue) && triggerValue)
                {
                    if (!_IsIndexTriggerPressed)
                    {
                        Debug.Log("Index Trigger is pressed.");
                        Logger.Log("Index Trigger is pressed.");
                        _IsIndexTriggerPressed = true;
                        return true;
                    }
                }
                else if (_IsIndexTriggerPressed)
                {
                    _IsIndexTriggerPressed = false;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public bool DetectLeftControllerTimeout()
        {
            if (Time.time > _NextActionTimeLeftController)
            {
                _NextActionTimeLeftController = Time.time + _PeriodLeftController;
                return true;
            }
            return false;
        }

        public bool DetectRightControllerTimeout()
        {
            if (Time.time > _NextActionTimeRightController)
            {
                _NextActionTimeRightController = Time.time + _PeriodRightController;
                return true;
            }
            return false;
        }

        public void ReadOutCurrentCursorOrientation()
        {
            Quaternion controllerRotation = OVRInput.
                GetLocalControllerRotation(OVRInput.Controller.RTouch);
            Vector3 controllerEulerAngles = controllerRotation.eulerAngles;

            float xAngle = Mathf.Round(controllerEulerAngles.x);
            float yAngle = Mathf.Round(controllerEulerAngles.y);
            float zAngle = Mathf.Round(controllerEulerAngles.z);

            if (xAngle > 180f)
            {
                xAngle = xAngle - 360f;
            }

            if (yAngle > 180f)
            {
                yAngle = yAngle - 360f;
            }
            
            string xAngleText;
            string yAngleText;

            if (xAngle >= 0)
            {
                xAngleText = "downwards";
            }
            else 
            {
                xAngleText = "upwards";
                xAngle = -xAngle;
            }

            if (yAngle >= 0)
            {
                yAngleText = "to the right";
            }
            else
            {
                yAngleText = "to the left";
                yAngle = -yAngle;
            }

            string infoToSpeak = string.
              Format("You are {0} degrees rotated " + xAngleText + " and {1} degrees rotated " + yAngleText,
              xAngle, yAngle);

            TTSEngine.Speak(infoToSpeak);
        }


        public Scene GetCurrentScene()
        {
            return new Scene();
        }

        // Should come from an LM (llama-3)
        public string DescribeScene(Scene scene)
        {
            return "";
        }

        public GameObject GetRootGameObject()
        {
            return null;
        }

        public virtual void GameSpecificAccessibilityPatch()
        {
            // Do nothing
        }

        public bool HasSceneChanged()
        {
           // TO DO
           return false;
        }

        public string GetGameObjectCoordinates(GameObject obj)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3[] globalcorners = new Vector3[4];
                rectTransform.GetWorldCorners(globalcorners);

                string globalString = "";

                for (int i = 0; i < 4; i++)
                {
                    if (i == 0)
                        globalString += "BL: ";
                    else if (i == 1)
                        globalString += "TL: ";
                    else if (i == 2)
                        globalString += "TR: ";
                    else if (i == 3)
                        globalString += "BR: ";

                    globalString += globalcorners[i].ToString();

                    if (i < 3)
                    {
                        globalString += ", ";
                    }
                }

                return globalString;
            }
            return "";
        }



        public string GetGameObjectScreenCoordinates(GameObject obj)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector3[] globalCorners = new Vector3[4];
                rectTransform.GetWorldCorners(globalCorners);

                string globalString = "";

                Camera cam = Camera.main;

                for (int i = 0; i < 4; i++)
                {
                    Vector3 screenPoint = cam.WorldToScreenPoint(globalCorners[i]);

                    if (i == 0)
                        globalString += "BL: ";
                    else if (i == 1)
                        globalString += "TL: ";
                    else if (i == 2)
                        globalString += "TR: ";
                    else if (i == 3)
                        globalString += "BR: ";

                    globalString += $"({screenPoint.x}, {screenPoint.y})";

                    if (i < 3)
                    {
                        globalString += ", ";
                    }
                }

                return globalString;
            }
            return "";
        }


        public List<GameObject> GetAllGameObjectsFromCurrentScene()
        {
            List<GameObject> allGameObjects = new List<GameObject>();

            Scene currentScene = SceneManager.GetActiveScene();

            Logger.Log(currentScene.name);

            if (currentScene == null)
            {
                return null;
            }

            GameObject[] rootGameObjects = currentScene.GetRootGameObjects();

            foreach (GameObject rootGameObject in rootGameObjects)
            {
                TraverseGameObjectHierarchy(rootGameObject, 0, allGameObjects);
            }

            return allGameObjects;
        }

        private void TraverseGameObjectHierarchy(GameObject obj, int level, List<GameObject> allGameObjects)
        {
            allGameObjects.Add(obj);

            Text textComponent = obj.GetComponent<Text>();
            if (textComponent != null)
            {
                Logger.Log("renamed.");
                obj.name = textComponent.text;
            }

            if (obj.transform.parent!= null)
            {
                if (obj.name == "Wrapper" || obj.name == "BG")
                {
                    obj.name = obj.transform.parent.name;
                    Logger.Log("renamed to parent.");
                }
            }

            string screenCoordinates = obj.transform.position.ToString();

            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            Renderer renderer = obj.GetComponent<Renderer>();

            string coords = "";

            if (rectTransform != null)
            {
                coords = GetGameObjectCoordinates(obj);
            }

            if (renderer != null)
            {
                // Logger.Log("renderer not null.");
            }

            if (coords != "")
            {
                Debug.Log(new string(' ', level * 2) + obj.name + " Coordinates: " + coords);
                Logger.Log(new string(' ', level * 2) + obj.name + " Coordinates: " + coords);
            }

            foreach (Transform child in obj.transform)
            {
                TraverseGameObjectHierarchy(child.gameObject, level + 1, allGameObjects);
            }
        }

        public void HandlePointerEnter(GameObject gameObject, PointerEventData eventData)
        {
/*            Logger.Log($"Pointer entered GameObject: {gameObject.name}," +  $" PointerEventData: " +
                $"{eventData.pointerEnter.name}");
            Logger.Log("Entered Event.");*/

            GameObject go = gameObject;
            MetaDataObject metaDataObject = CreateNewMetaDataObject(go);

            AddMetaDataObjectToList(metaDataObject);
            CurrentItemOnFocus = metaDataObject;
        }


        public MetaDataObject CreateNewMetaDataObject(GameObject go) 
        {
            MetaDataObject metaDataObject = new MetaDataObject(go);

            metaDataObject.name = go.name;
            metaDataObject.type = go.tag;
            metaDataObject.coordinates = GetGameObjectCoordinates(go);
            metaDataObject.parent = null;

            if (go.transform.parent != null)
                metaDataObject.parent = go.transform.parent.gameObject;

            List<GameObject> children = new List<GameObject>();

            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                children.Add(child.gameObject);
            }

            metaDataObject.children = children;
            return metaDataObject;
        }


        public void HandleRayCast(PointerEventData data, List<RaycastResult> raycastResults)
        {
            Logger.Log("Raycast Event.");
            Logger.Log($"Pointer event data: {data}," +  $" raycastResults: {raycastResults}");
        }


        public void AddMetaDataObjectToList(MetaDataObject metaDataObject) 
        {
            bool exists = CurrentSceneMetaDataObjects.Exists(obj => obj.name == metaDataObject.name);

            if (!exists)
            {
                CurrentSceneMetaDataObjects.Add(metaDataObject);
            }
        }

        public void PrintMetaDataObjectList()
        {
            foreach (MetaDataObject obj in CurrentSceneMetaDataObjects)
            {
                Logger.Log("Name:" + $"{obj.name}");
                Logger.Log("Coordinates: " + $"{obj.coordinates}");
            }
        }


    }

}


