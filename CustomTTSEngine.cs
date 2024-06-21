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


        public void InitializeSpeech()
        {
            if (TheVoice == null)
            {
                TheVoice = this;
                initSpeech();
            }
        }

        void Start()
        {
            // this will be called in the generic plugin for any game
            // Put initialization codes here if needed
        }

        void Update()
        {
            // this will be called in the generic plugin for any game
            // Put update codes here if needed
        }

        public void DestroySpeech()
        {
            if (TheVoice == this)
            {
                destroySpeech();
                TheVoice = null;
            }
        }

        public void Speak(string msg, float delay = 0f)
        {
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
