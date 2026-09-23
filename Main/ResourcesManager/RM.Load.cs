using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace UnityInterface
{
    public interface IAssetLoader<out T> where T : Object
    {
        T Load(string path);
    }
    public static partial class ResourcesManager
    {
        public static T GetMetadata<T>(this string pathBase, T defualt) where T : class
        {
            if (Path.GetExtension(pathBase) == ".meta")
            {
                return null;
            }
            string metaPath = pathBase + ".meta";
            T metadata0 = null;
            if (File.Exists(metaPath))
            {
                metadata0 = FromJson<T>(File.ReadAllText(metaPath));
            }
            if (metadata0 == null)
            {
                File.WriteAllText(metaPath, ToJson(defualt));
            }
            metadata0 = JsonUtility.FromJson<T>(File.ReadAllText(metaPath));
            return metadata0;
        }

        internal static CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider();
        internal static CompilerParameters compilerParameters = new CompilerParameters()
        {
            GenerateInMemory = true
        };
        public static Type LoadComponent<T>(string scriptContent) where T : Component => LoadCodes(scriptContent).GetTypes().Where(a => typeof(T).IsAssignableFrom(a)).FirstOrDefault();
        public static Assembly LoadCodes(string scriptContent)
        {
            var c = cSharpCodeProvider.CompileAssemblyFromSource(compilerParameters, scriptContent);
            if (c.Errors.HasErrors)
            {
                Debug.LogError(c.Errors.ToString());
                return null;
            }
            var a = c.CompiledAssembly;
            try
            {
                compilerParameters.ReferencedAssemblies.Add(a.Location);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            return a;
        }

        public static Texture2D GetTexture2DFromPathSimple(string path)
        {
            Texture2D t = new Texture2D(1, 1);
            t.name = Path.GetFileNameWithoutExtension(path);
            if (t.LoadImage(File.ReadAllBytes(path)))
            {
                return t;
            }
            Debug.LogWarning("Could not Get texture from path");
            return null;
        }
        public static Sprite Texture2DToSpriteSimple(Texture2D texture)
        {
            Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            s.name = texture.name;
            return s;
        }
        public static AudioClip GetAudioClipFromPath(string path, AudioType type = AudioType.UNKNOWN)
        {
            AudioClip clip;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(path, type))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone) { }
                clip = DownloadHandlerAudioClip.GetContent(request);
            }
            clip.name = Path.GetFileNameWithoutExtension(path);
            return clip;
        }
        public static Mesh GetMeshFromPath(string path)
        {
            Mesh mesh = JsonConvert.DeserializeObject<Mesh>(File.ReadAllText(path));
            mesh.name = Path.GetFileNameWithoutExtension(path);
            return mesh;
        }
        public static AssetBundle GetAssetBundleFromPath(string path)
        {
            if (Path.GetExtension(path) == ".manifest")
            {
                return null;
            }

            AssetBundle ab = AssetBundle.LoadFromFile(path);
            foreach (var item in ab.LoadAllAssets())
            {
                Add(item);
            }
            return ab;
        }
    }
}