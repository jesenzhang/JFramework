using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

namespace Virivers
{

    [Serializable]
    public class PathStruct
    {
        // 路径
        public string path;
        // 获得资源的正则表达式选择
        public AssetRegexType assetRegexType;
    }

    [Serializable]
    public class PathList
    {
        public List<PathStruct> psList = new List<PathStruct>();
    }

    /**
     * 存储对应的AssetBunldeName
     * 
     * */
    [Serializable]
    public class AssetsItem
    {
        /**
         * 对应的assetBundle
         * */
        public string AssetBundleName;

        /**
         * 默认的属性
         * */
        public int VariantName = 0;
        /**
         * assets相关状态
         * */
        public SerializableDictionaryPathList paths = new SerializableDictionaryPathList();


        public AssetsItem Clone()
        {
            AssetsItem item = new AssetsItem();
            item.AssetBundleName = AssetBundleName;
            item.paths = copy(paths);
            return item;
        }

        private SerializableDictionaryPathList copy(SerializableDictionaryPathList dict)
        {
            SerializableDictionaryPathList d = new SerializableDictionaryPathList();
            foreach(KeyValuePair<string,PathList> kvp in dict)
            {
                d.Add(kvp.Key, copy(kvp.Value));
            }
            return d;
        }

        private PathList copy(PathList list)
        {
            PathList l = new PathList();
            for (int i = 0; i < list.psList.Count; i++)
            {
                PathStruct ps = new PathStruct();
                ps.path = list.psList[i].path;
                ps.assetRegexType = list.psList[i].assetRegexType;
                l.psList.Add(ps);
            }
            return l;
        }
    }
}
