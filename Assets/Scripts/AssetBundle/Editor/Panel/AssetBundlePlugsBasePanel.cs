using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace Virivers
{
    /**
     * 扩展面板的基础页面
     * */
    public class AssetBundlePlugsBasePanel : ViewAbstract
    {
        new public AssetBundleEditor Parent
        {
            set { parent = value; }
            get { return (AssetBundleEditor)parent; }
        }

        

        public override void Drawing()
        {
            if (Parent.data == null)
                return;
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical("Box");
            // 渲染一个可滚动的List
            Parent.scrollPos = EditorGUILayout.BeginScrollView(Parent.scrollPos, GUILayout.Width(Parent.position.width - 12), GUILayout.Height(200));
            // assetbundle列表
            if (Parent.list == null)
            {
                Parent.list = new ReorderableList(Parent.data.items, typeof(AssetsItem), false, false, true, true);
            }
            // 绘制Item显示列表
            Parent.list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                AssetsItem element = Parent.list.list[index] as AssetsItem;
                rect.y += 2;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, 40, EditorGUIUtility.singleLineHeight), "Name:");
                EditorGUI.TextField(new Rect(rect.x + 45, rect.y, rect.width - 45 - 60, EditorGUIUtility.singleLineHeight), element.AssetBundleName);
                EditorGUI.BeginChangeCheck();
                element.VariantName = EditorGUI.Popup(new Rect(rect.width - 50, rect.y, 60, EditorGUIUtility.singleLineHeight), element.VariantName, Parent.data.Variants.ToArray());
                if(EditorGUI.EndChangeCheck())
                    Parent.regsList = null;
            };
            // 绘制表头
            Parent.list.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "AssetBundleList");
            };
            // 删除一个assetbundle
            Parent.list.onRemoveCallback = (ReorderableList l) => {
                l.list.RemoveAt(l.index);
                l.index = 0;
                Parent.regsList = null;
            };
            // 选择回调
            Parent.list.onSelectCallback = (ReorderableList l) => {
                Parent.regsList = null;
            };
            // 添加事件
            Parent.list.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) => {
                var menu = new GenericMenu();
                Dictionary<string, int> hash = new Dictionary<string, int>();
                for (int i = 0; i < Parent.data.items.Count; i++)
                {
                    hash[Parent.data.items[i].AssetBundleName] = 0;
                }
                for (int i = 0; i < Parent.data.AssetBunldeName.Count; i++)
                {
                    if (hash.ContainsKey(Parent.data.AssetBunldeName[i]) == false)
                        menu.AddItem(new GUIContent(Parent.data.AssetBunldeName[i]), false, addABHandler, new menuParams() { name = Parent.data.AssetBunldeName[i] });
                }
                menu.ShowAsContext();
            };
            Parent.list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        public override void reset()
        {
            Parent.list = null;
            Parent.scrollPos = Vector2.zero;
        }

        private struct menuParams
        {
            public string name;
        }

        /**
         * 添加AssetBundle的Item对象
         * */
        private void addABHandler(object target)
        {
            menuParams m = (menuParams)target;
            AssetsItem asset = new AssetsItem();
            asset.AssetBundleName = m.name;
            asset.VariantName = 0;
            Parent.list.list.Add(asset);
            Parent.regsList = null;
        }
    }
}
