﻿/*#define XRUI_INPUT_MODULE_AVAILABLE*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.Networking;
using System.Security.Permissions;
using UnityEngine.UI;
using TMPro;

#if XRUI_INPUT_MODULE_AVAILABLE
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

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

        protected CustomTTSEngine TTSEngine;
        private GamePlayMetaDataLogger Logger;

        private List<MetaDataObject> CurrentSceneMetaDataObjects;

        private bool subscribed = false;
        private MetaDataObject CurrentItemOnFocus;


        private float gameObjectUpdateTimer = 0f;
        private float readOutUpdateTimer = 0f;

        private float gameObjectUpdateInterval = 5f; // 5 seconds
        private float readOutUpdateInterval = 1f;    // 1 second
        // private HashSet<GameObject> processedRoots = new HashSet<GameObject>();


        void OnEnable()
        {
            CurrentSceneMetaDataObjects = new List<MetaDataObject>();
            SceneManager.sceneLoaded += OnSceneLoaded;
            //SceneManager.activeSceneChanged += HasSceneChanged;

            TTSEngine = new CustomTTSEngine();
            TTSEngine.InitializeSpeech();
            TTSEngine.Speak("Ally lab at Penn State University.");
            
            Logger = new GamePlayMetaDataLogger("OpenAccGenericPlugin.txt");
            Logger.ActivateLogger();
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
            if (EventSystem.current != null)
            {
                string eventSystemCurrentString = EventSystem.current.ToString();

                if (eventSystemCurrentString.Contains("XRUIInputModule"))
                {
                    #if XRUI_INPUT_MODULE_AVAILABLE
                    XRUIInputModule xruiInputModule = FindObjectOfType<XRUIInputModule>();

                    if (xruiInputModule != null && subscribed == false)
                    {
                        xruiInputModule.pointerEnter += HandlePointerEnter;
                        subscribed = true;
                    }
                    #else
                        Logger.Log("XRUIInputModule is not available in this build.");
                    #endif
                }
                else
                {
                    // Update timers
                    gameObjectUpdateTimer += Time.deltaTime;
                    readOutUpdateTimer += Time.deltaTime;

                    // Call every 5 seconds
                    if (gameObjectUpdateTimer >= gameObjectUpdateInterval)
                    {
                        // GetAllGameObjectsFromCurrentScene();
                        gameObjectUpdateTimer = 0f;
                    }

                    // Call every 1 second
                    if (readOutUpdateTimer >= readOutUpdateInterval)
                    {
                        ReadOutCurrentItemUsingToStringAnalysis();
                        readOutUpdateTimer = 0f;
                    }
                }

/*                if (CurrentItemOnFocus.item != null)
                {
                    TTSEngine.Speak(CurrentItemOnFocus.name);
                    Logger.Log(CurrentItemOnFocus.name);
                }*/

            }
        }

        public void ReadOutCurrentItemUsingToStringAnalysis()
        {
            //check for null here
            if (EventSystem.current == null) 
            {
                Logger.Log("EventSystem.current is null");
                return;
            }

            if (EventSystem.current.currentInputModule == null)
            {
                Logger.Log("EventSystem.current.currentInputModule is null");
                return;
            }

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
                    name = name.Trim();
                    Logger.Log("Before GO check:" + name);

                    GameObject foundObject = GameObject.Find(name);
                    if (foundObject != null)
                    {
                        GameObject root = foundObject.transform.root.gameObject;

                        /*if (processedRoots.Contains(root))
                        {
                            Logger.Log("Entered:" + name);
                            TTSEngine.Speak(name);
                            return;
                        }
                        processedRoots.Add(root);*/

                        TTSEngine.Speak(name);
                        Logger.Log("Entered:" + name);
                        /*                        Logger.Log("Found GameObject: " + foundObject.name);
                                                Logger.Log("Root GameObject: " + root.name);*/

                        PrintAllChildrenRecursive(root.transform, 0);

                        MetaDataObject metaDataObject = CreateNewMetaDataObject(foundObject);
                        metaDataObject.name = name;

                        AddMetaDataObjectToList(metaDataObject);
                        CurrentItemOnFocus = metaDataObject;
                    }
                    else
                    {
                        Logger.Log("GameObject is not found");
                        RaycastFromViewCenter();
                    }

                }
                else 
                {
                    Logger.Log("name if empty");
                }
            }
            else
            {
                Logger.Log("PointerEnter Doesn't exist.");
                RaycastFromViewCenter();
            }
        }


        private void RaycastFromViewCenter()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Logger.Log("No main camera found!");
                return;
            }

            Vector3 origin = cam.transform.position + cam.transform.forward * 0.2f;
            Vector3 direction = cam.transform.forward;
            float rayLength = 100f;

            Debug.DrawRay(origin, direction * rayLength, Color.red, 1.0f);

            Ray ray = new Ray(origin, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, rayLength))
            {
                GameObject hitObject = hit.collider.gameObject;

                string displayName = RenameGameObjectBasedOnTextComponents(hitObject);

                Logger.Log("Raycast hit: " + displayName);
                TTSEngine.Speak(hitObject.name);
            }
            else
            {
                Logger.Log("Raycast hit nothing");
            }
        }



        private string RenameGameObjectBasedOnTextComponents(GameObject obj)
        {
            string displayName = obj.name;

            Text uiText = obj.GetComponent<Text>();
            if (uiText != null && !string.IsNullOrWhiteSpace(uiText.text))
            {
                displayName += $" ({uiText.text})";
                obj.name = uiText.text;
            }

            TextMeshProUGUI tmpText = obj.GetComponent<TextMeshProUGUI>();
            if (tmpText != null && !string.IsNullOrWhiteSpace(tmpText.text))
            {
                displayName += $" ({tmpText.text})";
                obj.name = tmpText.text;
            }

            return displayName;
        }



        private void PrintAllChildrenRecursive(Transform parent, int depth)
        {
            string indent = new string(' ', depth * 2);

            string displayName = RenameGameObjectBasedOnTextComponents(parent.gameObject);

            Vector3 screenPos = Vector3.zero;
            if (Camera.main != null)
            {
                screenPos = Camera.main.WorldToScreenPoint(parent.position);
            }

            string screenInfo = $" [ScreenPos: ({screenPos.x:F0}, {screenPos.y:F0})]";
            // Logger.Log(indent + displayName + screenInfo);

            foreach (Transform child in parent)
            {
                PrintAllChildrenRecursive(child, depth + 1);
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
            Debug.Log("Scene Loaded: " + scene.name);
            Debug.Log("Scene Load Mode: " + mode);

            // GetAllGameObjectsFromCurrentScene();

            CurrentSceneMetaDataObjects.Clear();
            subscribed = false;
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

        public void HandlePointerEnter(GameObject gameObject, 
            PointerEventData eventData)
        {
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


