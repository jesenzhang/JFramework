using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

namespace Virivers
{
    public class AssetBundlePlugsEditor : BaseEditorWindow
    {
        [MenuItem("Tools/AssetBundleManager/build")]
        public static void Build()
        {
            BuildPipeline.BuildAssetBundles("Export/DefalutAssetBundle", BuildAssetBundleOptions.DeterministicAssetBundle|BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem("Tools/AssetBundleManager/Plugs")]
        public static void Open()
        {
            GetWindow<AssetBundlePlugsEditor>("AssetBundlePlugsEditor").Show();
        }

        [MenuItem("Tools/AssetBundleManager/CleanAssetBundleNames")]
        public static void CleanAssetBundleNames()
        {
            var names = AssetDatabase.GetAllAssetBundleNames();
            foreach (string name in names)
            {
                Debug.Log("Asset Bundle: " + name);
                AssetDatabase.RemoveAssetBundleName(name, true);
            }
        }

       

    protected override Type[] getViewListType()
        {
            return new Type[] { typeof(AssetBundleInitPanel),typeof(AssetBundleRootPanel) };
        }

        public AssetBundleData data;

        void Awake()
        {
            
        }
    }
}
