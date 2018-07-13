using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace Virivers
{
    /**
     * 资产编辑的数据
     * 
     * */
    public class AssetBundleData : ScriptableObject
    {
        // ab名称
        public List<string> AssetBunldeName = new List<string>();
        // 属性列表
        public List<string> Variants = new List<string>() {"none"};

        // 对应的AssetBundle配置
        public List<AssetsItem> items = new List<AssetsItem>();

        // Bundle配置文件的名字
        public string BundleName;
        // 编译参数
        public BuildAssetBundleOptions assetBundleOptions;
        // 平台参数
        public BuildTarget targetPlatform;

        // Bundle输出路径
        public string OutPutPath;

        public AssetBundleData Clone()
        {
            AssetBundleData data = ScriptableObject.CreateInstance("AssetBundleData") as AssetBundleData;
            data.BundleName = BundleName;
            data.OutPutPath = OutPutPath;
            data.assetBundleOptions = assetBundleOptions;
            data.targetPlatform = targetPlatform;
            data.AssetBunldeName = copy(AssetBunldeName);
            data.Variants = copy(Variants);
            data.items = copy(items); 
            return data; 
        }

        private List<string> copy(List<string> list)
        {
            List<string> l = new List<string>();
            for(int i = 0; i < list.Count; i++)
            {
                l.Add(list[i]);
            }
            return l;
        }

        private List<AssetsItem> copy(List<AssetsItem> list)
        {
            List<AssetsItem> l = new List<AssetsItem>();
            for (int i = 0; i < list.Count; i++)
            {
                l.Add(list[i].Clone());
            }

            return l;
        }
    }
}
