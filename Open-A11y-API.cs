using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomPlugin
{
    public interface OpenAccAPI
    {
        GameObject GetGameObjectByPoint2D(float x, float y);
        GameObject GetGameObjectByPoint3D(float x, float y, float z);

        // GameObject GetGameObjectByPoint3DCV(float x, float y, float z);

        Scene GetCurrentScene();

        // Should come from an LM (llama-3)
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
    }
}
