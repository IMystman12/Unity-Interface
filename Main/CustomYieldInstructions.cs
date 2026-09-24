using UnityEngine;

namespace UnityInterface
{
    /// <summary>
    /// It's a good choice for saving memory!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class YieldInstructionSingleton<T> : CustomYieldInstruction where T : CustomYieldInstruction, new()
    {
        protected static T instance;
        public static T Instance => instance != null ? instance : (instance = new T());
        public override bool keepWaiting => false;
    }
    /// <summary>
    /// Reminding done is true! Or else it still waiting!
    /// </summary>
    public class WaitForBuiltInResource : YieldInstructionSingleton<WaitForBuiltInResource>
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
    /// <summary>
    /// Done will be true after all asset loaded!
    /// </summary>
    public class WaitForPremadeResource : YieldInstructionSingleton<WaitForPremadeResource>
    {
        public static bool done;
        public override bool keepWaiting => !done;
    }
}