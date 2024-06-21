using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomPlugin
{
    public static class Utility
    {
        public static Coroutine ExecuteLater(this MonoBehaviour behaviour, float delay, System.Action fn)
        {
            return behaviour.StartCoroutine(_realExecute(delay, fn));
        }
        static IEnumerator _realExecute(float delay, System.Action fn)
        {
            yield return new WaitForSeconds(delay);
            fn();
        }


        public static Vector2 GetVector2FromString(string originalString)
        {
            originalString = TrimFirstBrackets(originalString);
            string[] floatStrings = originalString.Split(new string[] { ", " },
                StringSplitOptions.RemoveEmptyEntries);

            float float1 = float.Parse(floatStrings[0]);
            float float2 = float.Parse(floatStrings[1]);

            return new Vector2(float1, float2);
        }


        public static Vector3 GetVector3FromString(string originalString)
        {
            originalString = TrimFirstBrackets(originalString);
            string[] floatStrings = originalString.Split(new string[] { ", " },
                StringSplitOptions.RemoveEmptyEntries);

            float float1 = float.Parse(floatStrings[0]);
            float float2 = float.Parse(floatStrings[1]);
            float float3 = float.Parse(floatStrings[2]);

            return new Vector3(float1, float2, float3);
        }


        public static string TrimFirstBrackets(string input)
        {
            if (input.StartsWith("(") && input.EndsWith(")"))
                return input.Substring(1, input.Length - 2);
            else
                return input;
        }


        public static Vector2 ExtractPointInVector2Form(EventSystem m_EventSystem, string target)
        {
            string info = m_EventSystem.currentInputModule.ToString();

            if (!info.Contains(target))
                return new Vector2(-1f, -1f);

            int start = info.IndexOf(target) + target.Length + 1;
            string name = "";

            for (int i = start; ; i++)
            {
                if (info[i] != '<' && info[i] != ')')
                    name += info[i];
                else
                    break;
            }

            Vector2 vector = GetVector2FromString(name);

            return vector;
        }


        public static float ExtractSingleValue(EventSystem m_EventSystem, string target)
        {
            string info = m_EventSystem.currentInputModule.ToString();

            if (!info.Contains(target))
                return -1f;


            int start = info.IndexOf(target) + target.Length;
            string name = "";

            for (int i = start; ; i++)
            {
                if (char.IsDigit(info[i]))
                    name += info[i];
                else
                    break;
            }
            return float.Parse(name);
        }

        public static Vector3 ExtractPointInVector3Form(EventSystem m_EventSystem, string target)
        {
            string info = m_EventSystem.currentInputModule.ToString();

            if (!info.Contains(target))
                return new Vector3(-1f, -1f, -1f);

            int start = info.IndexOf(target) + target.Length + 1;
            string name = "";

            for (int i = start; ; i++)
            {
                if (info[i] != '<' && info[i] != ')')
                    name += info[i];
                else
                    break;
            }

            Vector3 vector = GetVector3FromString(name);

            return vector;
        }

        public static bool AllComponentsAreNegativeOne(Vector3 vector)
        {
            return vector.x == -1f && vector.y == -1f && vector.z == -1f;
        }


    }





}
