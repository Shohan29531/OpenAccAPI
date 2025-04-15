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
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;

using System.Speech.Recognition;

/*#if XRUI_INPUT_MODULE_AVAILABLE
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif*/



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
        private bool PrintInputModule = true;


        private float gameObjectUpdateTimer = 0f;
        private float readOutUpdateTimer = 0f;

        private float gameObjectUpdateInterval = 5f; // 5 seconds
        private float readOutUpdateInterval = 1f;    // 1 second

        private GameObject agentGO;
        public float agentMoveSpeed = 10f;
        private Vector3 previousMousePositionAgent;

        private SpeechRecognitionEngine recognizer;

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
            Logger.Log("Inside Start().");
            InitializeVoiceRecognition();
        }



        private void InitializeVoiceRecognition()
        {
            try
            {
                recognizer = new SpeechRecognitionEngine();
                recognizer.SetInputToDefaultAudioDevice();

                // Define simple commands
                Choices commands = new Choices();
                commands.Add(new string[] {
            "forward", "back", "left", "right",
            "click", "log", "read", "describe", "stop", "shoot", "charge"
        });

                GrammarBuilder grammarBuilder = new GrammarBuilder();
                grammarBuilder.Append(commands);

                Grammar grammar = new Grammar(grammarBuilder);
                recognizer.LoadGrammar(grammar);

                recognizer.SpeechRecognized += OnSpeechRecognized;
                recognizer.RecognizeAsync(RecognizeMode.Multiple);

                Logger.Log("Voice recognition initialized.");
            }
            catch (Exception ex)
            {
                Logger.Log("Speech recognition failed: " + ex.Message);
            }
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string command = e.Result.Text.ToLower();
            Logger.Log("Voice command recognized: " + command);

            switch (command)
            {
                case "stop":
                    ReleaseHeldKey(KeyCode.I);
                    ReleaseHeldKey(KeyCode.J);
                    ReleaseHeldKey(KeyCode.K);
                    ReleaseHeldKey(KeyCode.L);
                    NativeInputSimulator.SimulateMouseUp(0); // left
                    NativeInputSimulator.SimulateMouseUp(1); // right
                    Logger.Log("Movement and clicks stopped by voice command.");
                    break;

                case "forward":
                    HandleKey(KeyCode.I);
                    break;
                case "back":
                    HandleKey(KeyCode.K);
                    break;
                case "left":
                    HandleKey(KeyCode.J);
                    break;
                case "right":
                    HandleKey(KeyCode.L);
                    break;

                case "shoot":
                    NativeInputSimulator.SimulateMouseDown(0); // hold left click
                    Logger.Log("Holding left mouse button (shoot).");
                    break;

                case "charge":
                    NativeInputSimulator.SimulateMouseDown(1); // hold right click
                    Logger.Log("Holding right mouse button (charge).");
                    break;

                case "log":
                    PrintMetaDataObjectList();
                    break;

                case "read":
                    ReadOutCurrentItemUsingToStringAnalysis();
                    break;

                case "describe":
                    TTSEngine.Speak("Scene description is not implemented yet.");
                    break;

                default:
                    Logger.Log("Unrecognized voice command: " + command);
                    break;
            }
        }




        void OnDestroy()
        {
            Logger.Log("Inside Destroy.");
            TTSEngine.DestroySpeech();
            Logger.SaveLogtoFile();

            if (recognizer != null)
            {
                recognizer.RecognizeAsyncStop();
                recognizer.Dispose();
            }
        }

        void Update()
        {

            if (agentGO == null)
            {
                CreateRobotAgent();
            }

            if (EventSystem.current != null)
            {
                if (PrintInputModule)
                {
                    Logger.Log(EventSystem.current.ToString());
                    PrintInputModule = false;
                }
                string eventSystemCurrentString = EventSystem.current.ToString();

                if (eventSystemCurrentString.Contains("XRUIInputModule"))
                {
/*                    XRUIInputModule xruiInputModule = FindObjectOfType<XRUIInputModule>();

                    if (xruiInputModule != null && subscribed == false)
                    {
                        xruiInputModule.pointerEnter += HandlePointerEnter;
                        subscribed = true;
                        Logger.Log("Subscribed.");
                    }*/

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
                        // ReadOutCurrentItemUsingToStringAnalysis();
                        readOutUpdateTimer = 0f;
                    }
                }

                /*                if (CurrentItemOnFocus.item != null)
                                {
                                    TTSEngine.Speak(CurrentItemOnFocus.name);
                                    Logger.Log(CurrentItemOnFocus.name);
                                }*/

               DetectAndSendInputToAgent();
            }
        }


              

        private void CreateRobotAgent()
        {
            agentGO = new GameObject("RL Agent");

            // BODY
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "RL Agent Body";
            body.transform.SetParent(agentGO.transform);
            body.transform.localPosition = new Vector3(0, 0.5f, 0);
            body.transform.localScale = new Vector3(0.5f, 1f, 0.3f);
            body.GetComponent<Renderer>().material.color = Color.cyan;

            // HEAD
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "RL Agent Head";
            head.transform.SetParent(agentGO.transform);
            head.transform.localPosition = new Vector3(0, 1.25f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            head.GetComponent<Renderer>().material.color = Color.white;

            // EYES
            GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftEye.name = "RL Agent Left Eye";
            leftEye.transform.SetParent(agentGO.transform);
            leftEye.transform.localPosition = new Vector3(-0.15f, 1.3f, 0.25f);
            leftEye.transform.localScale = Vector3.one * 0.05f;
            leftEye.GetComponent<Renderer>().material.color = Color.black;

            GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightEye.name = "RL Agent Right Eye";
            rightEye.transform.SetParent(agentGO.transform);
            rightEye.transform.localPosition = new Vector3(0.15f, 1.3f, 0.25f);
            rightEye.transform.localScale = Vector3.one * 0.05f;
            rightEye.GetComponent<Renderer>().material.color = Color.black;

            // ARMS
            GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftArm.name = "RL Agent Left Arm";
            leftArm.transform.SetParent(agentGO.transform);
            leftArm.transform.localPosition = new Vector3(-0.5f, 0.75f, 0);
            leftArm.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            leftArm.GetComponent<Renderer>().material.color = Color.gray;

            GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightArm.name = "RL Agent Right Arm";
            rightArm.transform.SetParent(agentGO.transform);
            rightArm.transform.localPosition = new Vector3(0.5f, 0.75f, 0);
            rightArm.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            rightArm.GetComponent<Renderer>().material.color = Color.gray;

            // LEGS
            GameObject leftLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftLeg.name = "RL Agent Left Leg";
            leftLeg.transform.SetParent(agentGO.transform);
            leftLeg.transform.localPosition = new Vector3(-0.2f, 0.0f, 0);
            leftLeg.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
            leftLeg.GetComponent<Renderer>().material.color = Color.gray;

            GameObject rightLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightLeg.name = "RL Agent Right Leg";
            rightLeg.transform.SetParent(agentGO.transform);
            rightLeg.transform.localPosition = new Vector3(0.2f, 0.0f, 0);
            rightLeg.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
            rightLeg.GetComponent<Renderer>().material.color = Color.gray;

            // PLACE IN FRONT OF CAMERA
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 spawnPos = cam.transform.position + cam.transform.forward * 2f;
                agentGO.transform.position = spawnPos;
                agentGO.transform.rotation = Quaternion.LookRotation(-cam.transform.forward); // face the camera
            }
            else
            {
                agentGO.transform.position = new Vector3(0, 1, 0);
            }

            Logger.Log("Robot-style RL Agent created in front of camera with named body parts.");
        }



        private void DetectAndSendInputToAgent()
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    Logger.Log($"Key pressed: {key}");
                    PerformAction("KeyPress", key);
                }

                if (Input.GetKeyUp(key))
                {
                    Logger.Log($"Key released: {key}");
                    ReleaseHeldKey(key);
                }
            }

            Vector3 currentMousePosition = Input.mousePosition;
            if (Vector3.Distance(currentMousePosition, previousMousePositionAgent) > 1f)
            {
                PerformAction("MouseMove", new Vector2[]
                {
            new Vector2(previousMousePositionAgent.x, previousMousePositionAgent.y),
            new Vector2(currentMousePosition.x, currentMousePosition.y)
                });
            }
            previousMousePositionAgent = currentMousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                Logger.Log($"Mouse Left Click at ({currentMousePosition.x}, {currentMousePosition.y})");
                PerformAction("MouseClick", currentMousePosition);
            }
            if (Input.GetMouseButtonDown(1))
            {
                Logger.Log($"Mouse Right Click at ({currentMousePosition.x}, {currentMousePosition.y})");
                PerformAction("MouseClick", currentMousePosition);
            }
            if (Input.GetMouseButtonDown(2))
            {
                Logger.Log($"Mouse Middle Click at ({currentMousePosition.x}, {currentMousePosition.y})");
                PerformAction("MouseClick", currentMousePosition);
            }
        }




        private void Perceive()
        {
            // Expand later — maybe raycasting or state tracking
            Logger.Log("Agent perceives environment.");
        }

        private void PerformAction(string inputType, object value)
        {
            switch (inputType)
            {
                case "KeyPress":
                    KeyCode key = (KeyCode)value;
                    Logger.Log($"Agent received key press: {key}");
                    HandleKey(key);
                    break;

                case "MouseClick":
                    Vector3 clickPos = (Vector3)value;
                    Logger.Log($"Agent received mouse click at {clickPos}");
                    break;

                case "MouseMove":
                    Vector2[] positions = (Vector2[])value;
                    //Logger.Log($"Agent saw mouse move from {positions[0]} to {positions[1]}");
                    break;
            }
        }

        private void HandleKey(KeyCode key)
        {
            if (agentGO == null)
            {
                Logger.Log("ERROR: agentGO is null.");
                return;
            }

            byte virtualKey = 0;
            string action = "";

            switch (key)
            {
                case KeyCode.I:
                    virtualKey = 0x57; // W
                    action = "Move Forward";
                    break;
                case KeyCode.J:
                    virtualKey = 0x41; // A
                    action = "Move Left";
                    break;
                case KeyCode.K:
                    virtualKey = 0x53; // S
                    action = "Move Backward";
                    break;
                case KeyCode.L:
                    virtualKey = 0x44; // D
                    action = "Move Right";
                    break;
                default:
                    Logger.Log($"Unhandled key: {key}");
                    return;
            }

            NativeInputSimulator.SimulateKeyDown(virtualKey);
            Logger.Log($"Holding key: {key} -> {action}");

            // Optional agent movement for visual feedback
            Vector3 direction = Vector3.zero;
            switch (key)
            {
                case KeyCode.I: direction = Vector3.forward; break;
                case KeyCode.J: direction = Vector3.left; break;
                case KeyCode.K: direction = Vector3.back; break;
                case KeyCode.L: direction = Vector3.right; break;
            }

            if (direction != Vector3.zero)
            {
                agentGO.transform.position += direction * agentMoveSpeed * Time.deltaTime;
                Logger.Log($"Agent moved {direction}");
            }
        }


        private void ReleaseHeldKey(KeyCode key)
        {
            byte virtualKey = 0;

            switch (key)
            {
                case KeyCode.I: virtualKey = 0x57; break; // W
                case KeyCode.J: virtualKey = 0x41; break; // A
                case KeyCode.K: virtualKey = 0x53; break; // S
                case KeyCode.L: virtualKey = 0x44; break; // D
                default: return;
            }

            NativeInputSimulator.SimulateKeyUp(virtualKey);
            Logger.Log($"Released key: {key}");
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

                    GameObject foundObject = GameObject.Find(name);
                    if (foundObject != null)
                    {
                        GameObject root = foundObject.transform.root.gameObject;


                        TTSEngine.Speak(name);
                        Logger.Log("Entered:" + name);

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
            Logger.Log("Scene Loaded: " + scene.name);
            Logger.Log("Scene Load Mode: " + mode);

            CurrentSceneMetaDataObjects.Clear();
            subscribed = false;

            // GetAllGameObjectsFromCurrentScene();
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
