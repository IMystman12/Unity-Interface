
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityInterface.AssetLoaders
{
    [Serializable]
    public class Texture2DMetadata
    {
        public bool isReadable;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public FilterMode filterMode = FilterMode.Point;
        public int anisoLevel = 1;
        public Texture2D LoadAndApply(string path)
        {
            Texture2D t = new Texture2D(1, 1);
            t.name = Path.GetFileNameWithoutExtension(path);

            t.wrapMode = wrapMode;
            t.filterMode = filterMode;
            t.anisoLevel = anisoLevel;

            if (t.LoadImage(File.ReadAllBytes(path), !isReadable))
            {
                return t;
            }
            Debug.LogWarning("Could not Get texture from path");
            return null;
        }
    }

    [Serializable]
    public class SpriteMetadata : Texture2DMetadata
    {
        public float rectX = 0, rectY = 0, rectWidth, rectHeight, pivotX = 0.5f, pivotY = 0.5f, pixelsPerUnit = 100;
    }

    [Serializable]
    public class AssemblyObject : Object
    {
        public Assembly assembly;
    }

    public class Texture2DLoader : IAssetLoader<Texture2D>
    {
        public static Texture2DMetadata metadataEmpty = new Texture2DMetadata();
        public Texture2D Load(string path) => (Path.GetExtension(path) == ".meta") ? null : path.GetMetadata(metadataEmpty).LoadAndApply(path);
    }

    public class AudioClipLoader : IAssetLoader<AudioClip>
    {
        public AudioClip Load(string path) => ResourcesManager.GetAudioClipFromPath(path);
    }

    public class SpriteLoader : IAssetLoader<Sprite>
    {
        static SpriteMetadata metadataEmpty = new SpriteMetadata();
        public Sprite Load(string path)
        {
            if (Path.GetExtension(path) == ".meta")
            {
                return null;
            }
            string pathMeta = path + ".meta";
            Texture2D texture; Sprite s; SpriteMetadata metadata;
            if (!File.Exists(pathMeta))
            {
                texture = ResourcesManager.GetTexture2DFromPathSimple(path);

                metadata = new SpriteMetadata();
                metadata.rectX = 0;
                metadata.rectY = 0;
                metadata.rectWidth = texture.width;
                metadata.rectHeight = texture.height;
                metadata.pivotX = 0.5f;
                metadata.pivotY = 0.5f;
                metadata.pixelsPerUnit = 100;
            }
            else
            {
                metadata = path.GetMetadata(metadataEmpty);
                texture = metadata.LoadAndApply(path);
            }

            s = Sprite.Create(texture, new Rect(metadata.rectX, metadata.rectY, metadata.rectWidth, metadata.rectHeight), new Vector2(metadata.pivotX, metadata.pivotY), metadata.pixelsPerUnit);
            s.name = texture.name;
            return s;
        }
    }

    public class MeshLoader : IAssetLoader<Mesh>
    {
        public Mesh Load(string path) => ResourcesManager.GetMeshFromPath(path);
    }

    public class AssetBundleLoader : IAssetLoader<AssetBundle>
    {
        public AssetBundle Load(string path) => ResourcesManager.GetAssetBundleFromPath(path);
    }

    public class ScriptLoader : IAssetLoader<AssemblyObject>
    {
        public AssemblyObject Load(string path) => new AssemblyObject() { assembly = ResourcesManager.LoadCodes(path) };
    }
}
