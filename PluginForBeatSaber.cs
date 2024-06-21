using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using VRUIControls;

namespace CustomPlugin
{
    class PluginForBeatSaber : OpenAccAPIDefautImplementation
    {
        private string _GameName = "Beat Saber";

        private List<OnScreenItem> _OnScreenItems;
        private List<OnScreenItem> _PersistentOnScreenItems;
        private int _CurrentItemIndex = 0;

        private float _InGamePlayElapseTime = 0f;
        private float _InGamePlayTTSInterval = 0.5f;


        void OnEnable()
        {
            CustomOnEnable();
            _OnScreenItems = new List<OnScreenItem>();
        }

        void Start()
        {
            CustomStart();
        }

        void Update()
        {
            CustomUpdate();
            if (DetectGripTriggerPress())
            {
                if (_OnScreenItems != null && _OnScreenItems.Count > 1)
                {
                    if (_CursorMovementLocked)
                    {
                        _CursorMovementLocked = false;
                        VRPointer vrPointer = FindObjectOfType<VRPointer>();
                        if (vrPointer != null)
                        {
                            vrPointer.Awake();
                            vrPointer.OnEnable();
                        }
                        Speak("Cursor Enabled.");
                        CreateHapticImpulseRightController(5);
                        AddToCsv(MakeStringFromAction("Grip Trigger Press", "Cursor Enabled."));
                    }
                    else
                    {
                        _CursorMovementLocked = true;
                        VRPointer vrPointer = FindObjectOfType<VRPointer>();
                        if (vrPointer != null)
                        {
                            vrPointer.OnDisable();
                        }
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
                    VRPointer vrPointer = FindObjectOfType<VRPointer>();
                    if (vrPointer != null)
                    {
                        vrPointer.Awake();
                        vrPointer.OnEnable();
                    }
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
            }

        }

        void OnDestroy()
        {
            CustomOnDestroy();
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


        // Method to extract all the current screen's selectable items
        // from the VRInputModule's toString() method
        public void FindAllSelectableScreenItems()
        {
            if (EventSystem.current == null)
                return;

            _OnScreenItems = null;

            float floatcount = Utility.ExtractSingleValue(EventSystem.current,
                "<b>Current Screen Item Count</b>: ");

            if (floatcount == -1f)
                return;

            int count = (int)floatcount;

            for (int i = 0; i < count; i++)
            {
                string target = "<b>Current Screen Item-" + i + "</b>: ";

                // all info
                string screen_item_string = FindSelectableScreenItem(EventSystem.current,
                    target);

                // the name and then the four coordinates
                string[] screen_item_details = screen_item_string.Split(';');

                // the name is first
                string name = screen_item_details[0];

                Vector3 top_left = Utility.GetVector3FromString(screen_item_details[1]);
                Vector3 bottom_left = Utility.GetVector3FromString(screen_item_details[2]);
                Vector3 bottom_right = Utility.GetVector3FromString(screen_item_details[3]);
                Vector3 top_right = Utility.GetVector3FromString(screen_item_details[4]);

                OnScreenItem onScreenItem = new OnScreenItem(name, top_left, top_right,
                    bottom_right, bottom_left);

                if (_OnScreenItems == null)
                {
                    _OnScreenItems = new List<OnScreenItem>();
                }
                _OnScreenItems.Add(onScreenItem);
            }
            // sorting based on the positions of the menu items
            _OnScreenItems.Sort(new OnScreenItemComparer());
        }

        // given the target's name with it's seqeuence number, locate the target in the whole string
        public string FindSelectableScreenItem(EventSystem m_EventSystem, string target)
        {
            string info = m_EventSystem.currentInputModule.ToString();

            int start = info.IndexOf(target) + target.Length;
            string name = "";

            int semicolon_count = 0;

            for (int i = start; ; i++)
            {
                if (info[i] == ';')
                {
                    semicolon_count++;
                    if (semicolon_count == 5)
                        break;
                }
                name += info[i];
            }
            return name;
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
            string objectName = "BigCuttable";

            GameObject[] foundObjects = FindObjectsOfType<GameObject>()
                .Where(obj => obj.name == objectName).ToArray();

            GameObject closestObstacle = null;

            foreach (GameObject obj in foundObjects)
            {
                if (obj.transform.position.z < 0) continue;

                if (closestObstacle == null)
                {
                    closestObstacle = obj;
                }
                else
                {
                    if ((obj.transform.position.z < closestObstacle.transform.position.z))
                        closestObstacle = obj;
                }
            }

            if (closestObstacle != null)
            {
                if (closestObstacle.transform.position.x >= 0)
                {
                    if (closestObstacle.transform.position.x == 0.3f)
                    {
                        Speak("Near Right.");
                        CreateHapticImpulseRightController(5);
                    }
                    else
                    {
                        Speak("Far Right.");
                        CreateHapticImpulseRightController(2);
                    }
                }
                else
                {
                    if (closestObstacle.transform.position.x == -0.3f)
                    {
                        Speak("Near Left.");
                        CreateHapticImpulseLeftController(5);
                    }
                    else
                    {
                        Speak("Far Left.");
                        CreateHapticImpulseLeftController(2);
                    }
                }
            }
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
