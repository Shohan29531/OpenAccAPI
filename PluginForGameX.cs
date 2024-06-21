using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRUIControls;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;

namespace CustomPlugin
{
    class PluginForGameX : OpenAccAPIDefautImplementation
    {
        private string _GameName = "X";

        private List<OnScreenItem> _OnScreenItems;
        private List<OnScreenItem> _PersistentOnScreenItems;
        private int _CurrentItemIndex = 0;

        private float _InGamePlayElapseTime = 0f;
        private float _InGamePlayTTSInterval = 0.5f;

        private Scene activeScene;

        static void WriteLineToFile(string filePath, string line)
        {
            try
            {
                // Append the line to the file or create the file if it doesn't exist
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        void OnEnable()
        {
            CustomOnEnable();
            _OnScreenItems = new List<OnScreenItem>();
        }

        void Start()
        {
            CustomStart();
        }

        string GetScreenCoordinates(GameObject obj)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // For UI elements with RectTransform
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


        void PrintGameObjectHierarchy(GameObject obj, int level)
        {
            // Check if the GameObject has a Text component
            Text textComponent = obj.GetComponent<Text>();
            if (textComponent != null)
            {
                // Replace the GameObject's name with the Text component's name
                _lines.Add("renamed.");
                obj.name = textComponent.text;
            }
            string screenCoordinates = obj.transform.position.ToString();


            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            Renderer renderer = obj.GetComponent<Renderer>();

            string coords = "";

            if (rectTransform != null)
            {
                coords = GetScreenCoordinates(obj);
            }

            if (renderer != null) 
            {
               // _lines.Add("renderer not null.");
            }


            // Print the current GameObject's name with indentation and screen coordinates
            if (coords != "")
            {
                Debug.Log(new string(' ', level * 2) + obj.name + " Coordinates: " + coords);

                _lines.Add(new string(' ', level * 2) + obj.name + " Coordinates: " + coords);
            }
            // Recursively print each child GameObject
            foreach (Transform child in obj.transform)
            {
                PrintGameObjectHierarchy(child.gameObject, level + 1);
            }
        }

        private void HandlePointerEnter(GameObject go, PointerEventData eventData)
        {
            Debug.Log($"Pointer entered GameObject: {go.name}," +
                $" PointerEventData: {eventData}");
            _lines.Add($"Pointer entered GameObject: {go.name}," +
                $" PointerEventData: {eventData}");
        }

        void Update() 
        {
            if (EventSystem.current != null)
            {
                _lines.Add(EventSystem.current.ToString());

                BaseInputModule currentInputModule = EventSystem.current.currentInputModule;
                string info = currentInputModule.ToString();
                string target = "<b>pointerEnter</b>:";

                int index = info.IndexOf(target);

                if ( index != -1)
                {

                    int start = index + target.Length;
                    string name = "";

                    for (int i = start; ; i++)
                    {
                        if (info[i] != '(' && info[i] != '<')
                            name += info[i];
                        else
                            break;
                    }

                    if (name != "")
                    {
                        _lines.Add(name);
                        Speak(name);
                    }
                }


                XRUIInputModule xruiInputModule = 
                    FindObjectOfType<XRUIInputModule>();
                if (xruiInputModule != null)
                {
                    Debug.Log("XRUIInputModule instance found!");
                    _lines.Add("XRUIInputModule instance found!");
                    xruiInputModule.pointerEnter += HandlePointerEnter;
                }
                else
                {
                    Debug.LogError("XRUIInputModule instance not found.");
                    _lines.Add("XRUIInputModule instance found!");
                }

                Debug.Log("All Scene Root Objects:");
                _lines.Add("All Scene Root Objects:");

                Scene currentScene = SceneManager.GetActiveScene();

                if (currentScene == activeScene)
                {
                    return;
                }
                activeScene = currentScene;


                // Find all root GameObjects in the current scene
                GameObject[] rootGameObjects = currentScene.GetRootGameObjects();

                foreach (GameObject rootGameObject in rootGameObjects)
                {
                    //PrintGameObjectHierarchy(rootGameObject, 0);
                }

                if (EventSystem.current.currentSelectedGameObject != null)
                {
                    _lines.Add("Currently Selected:" + EventSystem.current.currentSelectedGameObject.ToString());
                    // Speak(EventSystem.current.currentSelectedGameObject.ToString());

                }
            }
            CustomUpdate();
            /*CustomUpdate();
            if (DetectGripTriggerPress())
            {
                if (_OnScreenItems != null && _OnScreenItems.Count > 1)
                {
                    if (_CursorMovementLocked)
                    {
                        _CursorMovementLocked = false;

                        EnablePointer();

                        Speak("Cursor Enabled.");
                        CreateHapticImpulseRightController(5);
                        AddToCsv(MakeStringFromAction("Grip Trigger Press", "Cursor Enabled."));
                    }
                    else
                    {
                        _CursorMovementLocked = true;

                        DisablePointer();

                        Speak("Cursor Disabled.");
                        CreateHapticImpulseRightController(5);
                        AddToCsv(MakeStringFromAction("Grip Trigger Press", "Cursor Disabled."));
                    }
                }
                else
                {
                    // Speak("Invalid Screen Section. Cannot Disable Cursor Here.");
                    _BoundaryHitSound.Play();
                }
            }


            // pointer cursor unavailable, navigation the joystick

            if (_CursorMovementLocked == true)
            {
                if (DetectIndexTriggerPress())
                {
                    AddToCsv(MakeStringFromAction("Index Trigger Press", "Item Clicked."));
                    SimulateClickEvent();
                    _CursorMovementLocked = false;

                    EnablePointer();

                    _CurrentItemIndex = 0;
                    Speak("Cursor is now enabled.");
                }

                if (DetectRightControllerTimeout())
                {
                    AddToCsv(MakeStringFromAction("None", "Item Name Spoken on TTS."));
                    ReadOutCurrentItemAndDrawRectangle();
                }

                if (_OnScreenItems != null)
                {
                    _PersistentOnScreenItems = _OnScreenItems.ToList();
                }

                if (DetectJoystickRightOrDownMovement())
                {
                    CreateHapticImpulseRightController(2);

                    _CurrentItemIndex++;
                    if (_CurrentItemIndex == _PersistentOnScreenItems.Count)
                    {
                        AddToCsv(MakeStringFromAction("Joystick Moved Right", "Boundary Hit."));
                        _BoundaryHitSound.Play();
                        _CurrentItemIndex = _PersistentOnScreenItems.Count - 1;
                    }
                    else
                    {
                        AddToCsv(MakeStringFromAction("Joystick Moved Right", "Next Item Selected."));
                        _MovingBetweenItemsSound.Play();
                    }
                    ReadOutCurrentItemAndDrawRectangle();
                }

                if (DetectJoystickLeftOrUpMovement())
                {
                    CreateHapticImpulseRightController(2);

                    _CurrentItemIndex--;
                    if (_CurrentItemIndex == -1)
                    {
                        AddToCsv(MakeStringFromAction("Joystick Moved Left", "Boundary Hit."));
                        _BoundaryHitSound.Play();
                        _CurrentItemIndex = 0;
                    }
                    else
                    {
                        AddToCsv(MakeStringFromAction("Joystick Moved Left", "Previous Item Selected."));
                        _MovingBetweenItemsSound.Play();
                    }
                    ReadOutCurrentItemAndDrawRectangle();
                }

            }

            //  pointer cursor available, navigation via the pointer

            if (_CursorMovementLocked == false)
            {
                if (DetectRightControllerTimeout())
                {
                    if (_CursorMovementLocked == false)
                    {
                        FindAllSelectableScreenItems();
                        PrintSelectableScreenItems();
                    }
                    DetectHitInGameMenu("<b>pointerEnter</b>: ");
                }
                if (DetectController_A_ButtonPress() || DetectController_B_ButtonPress())
                {
                    AddToCsv(MakeStringFromAction("Joystick Button Pressed", "Controller Orientation on TTS."));
                    ReadOutCurrentCursorOrientation();
                }

                // In gameplay codes

                _InGamePlayElapseTime += Time.deltaTime;
                if (_InGamePlayElapseTime >= _InGamePlayTTSInterval)
                {
                    DetectAndReadClosestObstaclePosition();
                    _InGamePlayElapseTime = 0f;
                }
            }*/

        }

        void OnDestroy() 
        {
            CustomOnDestroy();
            using (FileStream fs = new FileStream(_logPath, FileMode.Create))
            {
            }
            foreach (string line in _lines)
            {
                WriteLineToFile(_logPath, line);
            }
        }

        // TODO Dev: Find all selectable on screen items
        public void FindAllSelectableScreenItems()
        {
            //
            // populate _OnScreenItems with all the selectable on screen items and their information
            //
            _OnScreenItems.Sort(new OnScreenItemComparer());
        }

        //TODO Dev: Unlock/Enable the cursor
        public void EnablePointer()
        {

        }

        //TODO Dev: Lock/Disbale the cursor
        public void DisablePointer()
        {

        }

        public override void SimulateClickEvent()
        {
            if (_PersistentOnScreenItems == null || _PersistentOnScreenItems.Count == 0)
            {
                Debug.LogError("Nothing Created Yet.");
                _lines.Add("Nothing Created Yet.");
                return;
            }

            OnScreenItem currentItem = _PersistentOnScreenItems[_CurrentItemIndex];
            GameObject target = GameObject.Find(currentItem.name);

            if (target != null)
            {
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);

                float centerX = (currentItem.top_left.x + currentItem.bottom_right.x) / 2;
                float centerY = (currentItem.top_left.y + currentItem.bottom_right.y) / 2;

                pointerEventData.position = new Vector2(centerX, centerY);

                ExecuteEvents.Execute(target, pointerEventData, ExecuteEvents.pointerClickHandler);
            }
            else
            {
                Debug.LogError("Target GameObject is null.");
                _lines.Add("Target GameObject is null.");
            }
        }

        public void ReadOutCurrentItemAndDrawRectangle()
        {
            Speak(_PersistentOnScreenItems[_CurrentItemIndex].name);

            Vector3[] corners = new Vector3[4];

            corners[0] = _PersistentOnScreenItems[_CurrentItemIndex].top_left;
            corners[1] = _PersistentOnScreenItems[_CurrentItemIndex].bottom_left;
            corners[2] = _PersistentOnScreenItems[_CurrentItemIndex].bottom_right;
            corners[3] = _PersistentOnScreenItems[_CurrentItemIndex].top_right;

            if (_RectangleObject == null)
            {
                InitializeHighlightRectangle();
                DrawHighlightRectangle(corners);
            }
            else
            {
                DrawHighlightRectangle(corners);
            }
        }


        public void PrintSelectableScreenItems()
        {
            foreach (OnScreenItem item in _OnScreenItems)
            {
                Debug.Log($"Name: {item.name}");
                _lines.Add($"Name: {item.name}");

                Debug.Log($"Top Left: {item.top_left}");
                _lines.Add($"Top Left: {item.top_left}");

                Debug.Log($"Top Right: {item.top_right}");
                _lines.Add($"Top Right: {item.top_right}");

                Debug.Log($"Bottom Right: {item.bottom_right}");
                _lines.Add($"Bottom Right: {item.bottom_right}");

                Debug.Log($"Bottom Left: {item.bottom_left}");
                _lines.Add($"Bottom Left: {item.bottom_left}");
            }
        }

        public void UpdateCurrentItemOnCursorMovement(EventSystem m_EventSystem,
    string target, Vector3[] corners)
        {

            string info = m_EventSystem.currentInputModule.ToString();

            int start = info.IndexOf(target) + target.Length;
            string name = "";

            for (int i = start; ; i++)
            {
                if (info[i] != ' ' && info[i] != '<')
                    name += info[i];
                else
                    break;
            }

            /*3- top left, 2- bottom left, 1- bottom right, 0- top right*/
            int index = _OnScreenItems.FindIndex(item =>
            {
                return item.name == name &&
                       item.bottom_left.ToString() == corners[2].ToString() &&
                       item.bottom_right.ToString() == corners[1].ToString() &&
                       item.top_right.ToString() == corners[0].ToString() &&
                       item.top_left.ToString() == corners[3].ToString();
            });

            /*Plugin.logger.Debug("Index is:" + index + " " + name);*/

            if (index != -1)
                _CurrentItemIndex = index;
        }

        public void DetectHitInGameMenu(string target)
        {
            EventSystem m_EventSystem = EventSystem.current;
            if (m_EventSystem == null)
                return;
            ReadOutCurrentMenuItemUnderCursor(m_EventSystem, target);

            Vector3 bottom_left = Utility.ExtractPointInVector3Form(m_EventSystem, "<b>Bottom-Left</b>: ");
            Vector3 bottom_right = Utility.ExtractPointInVector3Form(m_EventSystem, "<b>Bottom-Right</b>: ");
            Vector3 top_right = Utility.ExtractPointInVector3Form(m_EventSystem, "<b>Top-Right</b>: ");
            Vector3 top_left = Utility.ExtractPointInVector3Form(m_EventSystem, "<b>Top-Left</b>: ");


            if (
                Utility.AllComponentsAreNegativeOne(bottom_left) ||
                Utility.AllComponentsAreNegativeOne(bottom_right) ||
                Utility.AllComponentsAreNegativeOne(top_right) ||
                Utility.AllComponentsAreNegativeOne(top_left)
                )
            {
                return;
            }

            Vector3[] corners = new Vector3[4];

            corners[0] = bottom_left;
            corners[1] = bottom_right;
            corners[2] = top_right;
            corners[3] = top_left;


            if (_RectangleObject == null)
            {
                InitializeHighlightRectangle();
                DrawHighlightRectangle(corners);
                UpdateCurrentItemOnCursorMovement(m_EventSystem, target, corners);
            }
            else
            {
                DrawHighlightRectangle(corners);
                UpdateCurrentItemOnCursorMovement(m_EventSystem, target, corners);
            }
        }

        public void DetectAndReadClosestObstaclePosition()
        {

        }

        public void ReadOutCurrentMenuItemUnderCursor(EventSystem m_EventSystem, string target)
        {
            string info = m_EventSystem.currentInputModule.ToString();

            if (!info.Contains(target))
                return;

            int start = info.IndexOf(target) + target.Length;
            string name = "";

            for (int i = start; ; i++)
            {
                if (info[i] != ' ' && info[i] != '<')
                    name += info[i];
                else
                    break;
            }

            if (name != "Wrapper" && name != "Text")
            {
                if (name != _LastObjectName)
                {
                    _LastObjectName = name;

                    CreateHapticImpulseRightController(2);

                    if (_ReadOutActualCursorItemFlag)
                    {
                        AddToCsv(MakeStringFromAction("Cursor Moved to New Item", "Item Under Current Cursor on TTS."));
                        Speak(name);
                    }
                    PrintEverything(info);
                }
            }
            else
            {
                DetectHitInGameMenu("<b>parent</b>: ");
            }
        }
    }
}
