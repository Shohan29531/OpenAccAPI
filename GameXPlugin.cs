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
            string objectName = "BigCuttable";
            GameObject[] foundObjects = FindObjectsOfType<GameObject>()
                .Where(obj => obj.name == objectName).ToArray();
            GameObject closestObstacle = null;

            foreach (GameObject obj in foundObjects) { 
                if (obj.transform.position.z < 0) continue;
                if (closestObstacle == null) closestObstacle = obj;
                else { 
                    if ((obj.transform.position.z < 
                        closestObstacle.transform.position.z))
                        closestObstacle = obj;
                }
            }
            if (closestObstacle != null)
            {
                if (closestObstacle.transform.position.x >= 0) {
                    if (closestObstacle.transform.position.x == 0.3f) 
                        TTSEngine.Speak("Near Right.");
                    else TTSEngine.Speak("Far Right.");
                }
                else {
                    if (closestObstacle.transform.position.x == -0.3f)  
                        TTSEngine.Speak("Near Left.");
                    else TTSEngine.Speak("Far Left.");
                }
            }
        }
    }
}
