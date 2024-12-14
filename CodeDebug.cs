using UnityEngine;

namespace UnityInterface
{
    public static class DebugController
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
        public static void Print(Object o)
        {
            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(o));
        }
    }
}

