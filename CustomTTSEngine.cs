using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


namespace CustomPlugin
{

    public class CustomTTSEngine: MonoBehaviour
    {
        [DllImport("WindowsVoice")]
        public static extern void initSpeech();
        [DllImport("WindowsVoice")]
        public static extern void destroySpeech();
        [DllImport("WindowsVoice")]
        public static extern void addToSpeechQueue(string s);
        [DllImport("WindowsVoice")]
        public static extern void clearSpeechQueue();
        [DllImport("WindowsVoice")]
        public static extern void statusMessage(StringBuilder str, int length);
        public static CustomTTSEngine TheVoice = null;

        protected string _logPath = "log.txt";
        protected static List<string> _lines = new List<string>();


        protected void CustomOnEnable()
        {
            if (TheVoice == null)
            {
                TheVoice = this;
                Debug.Log("Initializing speech");
                _lines.Add("Initializing speech");
                initSpeech();
                Debug.Log("Initializing speech done");
                _lines.Add("Initializing speech done");
            }
        }

        protected void CustomStart()
        {
            // this will be called in the generic plugin for any game
            // Put initialization codes here if needed
        }

        protected void CustomUpdate()
        {
            // this will be called in the generic plugin for any game
            // Put update codes here if needed
        }

        protected void CustomOnDestroy()
        {
            if (TheVoice == this)
            {
                Debug.Log("Destroying speech");
                _lines.Add("Destroying speech");
                destroySpeech();
                Debug.Log("Speech destroyed");
                _lines.Add("Speech destroyed");
                TheVoice = null;
            }
        }

        public static void Speak(string msg, float delay = 0f)
        {
            Debug.Log("inside speak with msg:" + msg);
            _lines.Add("inside speak with msg:" + msg);

            clearSpeechQueue();
            if (delay == 0f)
                addToSpeechQueue(msg);
            else
                TheVoice.ExecuteLater(delay, () => Speak(msg));
        }

        public static string GetStatusMessage()
        {
            StringBuilder sb = new StringBuilder(40);
            statusMessage(sb, 40);
            return sb.ToString();
        }
    }
}
