using UnityEngine;
using System.Collections;
using System;
using UnityEditorInternal;
using UnityEditor;
using System.Collections.Generic;

namespace Virivers
{
    /**
     * 扩展面板的路径界面
     * */
    public class AssetBundlePlugsPathPanel : ViewAbstract
    {
        new public AssetBundleEditor Parent
        {
            set { parent = value; }
            get { return (AssetBundleEditor)parent; }
        }

        private AssetFileEditor window;

        public override void Drawing()
        {
            if (Parent.data == null)
                return;
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical("Box");
            Parent.regScrollPos = EditorGUILayout.BeginScrollView(Parent.regScrollPos, GUILayout.Width(Parent.position.width - 12), GUILayout.Height(200));
            // 没有初始化过list
            if (Parent.regsList == null)
            {
                // asset选择过，并且数据列表不为空
                if (Parent.list.index >= 0 && Parent.data.items.Count > 0)
                {
                    AssetsItem items = Parent.data.items[Parent.list.index];
                    if(items.paths.ContainsKey(Parent.data.Variants[items.VariantName]) == false)
                    {
                        items.paths.Add(Parent.data.Variants[items.VariantName], new PathList());
                    }

                    Parent.regsList = new ReorderableList(items.paths[Parent.data.Variants[items.VariantName]].psList, typeof(PathStruct), false, false, false, true);
                }
                else
                {
                    List<PathStruct> createList = new List<PathStruct>();
                    Parent.regsList = new ReorderableList(createList, typeof(PathStruct), false, false, false, true);
                }
            }
            // 绘制表头
            Parent.regsList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Assets Path");
            };
            // 渲染element
            Parent.regsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                PathStruct ps = Parent.regsList.list[index] as PathStruct;
                EditorGUI.LabelField(new Rect(rect.x, rect.y + 2, 40, EditorGUIUtility.singleLineHeight), "Path:");
                EditorGUI.TextField(new Rect(rect.x + 45, rect.y + 2, rect.width - 45 - 60, EditorGUIUtility.singleLineHeight), ps.path);
                ps.assetRegexType = (AssetRegexType)EditorGUI.EnumMaskField(new Rect(rect.width - 50, rect.y + 2, 60, EditorGUIUtility.singleLineHeight), (AssetRegexType)ps.assetRegexType);
            };
            // 删除
            Parent.regsList.onRemoveCallback = (ReorderableList l) => {
                l.list.RemoveAt(l.index);
                l.index = -1;
            };
            Parent.regsList.DoLayoutList();
            DropProc();

            EditorGUILayout.EndScrollView();
            //检查打包内容是否选的nothing
            for (int i = 0; i < Parent.regsList.list.Count; i++)
            {
                PathStruct ps = Parent.regsList.list[i] as PathStruct; ;

                if ((int)ps.assetRegexType == 0)
                {
                    EditorGUILayout.HelpBox("RegexType is nothing ", MessageType.Error); 
                }
            }


            if (GUILayout.Button("Applied Programe"))
            { 
                // 将当前的配置方案应用到项目
                Dictionary<string,List<string>> dict = BundleUtility.CheckFilesOver(Parent.data);
                bool pass = true;
                foreach(KeyValuePair<string,List<string>> kvp in dict)
                {
                    if (kvp.Value.Count > 1)
                    {
                        // 说明存在重复
                        for (int i = 0; i < kvp.Value.Count; i++)
                        {
                            pass = false;
                            Debug.LogError("file path:" + kvp.Key + "    AssetBundle:" + kvp.Value[i]);
                        }
                    }
                }
                if (pass == false)
                    return;

                // 通过检测，开始配置数据
                if ( Parent.data.items.Count > 0)
                {
                    for (int n = 0; n < Parent.data.items.Count; n++)
                    {
                        AssetsItem items = Parent.data.items[n];
                        string variant = string.Empty;
                        if (items.VariantName != 0)
                        {
                           variant = Parent.data.Variants[items.VariantName];
                        }
                        foreach(KeyValuePair<string,PathList>m_kvp in items.paths)
                        {
                            string vkey = m_kvp.Key;
                            PathList pl = items.paths[vkey];
                            for (int j = 0; j < pl.psList.Count; j++)
                            {
                                PathStruct ps = pl.psList[j];
 
                                Dictionary<string, int> paths = DirectorytUtility.getPathFiles(ps.path, ps.assetRegexType);
                                foreach (KeyValuePair<string, int> kvp in paths)
                                {
                                    string pkey = (string)kvp.Key;
                                    AssetImporter ai = AssetImporter.GetAtPath(pkey);
                                    ai.assetBundleName = items.AssetBundleName;
                                    ai.assetBundleVariant = variant;
                                }
                            }
                        }

                    }
                   AssetDatabase.RemoveUnusedAssetBundleNames();

                    //成功弹窗
                    EditorUtility.DisplayDialog("Apply success", "Done! ", "ok", null);
                }

              
            }
            if (GUILayout.Button("Files Show"))
            {
               
                // 将当前的配置方案应用到项目
                showWindow();
                //BundleUtility.CheckFilesOver(Parent.data);
            }
            EditorGUILayout.EndVertical();
        }

        private void showWindow(bool close = false)
        {
            if (window)
                window.Close();
            if (close == true)
                return;
            window = EditorWindow.GetWindow<AssetFileEditor>("AssetFileEditor");
            // 确定拥有列表，弹出当前ab的打包所有内从
            if (Parent.list.index >= 0 && Parent.data.items.Count > 0)
            {
                window.items = Parent.data.items[Parent.list.index];
                window.Variants = Parent.data.Variants;
                window.Reset();
                window.Show();
            }

            
        }

        /**
         * 拖拽
         * */
        private void DropProc()
        {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetLastRect();
            int id = GUIUtility.GetControlID(FocusType.Passive);
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition)) break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                    DragAndDrop.activeControlID = id;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                        {
                            for (int i = 0; i < DragAndDrop.paths.Length; i++)
                            {
                                PathStruct ps = new PathStruct();
                                ps.path = DragAndDrop.paths[i];
                                Parent.regsList.list.Add(ps);
                            }
                        }
                        DragAndDrop.activeControlID = 0;
                    }
                    Event.current.Use();
                    break;
            }
        }

        /**
         * 重置
         * */
        public override void reset()
        {
            Parent.regsList = null;
            Parent.regScrollPos = Vector2.zero;
        }
    }
}