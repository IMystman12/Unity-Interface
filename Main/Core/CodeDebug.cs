using Newtonsoft.Json;
using UnityEngine;
using static UnityInterface.UnityObject;

namespace UnityInterface
{
    public class DEBUGGER_TRANSFORM_RECT : MonoBehaviour
    {
        RectTransform rt => GetComponent<RectTransform>();
        public int moveDelta = 10;
        public void Update()
        {
            rt.anchoredPosition += moveDelta * DebugController.arrowDelta_XY;
        }
    }
    public class DEBUGGER_JSON_OBJ : MonoBehaviour
    {
        public string j;
        public Object o;
        public bool p;
        public void Update()
        {
            if (p)
            {
                p = false;
                j = JsonConvert.SerializeObject(o, new UnityObjectConverter());
                Debug.Log(j);
            }
        }
    }
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

