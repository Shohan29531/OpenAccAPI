# OpenAccAPI: Accessibility API for VR and 3D Applications

OpenAccAPI is an open-source accessibility API designed to enhance the accessibility of Virtual Reality (VR) and 3D applications for blind and low-vision (BLV) users. The project includes a reference implementation, **OpenAccGenericPlugin**, specifically for Unity-based applications. With features like text-to-speech (TTS), haptic feedback, and alternative navigation options, OpenAccAPI bridges the gap between immersive environments and assistive technologies.

---

## Key Features

1. **Text-to-Speech (TTS):** Provides audio feedback for UI elements and in-game objects.
2. **Event-Based Haptics:** Enables tactile feedback for enhanced spatial awareness.
3. **Menu Text Highlighting:** Makes navigation easier for low-vision users by visually enhancing focused menu items.
4. **Cursor Position Feedback:** Reports cursor location as percentages or VR controller angles to aid navigation.
5. **Alternative Navigation Options:** Supports joystick or keyboard navigation for simplified interactions.
6. **Game Agnostic:** Integrates into any Unity-based game with minimal effort.

---

## Installation
- clone the repo
- cd OpenAccAPI
- Modify the plugin opening CusomPlugin.sln in Microsoft Visual Studio
- copy the bin/x64/Debug/OpenACCGenericPlugin.dll to your game's root directory's plugin folder
- make sure the game is IPA patched and ready to accept plugins
