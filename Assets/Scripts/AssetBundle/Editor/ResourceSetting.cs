using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

namespace Virivers
{
    public class ResourceSetting
    {
        // 资源储存位置的相对路径
        public static string PATH = "Test/Assets/";

        // 目录是否存在
        public static void CheckAssetPath()
        {
            if (!Directory.Exists(Application.dataPath + "/" + PATH))
            {
                Directory.CreateDirectory(Application.dataPath + "/" + PATH);
            }
        }

        // 目录是否存在
        public static void CheckAssetPath(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }


        // 获得资源储存路径地址
        public static string GetSetAssetPath(string setName)
        {
            
            return "Assets/" + PATH + setName + ".asset";
        }

        // 保存文件的地址
        public static string SavePath(string setName)
        {
            return PATH + setName + ".asset";
        }
    }
}
