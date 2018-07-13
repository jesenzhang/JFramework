using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Virivers
{
    public class AssetFilePanel : ViewAbstract
    {
        // 滚动条
        private Dictionary<string, Vector2> scrollDict = new Dictionary<string, Vector2>();

        private Dictionary<string, ReorderableList> rlist = new Dictionary<string, ReorderableList>();

        private Dictionary<string, List<string>> files = new Dictionary<string, List<string>>();

        new public AssetFileEditor Parent
        {
            set { parent = value; }
            get { return (AssetFileEditor)parent; }
        }

        public override void Drawing()
        {
            if (Parent.Variants == null)
                return;
            if (Parent.items == null)
                return;
            // 绘制文件列表
            for (int i = 0; i < Parent.Variants.Count; i++)
            {
                if(Parent.items.paths.ContainsKey(Parent.Variants[i]))
                {
                    PathList pl = Parent.items.paths[Parent.Variants[i]];
                    DrawingList(Parent.Variants[i], pl);
                }
            }
        }

        // 绘制所有的item分类
        private void DrawingList(string titleName,PathList pl)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical("Box");
            if (scrollDict.ContainsKey(titleName) == false)
                scrollDict.Add(titleName, new Vector2());
            scrollDict[titleName] = EditorGUILayout.BeginScrollView(scrollDict[titleName], GUILayout.Width(Parent.position.width - 12), GUILayout.Height(200));

            ReorderableList list;
            if (rlist.ContainsKey(titleName) == false)
            {
                // 初始化列表
                files.Add(titleName, new List<string>());
                for (int i = 0; i < pl.psList.Count; i++)
                {
                    Dictionary<string, int> f = DirectorytUtility.getPathFiles(pl.psList[i].path, pl.psList[i].assetRegexType);
                    foreach (KeyValuePair<string, int> kv in f)
                    {
                        if (files[titleName].IndexOf(kv.Key) == -1)
                        {
                            files[titleName].Add(kv.Key);
                        }
                    }
                }

                list = new ReorderableList(files[titleName], typeof(string), false, false, false, false);
                rlist.Add(titleName, list);
            }
            else
            {
                list = rlist[titleName];
            }

            // 绘制表头
            list.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, titleName);
            };
            // 渲染element
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.TextField(rect,"path:", (string)list.list[index]);
            };

            list.DoLayoutList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        public override void reset()
        {
            scrollDict = new Dictionary<string, Vector2>();
            rlist = new Dictionary<string, ReorderableList>();
            files = new Dictionary<string, List<string>>();
        }
    }
}