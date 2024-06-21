using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Speech.Synthesis;
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
    class GameXPlugin : OpenAccGenericPlugin
    {
        private string _GameName = "X";

        void OnEnable()
        {

        }

        void Start()
        {

        }

        void OnDestroy() 
        {

        }

        void Update()
        {
        
        }

        public override void GameSpecificAccessibilityPatch()
        {
            // Apply game specific patch
        }

    }
}
