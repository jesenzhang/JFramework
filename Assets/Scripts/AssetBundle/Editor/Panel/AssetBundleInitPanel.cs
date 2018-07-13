using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace Virivers
{
    /**
     * 初始化界面
     * */
    public class AssetBundleInitPanel : ViewAbstract
    {

        new public AssetBundlePlugsEditor Parent
        {
            set { parent = value; }
            get { return (AssetBundlePlugsEditor)parent; } 
        }

        private string bundleName;

        public override void Drawing()
        {
            if (Parent.data == null)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("AssetBundle Manager");
                bundleName = EditorGUILayout.TextField("BundleName:", bundleName);
                
                if (GUILayout.Button("Create"))
                {
                    if (bundleName==null || bundleName == "")
                    {
                        EditorUtility.DisplayDialog("Error","Input BundleName","ok");
                        EditorGUILayout.EndVertical();
                        return;
                    }
                    if (bundleName != "")
                    {
                        if (!Regex.IsMatch(bundleName, @"^[0-9a-zA-Z_]*$"))
                        {
                            EditorUtility.DisplayDialog("Error", "Input BundleName Can Only Include a-z A-Z 0-9 and _", "ok");
                            EditorGUILayout.EndVertical();
                            return;
                        }
                    }
                    // 创建
                    if (EditorUtility.DisplayDialog("Warning", "will Create AssetBundle Name is " + bundleName, "ok", "no"))
                    {
                        AssetBundleData data = new AssetBundleData();
                        data.BundleName = bundleName;
                        ResourceSetting.CheckAssetPath();
                        AssetDatabase.CreateAsset(data, ResourceSetting.GetSetAssetPath(data.BundleName));
                        AssetDatabase.Refresh();
                        Parent.data = data;
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath(ResourceSetting.SavePath(Parent.data.BundleName), typeof(UnityEngine.Object));
                        bundleName = null;
                    }
                }
                if (GUILayout.Button("Import"))
                {
                    // 导入
                    string path = EditorUtility.OpenFilePanel("Load AssetBundleData", Application.dataPath + "/" + ResourceSetting.PATH, "asset");
                    if (path.Length != 0)
                    {
                        path = "Assets" + path.Replace(Application.dataPath, "");

                        Parent.data = AssetDatabase.LoadAssetAtPath(path, typeof(AssetBundleData)) as AssetBundleData;
                    }
                }
                
                EditorGUILayout.EndVertical();
                return;
            }
        }
    }
}