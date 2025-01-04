using System.Collections;
using BepInEx;
using HarmonyLib;
namespace UnityInterface
{
    [BepInPlugin("imystman12.unity.interface", "Unity Interface", "1.0")]
    public class BasePlugin : BaseUnityPlugin
    {
        void Awake()
        {
            new Harmony("imystman12.unity.interface").PatchAll();
        }
    }
}