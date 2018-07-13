using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEditorInternal;

namespace Virivers
{
    /**
     * AssetBundle配置管理页面
     * */
    public class AssetBundleEditor : BaseEditorWindow
    {

        public AssetBundleData data;
        public Vector2 regScrollPos;
        public ReorderableList regsList;

        public Vector2 scrollPos;
        public ReorderableList list;

        protected override Type[] getViewListType()
        {
            return new Type[] { typeof(AssetBundlePlugsBasePanel),typeof(AssetBundlePlugsPathPanel) };
        }

        void Awake()
        {
        }

        void OnDestroy()
        {
            data = null;
            Reset();
        }
    }
}