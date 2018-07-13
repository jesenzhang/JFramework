using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Virivers
{
    /**
     * 工具类
     * */
    public class BundleUtility
    {
        /**
         * 检测配置文件的正确性
         * */
        public static Dictionary<string, List<string>> CheckFilesOver(AssetBundleData data)
        {
            // 文件地址&&对应的列表
            Dictionary<string, List<string>> checkData = new Dictionary<string, List<string>>();
            // 所有的配置列表
            for(int i = 0; i < data.items.Count; i++)
            {
                AssetsItem item = data.items[i];

                foreach(KeyValuePair<string,PathList>kvp in item.paths)
                { 
                    string key = kvp.Key;
                    PathList paths = item.paths[key];
                    for (int n = 0; n < paths.psList.Count; n++)
                    {
                        // 目录下包含内容的规则
                        PathStruct ps = paths.psList[n];
                        Dictionary<string, int> files = DirectorytUtility.getPathFiles(ps.path, ps.assetRegexType);
                        foreach (var kv in files)
                        {
                            if (checkData.ContainsKey(kv.Key) == false)
                            {
                                checkData.Add(kv.Key, new List<string>() { item.AssetBundleName });
                            }
                            else
                            {
                                checkData[kv.Key].Add(item.AssetBundleName);
                            }
                        }
                    }
                }
            }
            return checkData;
        }
       
    }
}