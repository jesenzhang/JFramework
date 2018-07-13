#define _AB_MODE_

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



namespace VFrame.ABSystem
{

    public class AssetBundleDepLoader
    {
        private AssetBundleDataReader _depInfoReader;
        private Action _initCallback;

        public AssetBundleDataReader GetDepInfo()
        {
            return _depInfoReader;
        }

        void InitComplete()
        {
            if (_initCallback != null)
                _initCallback();
            _initCallback = null;
        }

        public void Init(Stream depStream, Action callback)
        {
            if (depStream.Length > 4)
            {
                BinaryReader br = new BinaryReader(depStream);
                if (br.ReadChar() == 'A' && br.ReadChar() == 'B' && br.ReadChar() == 'D')
                {
                    if (br.ReadChar() == 'T')
                        _depInfoReader = new AssetBundleDataReader();
                    else
                        _depInfoReader = new AssetBundleDataBinaryReader();

                    depStream.Position = 0;
                    _depInfoReader.Read(depStream);
                }
            }

            depStream.Close();

            if (callback != null)
                callback();
        }

        public IEnumerator LoadDepInfo(Action completeCall)
        {
            _initCallback = completeCall;
            string depFile = string.Format("{0}/{1}", AssetBundlePathResolver.BundleCacheDir, AssetBundlePathResolver.DependFileName);
            //编辑器模式下测试AB_MODE，直接读取
#if UNITY_EDITOR
            depFile = AssetBundlePathResolver.GetBundleSourceFile(AssetBundlePathResolver.DependFileName, false);
#endif

            if (File.Exists(depFile))
            {
                FileStream fs = new FileStream(depFile, FileMode.Open, FileAccess.Read);
                Init(fs, null);
                fs.Close();
            }
            else
            {
                string srcURL = AssetBundlePathResolver.GetBundleSourceFile(AssetBundlePathResolver.DependFileName);
                WWW w = new WWW(srcURL);
                yield return w;

                if (w.error == null)
                {
                    Init(new MemoryStream(w.bytes), null);
                    File.WriteAllBytes(depFile, w.bytes);
                }
                else
                {
                    Debug.LogError(string.Format("{0} not exist!", depFile));
                }
            }
        }

    }
}