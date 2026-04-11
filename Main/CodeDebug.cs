using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityInterface;

namespace UnityInterface
{
    internal static class DEBUGGER
    {
        public static Vector2 arrowDelta_XY
        {
            get
            {
                Vector2 ret = Vector2.zero;
                ret.y = Input.GetKey(KeyCode.UpArrow) ? 1 : Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                ret.x = Input.GetKey(KeyCode.LeftArrow) ? 1 : Input.GetKey(KeyCode.RightArrow) ? -1 : 0;
                return ret;
            }
        }
        public static void StartTest()
        {
            if (false)
            {
                Debug.Log("Set N Enum...");
                KeyCode k = "ANY".ToEnum<KeyCode>();
                Debug.Log($"Done. Result:{k == "ANY".ToEnum<KeyCode>()}");

                Debug.Log("Create sample array...");
                int[] orig = Range(3);
                PrintList(orig);

                Debug.Log("Add extra-stuff to array...");
                int[] origE = Range(6);
                PrintList(origE);

                Debug.Log("Summing them... Result:");
                PrintList(orig.AddAs(origE));
            }
            Debug.Log("All Done...");
        }
        static int[] Range(int stop)
        {
            int[] ary = new int[stop];
            for (int i = 0; i < stop; i++)
            {
                ary[i] = i;
            }
            return ary;
        }
        static void PrintList(int[] a)
        {
            foreach (var item in a)
            {
                Debug.Log(item);
            }
        }
    }
}
/// <summary>
/// Summary TST!
/// </summary>
public class DEBUGGER_TRANSFORM_RECT : MonoBehaviour
{
    /// <summary>
    /// Summary TST #0
    /// </summary>
    public void Method()
    {
    }
}
