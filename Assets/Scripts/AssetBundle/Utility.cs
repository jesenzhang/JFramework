using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace Virivers
{
     
    public class Utility {
        
        public static string GetStreamingAssetsPath ()
        {
            if (Application.isEditor)
            {
                return Application.streamingAssetsPath;
            }
            else if (Application.isMobilePlatform || Application.isConsolePlatform)
            {
                return Application.streamingAssetsPath;
            }
            else
            {
                // For standalone player.
                return "file://" + Application.streamingAssetsPath;
            }
        }

        public static string GetPlatformName()
        {
            #if UNITY_EDITOR
            return GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
            #else
            return GetPlatformName(Application.platform);
            #endif
        }

        /// <summary>
        /// cdn url
        /// TODO
        /// </summary>
        /// <returns>The downloading UR.</returns>
        public static string BaseDownloadingURL() {
            return "http://127.0.0.1/";
        }

        /// <summary>
        /// 保存路径
        /// </summary>
        /// <returns>The path.</returns>
        public static string PersistentPath() {
            return Path.Combine(Application.persistentDataPath, GetPlatformName());
        }

        public static string GetPlatformName(RuntimePlatform rp)
        {
            switch (rp)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.PS4:
                    return "PlayStation";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
            }
            return "";
        }

#if UNITY_EDITOR
        public static string GetPlatformForAssetBundles(BuildTarget target)
        {
            switch(target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                case BuildTarget.PS4:
                    return "PlayStation";
                default:
                    return null;
            }
        }
#endif
    }
}
