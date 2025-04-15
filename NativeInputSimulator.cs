using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing;
public static class NativeInputSimulator
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;

    private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const int MOUSEEVENTF_LEFTUP = 0x0004;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const int MOUSEEVENTF_MIDDLEUP = 0x0040;

    private static HashSet<byte> keysHeld = new HashSet<byte>();

    public static void SimulateKeyDown(byte virtualKey)
    {
        if (!keysHeld.Contains(virtualKey))
        {
            keybd_event(virtualKey, 0, KEYEVENTF_KEYDOWN, 0);
            keysHeld.Add(virtualKey);
        }
    }

    public static void SimulateKeyUp(byte virtualKey)
    {
        if (keysHeld.Contains(virtualKey))
        {
            keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, 0);
            keysHeld.Remove(virtualKey);
        }
    }

    public static void SimulateKeyPress(byte virtualKey)
    {
        keybd_event(virtualKey, 0, KEYEVENTF_KEYDOWN, 0);
        keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, 0);
    }

    public static void ReleaseAllHeldKeys()
    {
        foreach (var key in keysHeld)
        {
            keybd_event(key, 0, KEYEVENTF_KEYUP, 0);
        }
        keysHeld.Clear();
    }

    public static void SimulateMouseClick(int button = 0)
    {
        switch (button)
        {
            case 0: // Left click
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                break;
            case 1: // Right click
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                break;
            case 2: // Middle click
                mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                break;
        }
    }

    public static void SimulateMouseDown(int button = 0)
    {
        switch (button)
        {
            case 0:
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                break;
            case 1:
                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                break;
            case 2:
                mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                break;
        }
    }

    public static void SimulateMouseUp(int button = 0)
    {
        switch (button)
        {
            case 0:
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                break;
            case 1:
                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                break;
            case 2:
                mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                break;
        }
    }

}
