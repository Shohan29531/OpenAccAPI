using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CustomPlugin
{
    public interface OpenAccAPI
    {
        GameObject GetGameObjectByPoint2D(float x, float y);
        GameObject GetGameObjectByPoint3D(float x, float y, float z);

        Scene GetCurrentScene();

        string DescribeScene(Scene scene);

        GameObject GetRootGameObject();
        GameObject GetLastSelectedGameObject();
        GameObject GetGameObjectByName(string name);

        bool DetectJoystickRightOrDownMovement();
        bool DetectJoystickLeftOrUpMovement();
        bool DetectJoystickButtonPress();
        bool DetectController_A_ButtonPress();
        bool DetectController_B_ButtonPress();
        bool DetectGripTriggerPress();
        bool DetectIndexTriggerPress();
        bool DetectLeftControllerTimeout();
        bool DetectRightControllerTimeout();

        void CreateHapticImpulseRightController(int strength);
        void CreateHapticImpulseLeftController(int strength);

        void ReadOutCurrentCursorOrientation();
        void SimulateClickEvent();

        void GameSpecificAccessibilityPatch();

        bool HasSceneChanged();

        string GetGameObjectCoordinates(GameObject obj);

        List<GameObject> GetAllGameObjectsFromCurrentScene();

        void HandlePointerEnter(GameObject go, PointerEventData eventData);
    }
}
