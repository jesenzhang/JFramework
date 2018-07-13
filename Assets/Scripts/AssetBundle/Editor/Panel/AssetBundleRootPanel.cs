using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditorInternal;

namespace Virivers
{
    /**
     * 名字配置界面
     * */
    public class AssetBundleRootPanel : ViewAbstract
    {
        new public AssetBundlePlugsEditor Parent
        {
            set { parent = value; }
            get { return (AssetBundlePlugsEditor)parent; }
        }

        private string bundleName;
        private string outPutPath;
        private Vector2 nameScrollPos;
        private Vector2 variantScrollPos;
        private ReorderableList nameList;
        private ReorderableList variantList;
        private int nameSelected;
        private int variantSelected;
        private AssetBundleEditor window;

        public override void Drawing()
        {
            if (Parent.data == null)
                return;
                // 
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("AssetBundle Manager");
            if (bundleName == null)
                bundleName = Parent.data.BundleName;
            if (outPutPath == null || outPutPath == "")
                outPutPath = Parent.data.OutPutPath;
            // 设置保存文件名字
            bundleName = EditorGUILayout.TextField("BundleName:", bundleName);
            // 设置输出目录
            outPutPath = EditorGUILayout.TextField("outPutPath:", outPutPath);

            // 绘制编译参数选择
            var optionvalues = Enum.GetValues(typeof(BuildAssetBundleOptions));
            int optiontmp = 0;
            for (int i = 0; i < optionvalues.Length; ++i)
            {
                if (((int)Parent.data.assetBundleOptions & (int)optionvalues.GetValue(i)) != 0)
                    optiontmp |= (1 << i);
            }
            BuildAssetBundleOptions option = (BuildAssetBundleOptions)EditorGUILayout.EnumMaskField("BuildAssetBundleOptions", (BuildAssetBundleOptions)optiontmp);
            optiontmp = 0;
            for (int i = 0; i < optionvalues.Length; ++i)
            {
                if (((int)option & (1 << i)) != 0)
                    optiontmp |= (int)optionvalues.GetValue(i);
            }
            Parent.data.assetBundleOptions = (BuildAssetBundleOptions)optiontmp;

            // 选择输出平台
            Parent.data.targetPlatform = (BuildTarget)EditorGUILayout.EnumPopup("Platform", Parent.data.targetPlatform);

            // 渲染一个可滚动的List
            nameScrollPos = EditorGUILayout.BeginScrollView(nameScrollPos, GUILayout.Width(Parent.position.width - 12), GUILayout.Height(150));
            // assetbundle列表
            if (nameList == null)
            {
                // 加入数据数组
                nameList = new ReorderableList(Parent.data.AssetBunldeName, typeof(string[]), false, false, true, true);
            }

            // 绘制Item显示列表
            nameList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                string element = nameList.list[index] as string;
                rect.y += 2;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), "Name:");
                nameList.list[index] = EditorGUI.TextField(new Rect(rect.x + 45, rect.y, rect.width - 45, EditorGUIUtility.singleLineHeight), element).ToLower();
            };
            // 绘制表头
            nameList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "AssetBundleList");
            };
            // 选择回调
            nameList.onSelectCallback = (ReorderableList l) =>
            {
                nameSelected = l.index;
            };
            nameList.onRemoveCallback = (ReorderableList l) => {
                l.list.RemoveAt(nameSelected);
                nameSelected = 0;
            };
            nameList.onAddCallback = (ReorderableList l) => {
                l.list.Add("");
            };

            //list.elementHeight = 60;
            nameList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            checkName();

            // 渲染一个可滚动的List
            variantScrollPos = EditorGUILayout.BeginScrollView(variantScrollPos, GUILayout.Width(Parent.position.width - 12), GUILayout.Height(110));
            // assetbundle列表
            if (variantList == null)
            {
                // 加入数据数组
                variantList = new ReorderableList(Parent.data.Variants, typeof(string[]), false, false, true, true);
            }
            // 绘制Item显示列表
            variantList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                string element = variantList.list[index] as string;
                rect.y += 2;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), "Name:");
                variantList.list[index] = EditorGUI.TextField(new Rect(rect.x + 45, rect.y, rect.width - 45, EditorGUIUtility.singleLineHeight), element).ToLower();
            };
            // 绘制表头
            variantList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Variant List");
            };
            // 选择回调
            variantList.onSelectCallback = (ReorderableList l) =>
            {
                variantSelected = l.index;
            };
            variantList.onRemoveCallback = (ReorderableList l) => {
                if (variantSelected == 0)
                    return;
                l.list.RemoveAt(variantSelected);
                nameSelected = 0;
            };
            variantList.onAddCallback = (ReorderableList l) => {
                l.list.Add("");
            };
            //list.elementHeight = 60;
            variantList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            checkVariant();

            // 保存配置按钮
            if (GUILayout.Button("Save As"))
            {
                if(checkName() || checkVariant())
                {
                    EditorGUILayout.EndVertical();
                    return;
                }
                if (EditorUtility.DisplayDialog("Warning", "will save AssetBundle SettingData is " + bundleName, "ok", "no"))
                {
                    showWindow(true);
                    // 将当前文件保存到指定目录
                    AssetBundleData data = Parent.data.Clone();
                    if (data.BundleName != bundleName)
                    {
                        data.BundleName = bundleName;
                    }
                    if (data.OutPutPath != outPutPath)
                    {
                        data.OutPutPath = outPutPath;
                    }
                    AssetDatabase.CreateAsset(data, ResourceSetting.GetSetAssetPath(bundleName));
                    AssetDatabase.Refresh();
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath(ResourceSetting.SavePath(bundleName), typeof(UnityEngine.Object));
                    Parent.data = data;
                }
            }

            if (GUILayout.Button("Editor Assets"))
            {
                showWindow();
            }

            if (GUILayout.Button("Build"))
            {
                string path = Utility.GetPlatformForAssetBundles(Parent.data.targetPlatform);
                ResourceSetting.CheckAssetPath(Application.dataPath + "/" + Parent.data.OutPutPath + "/" + path);
               
                BuildPipeline.BuildAssetBundles(Application.dataPath + "/" + Parent.data.OutPutPath + "/" + path, Parent.data.assetBundleOptions, Parent.data.targetPlatform);
            }

            if (GUILayout.Button("Return Main"))
            {
                showWindow(true);
                bundleName = null;
                nameScrollPos = Vector2.zero;
                variantScrollPos = Vector2.zero;
                nameList = null;
                variantList = null;
                nameSelected = 0;
                variantSelected = 0;
                Parent.data = null;

            }

            EditorGUILayout.EndVertical();
        }

        private void showWindow(bool close = false)
        {
            if (window)
                window.Close();
            if (close == true)
                return;
            window = EditorWindow.GetWindow<AssetBundleEditor>("AssetBundleEditor");
            window.data = Parent.data;
            window.Reset();
            window.Show();
        }

        private bool checkVariant()
        {
            // 做提示错误 因为可能有名字相同的variant
            Dictionary<string, int> checkVariantDict = new Dictionary<string, int>();
            for (int i = 0; i < variantList.list.Count; i++)
            {
                if(variantList.list[i].ToString() == "")
                {
                    EditorGUILayout.HelpBox("variant not Null", MessageType.Error);
                    return true;
                }
                if (checkVariantDict.ContainsKey(variantList.list[i].ToString()) == true)
                {
                    EditorGUILayout.HelpBox("variant '" + variantList.list[i].ToString() + "' repeat", MessageType.Error);
                    return true;
                }
                checkVariantDict.Add(variantList.list[i].ToString(), 0);
            }
            return false;
        }

        private bool checkName()
        {
            // 做提示错误 因为可能有名字相同的AssetBundleName
            Dictionary<string, int> checkDict = new Dictionary<string, int>();
            for (int i = 0; i < nameList.list.Count; i++)
            {
                if (nameList.list[i].ToString() == "")
                {
                    EditorGUILayout.HelpBox("BundleName not Null", MessageType.Error);
                    return true;
                }
                if (checkDict.ContainsKey(nameList.list[i].ToString()) == true)
                {
                    EditorGUILayout.HelpBox("BundleName '" + nameList.list[i].ToString() + "' repeat", MessageType.Error);
                    return true;
                }
                checkDict.Add(nameList.list[i].ToString(), 0);
            }
            return false;
        }
    }
}
