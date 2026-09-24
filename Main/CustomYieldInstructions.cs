using UnityEngine;

namespace UnityInterface
{
    public class YieldInstructionSingleton<T> : CustomYieldInstruction where T : CustomYieldInstruction, new()
    {
        protected static T instance;
        public static T Instance => instance != null ? instance : (instance = new T());
        public override bool keepWaiting => false;
    }
    /// <summary>
    /// If you have toggle done as true! Or else it still waiting!
    /// </summary>
    public class WaitForBuiltInResource : YieldInstructionSingleton<WaitForBuiltInResource>
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
    public class WaitForPremadeResource : YieldInstructionSingleton<WaitForPremadeResource>
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
}