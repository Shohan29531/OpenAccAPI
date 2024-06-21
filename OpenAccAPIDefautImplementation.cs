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


namespace CustomPlugin
{
    public class OpenAccAPIDefautImplementation : CustomTTSEngine, OpenAccAPI
    {
        private float _NextActionTimeLeftController = 0.0f;
        private float _PeriodLeftController = 0.25f;

        private float _NextActionTimeRightController = 0.0f;
        private float _PeriodRightController = 0.25f;

        protected string _LastObjectName = "";

        private LineRenderer _LineRenderer;
        private Canvas _Canvas;
        private RectTransform _CanvasRectTransform;
        protected GameObject _RectangleObject;
        private Material _LineMaterial;

        private bool _PrintAllFlag = false;
        protected bool _ReadOutActualCursorItemFlag = true;
        protected bool _CursorMovementLocked = false;

        private bool _IsAButtonPressed = false;
        private bool _IsBButtonPressed = false;
        private bool _IsGripTriggerPressed = false;
        private bool _IsIndexTriggerPressed = false;
        private bool _IsJoystickButtonPressed = false;
        private bool _JoystickMovedRightOrDown = false;
        private bool _JoystickMovedLeftOrUp = false;

        protected AudioSource _BoundaryHitSound;
        protected AudioSource _MovingBetweenItemsSound;

        private static string _CurrentParticipant = "Alex";
        private List<string> _CsvRows;
        private string _CsvFilePath = _CurrentParticipant + ".csv";
        private int _CsvEntryNnumber = 1;


        protected struct AccNode
        {
            public GameObject item;
            public AccNode(GameObject item) : this()
            {
                this.item = item;
            }

            public string name;
            public string type;
            public Dictionary<string, Vector3> coordinates;
            public GameObject parent;
            public List<GameObject> children;
        }

        protected struct OnScreenItem
        {
            public string name;
            public Vector3 top_left;
            public Vector3 top_right;
            public Vector3 bottom_right;
            public Vector3 bottom_left;

            public OnScreenItem(string itemName, Vector3 tl, Vector3 tr,
                Vector3 br, Vector3 bl)
            {
                name = itemName;
                top_left = tl;
                top_right = tr;
                bottom_right = br;
                bottom_left = bl;
            }
        }

        protected class OnScreenItemComparer : IComparer<OnScreenItem>
        {
            public int Compare(OnScreenItem item1, OnScreenItem item2)
            {
                if (item1.top_left.y >= item2.bottom_right.y)
                    return -1;
                if (item2.top_left.y >= item1.bottom_right.y)
                    return 1;

                if (item2.bottom_right.x <= item1.top_left.x)
                    return 1;
                if (item1.bottom_right.x <= item2.top_left.x)
                    return -1;

                return 0;
            }
        }

        protected new void CustomOnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            base.CustomOnEnable();
            Speak("Ally lab at Penn State University.");
            InitializeHighlightRectangle();
        }

        protected new void CustomStart()
        {
            base.CustomStart();
            _CsvRows = new List<string>
            {
                "No.,Mode,Input,Action"
            };

            _BoundaryHitSound = GetComponent<AudioSource>();

            if (_BoundaryHitSound == null)
            {
                _BoundaryHitSound = gameObject.AddComponent<AudioSource>();
            }
            LoadAudio("boundary_hit.wav", _BoundaryHitSound);

            _MovingBetweenItemsSound = gameObject.AddComponent<AudioSource>();

            if (_MovingBetweenItemsSound == null)
            {
                _MovingBetweenItemsSound = gameObject.AddComponent<AudioSource>();
            }
            LoadAudio("move_between_items.wav", _MovingBetweenItemsSound);
        }

        protected new void CustomUpdate()
        {
            base.CustomUpdate();
        }

        protected new void CustomOnDestroy()
        {
            base.CustomOnDestroy();
            WriteCsvToFile(_CsvFilePath);
        }

        public void AddToCsv(string csvRow)
        {
            _CsvRows.Add(csvRow);
            _CsvEntryNnumber++;
        }

        public void WriteCsvToFile(string filePath)
        {
            try
            {
                File.WriteAllLines(filePath, _CsvRows);
                _CsvRows.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError("Error writing CSV to file: " + e.Message);
            }
        }

        public void LoadAudio(string filename, AudioSource audioSource)
        {
            StartCoroutine(LoadAudioCoroutine(filename, audioSource));
        }

        IEnumerator LoadAudioCoroutine(string filename, AudioSource audioSource)
        {
            string audioFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            Debug.Log("Loading audio from path: " + audioFilePath);
            _lines.Add("Loading audio from path: " + audioFilePath);

            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.
                GetAudioClip("file://" + audioFilePath, AudioType.WAV))
            {
                yield return audioRequest.SendWebRequest();

                if (audioRequest.isNetworkError || audioRequest.isHttpError)
                {
                    Debug.LogError("Error loading audio: " + audioRequest.error);
                }
                else
                {
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(audioRequest);

                    if (audioClip != null)
                    {
                        audioSource.clip = audioClip; 
                        Debug.Log("Audio loaded.");
                        _lines.Add("Audio loaded.");
                    }
                    else
                    {
                        Debug.LogError("Failed to load audio clip.");
                        _lines.Add("Failed to load audio clip.");
                    }
                }
            }
        }

        protected string MakeStringFromAction(string input, string action) 
        {
            string mode;
            if (_CursorMovementLocked)
                mode = "Serial";
            else
                mode = "Free";

            return _CsvEntryNnumber + "," + mode + "," + input + "," + action;    
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
            GameObject targetGameObject = GameObject.Find(name);

            if (targetGameObject != null)
            {
                
                Debug.Log("GetGameObjectByName found!");
                _lines.Add("GetGameObjectByName found!");
                return targetGameObject;
            }
            else
            {
                Debug.Log("GetGameObjectByName found NULL.");
                _lines.Add("GetGameObjectByName found NULL.");
                return null;
            }
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
        }


        public void InitializeHighlightRectangle()
        {
            _RectangleObject = new GameObject("RectangleObject");
            _LineRenderer = _RectangleObject.AddComponent<LineRenderer>();

            _LineRenderer.startColor = Color.green;
            _LineRenderer.endColor = Color.green;
            _LineRenderer.startWidth = 0.05f;
            _LineRenderer.endWidth = 0.05f;
            _LineRenderer.loop = true;

            _Canvas = new GameObject("Canvas").AddComponent<Canvas>();
            _Canvas.renderMode = RenderMode.WorldSpace;

            _CanvasRectTransform = _Canvas.GetComponent<RectTransform>();
            _CanvasRectTransform.localScale = Vector3.one * 0.1f;
            _CanvasRectTransform.position = Vector3.zero;

            _Canvas.sortingOrder = 1;

            _LineMaterial = new Material(Shader.Find("Unlit/Transparent"));
            _LineMaterial.color = Color.green;

            _LineRenderer.material = _LineMaterial;

            _RectangleObject.transform.SetParent(transform);
            _Canvas.transform.SetParent(transform);
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
                            _lines.Add("Joystick moved to the right.");
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
                            _lines.Add("Joystick moved to the left.");
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
                        _lines.Add("Joystick Button is pressed.");
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
                        _lines.Add("A button is pressed.");
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
                        _lines.Add("B button is pressed.");
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
                        _lines.Add("Grip Trigger is pressed.");
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
                        _lines.Add("Index Trigger is pressed.");
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

            Speak(infoToSpeak);
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



        public void PrintEverything(string info) 
        {
            if (_PrintAllFlag == true)
            { 
                Debug.Log(info);
                _lines.Add(info);
            }

        }

        public void DrawHighlightRectangle(Vector3[] rectangleCorners)
        {
            _LineRenderer.positionCount = 4;
            _LineRenderer.SetPositions(rectangleCorners);
        }

    }

}


