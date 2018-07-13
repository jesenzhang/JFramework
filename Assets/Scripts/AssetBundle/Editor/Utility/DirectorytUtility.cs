using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Virivers
{
    /**
     * 目录工具类
     * */
    public class DirectorytUtility
    {
        /**
         * 获得整个根目录结构下的所有文件地址
         * */
        public static Dictionary<string,int> getPathFiles(string path,AssetRegexType regexType)
        {
            Dictionary<string, int> files = new Dictionary<string, int>();
            string regex = RegexUtility.generateRegex(regexType);
            inputFiles(path, files, regex);
            string[] pathsList = Directory.GetDirectories(path);
            for (int i = 0; i < pathsList.Length; i++)
            {
                inputPathFiles(pathsList[i], files , regex);
            }
            return files;
        }

        public static void inputPathFiles(string path, Dictionary<string, int> files, string regex)
        {
            // 将当前目录的文件输入
            inputFiles(path, files, regex);
            // 继续向下遍历目录结构
            string[] pathsList = Directory.GetDirectories(path);
            for (int i = 0; i < pathsList.Length; i++)
            {
                inputPathFiles(pathsList[i], files, regex);
            }
        }

        public static void inputFiles(string path,Dictionary<string, int> files,string regex)
        {
            string[] filesList = Directory.GetFiles(path);
            for(int i = 0; i < filesList.Length; i++)
            {
                string filePath = filesList[i].Replace(@"\", @"/");
                if (Regex.IsMatch(filePath, regex) == true)
                {
                    if (files.ContainsKey(filePath) == false)
                    {
                        files.Add(filePath, 0);
                    }
                    else
                    {
                        files[filePath]++;
                    }
                }
            }
        }
    }
}
