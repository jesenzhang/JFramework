using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public static class EditorUtils
{
    public static BuildAssetBundleOptions OPTION = BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression;
	public static string DataPath()
	{
		return "Export/" + UserName() + "/Data/";
	}

	public static string PlatformPath(BuildTarget buildTarget)
	{
		string platform = "";
		switch(buildTarget)
		{
			case BuildTarget.iOS:
				{
					platform = "iOS";
				}
				break;
			case BuildTarget.Android:
				{
					platform = "Android";
				}
				break;
			case BuildTarget.StandaloneWindows:
				{
					platform = "PC";
				}
				break;
			case BuildTarget.StandaloneWindows64:
				{
					platform = "PC";
				}
				break;
		}

		if(platform == "")
		{
			//Debug.LogError("platform is invaild->" + EditorUserBuildSettings.activeBuildTarget);
			Debug.LogError("platform is invaild->" + buildTarget);
			return null;
		}
		platform += "/";

        if (Application.dataPath.Contains("Client-UI"))
        {
            return "Export/" + platform;
        }
        else
        {
            string userName = System.Environment.UserName;

            return "Export/" + userName + "/" + platform;
        }
    }

	public static string UserName()
	{
		return System.Environment.UserName;
	}

	public static Object GetPrefab(GameObject go, string name)
	{
		string path = "Assets/temp/";
		if(!Directory.Exists(path))
			Directory.CreateDirectory(path);
		Object temp = PrefabUtility.CreateEmptyPrefab(path + name + ".prefab");
		temp = PrefabUtility.ReplacePrefab(go, temp);
		Object.DestroyImmediate(go);
		return temp;
	}

	public static string NameProcess(string v)
	{
		return Regex.Replace(v, @"\s", "");
	}

    public static void RenameFile(string path, string origin, string dest)
    {
        if (!File.Exists(path + origin))
            return;

        string tempPath = "Export/temp/";
        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        File.Copy(path + origin, tempPath + origin);
        File.Delete(path + origin);

        string dirName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(path));
        if (File.Exists(path + dirName))
        {
            File.Delete(path + dirName);
        }
        if (File.Exists(path + dirName + ".manifest"))
        {
            File.Delete(path + dirName + ".manifest");
        }
        if (File.Exists(path + origin + ".manifest"))
        {
            File.Delete(path + origin + ".manifest");
        }

        File.Move(tempPath + origin, path + dest);
    }

    public static string GetAssetBundleName(string objPath,string fileName)
    {
        if (fileName == Path.GetFileName(Path.GetDirectoryName(objPath)))
        {
            return fileName + "_FILE";
        }
        else
        {
            return fileName;
        }
    }
}
