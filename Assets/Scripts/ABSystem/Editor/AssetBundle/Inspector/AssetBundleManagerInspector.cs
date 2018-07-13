using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VFrame.ABSystem
{
    [CustomEditor(typeof(AssetBundleManager))]
    public class AssetBundleManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var manager = target as AssetBundleManager;
            Dictionary<string, AssetBundleInfo> infos =  manager.GetLoadedAssetBundle();
            GUILayout.BeginVertical();
            foreach (var info in infos)
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label("bundleName"); GUILayout.Label(info.Value.bundleName);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("refCount"); GUILayout.Label(info.Value.refCount.ToString());
                GUILayout.EndHorizontal();
            
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }
            GUILayout.EndVertical();
            base.OnInspectorGUI();
        }

    }
}
