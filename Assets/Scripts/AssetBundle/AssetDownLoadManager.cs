using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace Virivers
{

    /// <summary>
    /// 资源下载管理
    /// 1.只管理资源下载
    /// 2.不使用cache
    /// </summary>
    public class AssetDownLoadManager : MonoBehaviour {

        /// <summary>
        /// 下载完成
        /// 可以进行后续处理
        /// </summary>
        [HideInInspector]
        public bool IsDone = false;

        private static AssetDownLoadManager _instance;

        /// <summary>
        /// cdn服务器的manifest文件
        /// </summary>
        private AssetBundleManifest cdn_manifest = null;

        /// <summary>
        /// cdn 二进制文件
        /// </summary>
        private byte[] cdn_manifest_byte = null;

        /// <summary>
        /// stream文件夹下的manifest文件
        /// </summary>
        [HideInInspector]
        public AssetBundleManifest stream_manifest = null;

        /// <summary>
        /// peresistent文件夹下的manifest文件
        /// </summary>
        [HideInInspector]
        public AssetBundleManifest peresistent_manifest = null;

        /// <summary>
        /// 发布时写入的应用版本号
        /// </summary>
        private string application_version = "1.1.0";

        /// <summary>
        /// persistent文件夹下的version
        /// </summary>
        private string persistent_version;

        /// <summary>
        /// 是否需要保存version
        /// </summary>
        private bool isSaveVersion = false;

        /// <summary>
        /// 需要下载的任务
        /// assetBundleName
        /// </summary>
        private List<string> downLoadTask = null;

        /// <summary>
        /// 最大下载队列数
        /// </summary>
        private const int maxDownLoadSize = 5;

        /// <summary>
        /// 正在下载的队列
        /// </summary>
//        private Dictionary<string, UnityWebRequest> downLoading = null;
        private Dictionary<string, WWW> downLoading = null;

        /// <summary>
        /// 已经下载的ab
        /// </summary>
        private Dictionary<string, byte[]> assetBundles = null;
//        private Dictionary<string, AssetBundle> assetBundles = null;

        /// <summary>
        /// 保存住，可以设置一些属性
        /// </summary>
        private AssetBundleDownLoadOperation operation;

        /// <summary>
        /// 下载进度条
        /// 可以通过外界传入
        /// </summary>
        private Action<float> OnProcess;

        /// <summary>
        /// 总下载量
        /// </summary>
        private int totalTask;

        /// <summary>
        /// 当前完成几个
        /// </summary>
        private int curTask;

        public void SetOnProcessAction(Action<float> processAction)
        {
            this.OnProcess = processAction;
        }

        public static AssetDownLoadManager Instance {
            get {
                return _instance;
            }
        }

        void Awake() {
            if (_instance == null)
            {
                _instance = this;
            }

            this.downLoadTask = new List<string>();
            this.downLoading = new Dictionary<string, WWW>();
            this.assetBundles = new Dictionary<string, byte[]>();
        }

        // Update is called once per frame
        void Update () {
            if (this.IsDone)
            {
                return;
            }

            var keysToRemove = new List<string> ();
            foreach (var item in this.downLoading)
            {
                string assetBundleName = item.Key;
                WWW request = item.Value;

                if (request.error != null) {
                    Debug.LogError("down load fail name = " + assetBundleName + " error: " + request.error);
                    keysToRemove.Add(assetBundleName);
                    continue;
                }

                // 这里isDone不能说明就成功了，有可能时系统错误。这时需要判断downloadHandler
                // 如果request.isDone是系统错误了，需要判断downloadHandler
                if (request.isDone)
                {
                    byte[] ab = request.bytes;
                    this.assetBundles.Add(assetBundleName, ab);
                    keysToRemove.Add(assetBundleName);

                    this.curTask += 1;

                    if (this.OnProcess != null && this.totalTask > 0)
                    {
                        this.OnProcess(this.curTask / this.totalTask);
                    }
                }
            }

            // 需要清理
            if (keysToRemove.Count > 0)
            {
                for (int i = 0; i < keysToRemove.Count; i++)
                {
                    string key = keysToRemove[i];
                    WWW request = this.downLoading[key];
                    this.downLoading.Remove(key);
                    if (request != null)
                    {
                        request.Dispose();
                        request = null;
                    }
                }

                // 继续下载
                this.DownLoad();
            }
        }

        /// <summary>
        /// 执行资源同步
        /// </summary>
        public AssetBundleDownLoadOperation Initialize ()
        {
            operation = new AssetBundleDownLoadOperation();
            StartCoroutine(CheckAssetBundle());
            return operation;
        }

        IEnumerator CheckAssetBundle() {
            if (!this.operation.isError)
            {
                this.LoadPersistentVersion();
            }
            if (!this.operation.isError)
            {
                this.IfDelPeresistent();
            }

            string platformName = Utility.GetPlatformName();
            yield return StartCoroutine(DownLoadCDNManifest(platformName, "AssetBundleManifest"));
            if (!this.operation.isError)
            {
                this.LoadStreamManifest(platformName, "AssetBundleManifest");
            }
            if (!this.operation.isError)
            {
                this.LoadPeresistentManifest(platformName, "AssetBundleManifest");
            }
            if (!this.operation.isError)
            {
                this.FindDownLoadTask();
            }
            if (!this.operation.isError)
            {
                this.DownLoad();
            }
        }

        /// <summary>
        /// 下载cdn的manifest文件
        /// </summary>
        IEnumerator DownLoadCDNManifest(string assetBundleName, string assetName) {
            
            string url = Utility.BaseDownloadingURL() + assetBundleName + "/" + assetBundleName;

            using (WWW download = new WWW(url))
            {
                yield return download;

                if (download.error != null)
                {
                    // TODO 错误处理
                    string error = "Failed to downLoad cdn manifest, error = " + download.error;
                    this.operation.isError = true;
                    this.operation.error = error;
                    Debug.LogError(error);
                    yield break;
                }

                if (download.isDone)
                {
                    AssetBundle ab = download.assetBundle;
                    cdn_manifest_byte = download.bytes;
                    AssetBundleRequest request = ab.LoadAssetAsync(assetName, typeof(AssetBundleManifest));
                    yield return request;

                    if (request.isDone)
                    {
                        cdn_manifest = request.asset as AssetBundleManifest;
                    }

                    ab.Unload(false);
                }

                download.Dispose();
            }
        }

        /// <summary>
        /// 加载stream文件夹下的manifest文件
        /// </summary>
        /// <returns>The stream manifest.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private void LoadStreamManifest(string assetBundleName, string assetName) {
            string path = Path.Combine(Utility.GetStreamingAssetsPath(), assetBundleName + "/" + assetBundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                string error = "Failed to load stream manifest !";
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);
                return;
            }

            this.stream_manifest = bundle.LoadAsset<AssetBundleManifest>(assetName);
            bundle.Unload(false);
        }

        /// <summary>
        /// 下载peresistent文件夹下的manifest文件
        /// </summary>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private void LoadPeresistentManifest(string assetBundleName, string assetName) {
            string path = Path.Combine(Utility.PersistentPath(), assetBundleName);

            if (!File.Exists(path))
            {
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                string error = "Failed to load persistent manifest !";
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);
                return;
            }

            this.peresistent_manifest = bundle.LoadAsset<AssetBundleManifest>(assetName);
            bundle.Unload(false);
        }

        /// <summary>
        /// 找出需要下载的任务
        /// </summary>
        private void FindDownLoadTask() {
            AssetBundleManifest localManifest = null;

            if (this.peresistent_manifest == null)
            {
                localManifest = this.stream_manifest;
            }
            else
            {
                localManifest = this.peresistent_manifest;
            }

            string[] cdn_abNames = this.cdn_manifest.GetAllAssetBundles();
            string[] local_abNames = localManifest.GetAllAssetBundles();

            int cdnLen = cdn_abNames.Length;
            int localLen = local_abNames.Length;
            for (int i = 0; i < cdnLen; i++)
            {
                string cdn_name = cdn_abNames[i];
                Hash128 cdn_hash = this.cdn_manifest.GetAssetBundleHash(cdn_name);
                // 是否需要更新，没有文件或hash不同时都需要更新
                bool needUpdate = true;

                Hash128 local_hash = localManifest.GetAssetBundleHash(cdn_name);
                // 相同就不需要更新了
                if (cdn_hash.Equals(local_hash))
                {
                    needUpdate = false;
                }

                // 需要更新
                if (needUpdate)
                {
                    this.downLoadTask.Add(cdn_name);
                }
            }

            this.totalTask = this.downLoadTask.Count;
        }

        /// <summary>
        /// 下载
        /// </summary>
        private void DownLoad() {
            int task_count = this.downLoadTask.Count;
            int downloading_count = this.downLoading.Count;
            // 没有任务也没有下载进度，说明已经完成了
            if (task_count == 0 && downloading_count == 0)
            {
                this.SaveAssetBundle();
				#if !UNITY_PS4
				Caching.ClearCache();
                #endif
				this.Clear();

                if (this.OnProcess != null)
                {
                    // 完成了一定返回 100%
                    this.OnProcess(1.0f);
                }
                return;
            }

            for (int i = 0; i < task_count;)
            {
                // 可以下载
                if (this.downLoading.Count < maxDownLoadSize)
                {
                    string name = this.downLoadTask[i];
                    this.DownLoadAssetBundle(name);
                    task_count--;
                    this.downLoadTask.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 下载单个ab
        /// </summary>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private void DownLoadAssetBundle(string assetBundleName) {
            string platform = Utility.GetPlatformName();
            string path = Utility.BaseDownloadingURL() + platform + "/" + assetBundleName;

            // 开始下载
            WWW request = new WWW(path);
            this.downLoading.Add(assetBundleName, request);
        }

        /// <summary>
        /// 保存下载的ab
        /// </summary>
        private void SaveAssetBundle() {
            int count = this.assetBundles.Count;
            // 没有需要保存的ab
            if (count == 0)
            {
                this.IsDone = true;
                return;
            }

            string persistentDataPath = Utility.PersistentPath();
            try {
                // 文件夹没有就创建一个
                if (!Directory.Exists(persistentDataPath))
                {
                    Directory.CreateDirectory(persistentDataPath);
                }
            } catch (Exception e) {
                string error = "save assetBundle fail, error: " + e.Message;
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);
                return;
            }

            foreach (var item in this.assetBundles)
            {
                string assetBundleName = item.Key;
                byte[] ab = item.Value;

                this.SavePeresistentAB(assetBundleName, ab);
            }

            this.SaveManifest();
            if (this.isSaveVersion && !this.operation.isError)
            {
                this.SaveVersion();
            }
            this.IsDone = true;
        }

        /// <summary>
        /// 保存新下载的AB
        /// </summary>
        /// <param name="assetBundleName">Asset bundle name.</param>
        /// <param name="ab">Ab.</param>
        private void SavePeresistentAB(string assetBundleName, byte[] ab) {

            FileStream fs = null;
            BinaryWriter bw = null;
            string file = Path.Combine(Utility.PersistentPath(), assetBundleName);
            string strPath = Path.GetDirectoryName(file);
            try
            {
                // 文件夹没有就创建一个
                if (!Directory.Exists(strPath))
                {
                    Directory.CreateDirectory(strPath);
                }
                // 如果有重复的资源会先删除之前的，再写入
                fs = new FileStream(file, FileMode.Create);
                bw = new BinaryWriter(fs);
                bw.Write(ab);
            } catch (Exception e) {
                string error = "save AssetBundle fail, name = " + assetBundleName + " error: " + e.Message;
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);

                File.Delete(file);
            } finally {
                if (bw != null)
                {
                    bw.Flush();
                    bw.Close();
                    bw = null;
                }

                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }
        }

        /// <summary>
        /// 保存最新的manifest文件
        /// </summary>
        private void SaveManifest() {
            string file = Path.Combine(Utility.PersistentPath(), Utility.GetPlatformName());
            FileStream fs = null;
            BinaryWriter bw = null;
            try {
//                byte[] bytes = this.Serialize(this.cdn_manifest);
                // 开始写入文件 TODO 这里需要使用using?
                fs = new FileStream(file, FileMode.Create);
                bw = new BinaryWriter(fs);
                bw.Write(this.cdn_manifest_byte);
            } catch (Exception e) {
                string error = "save manifest fail, path = " + file + " error: " + e.Message;
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);
                // 把错误文件删除
                File.Delete(file);
            } finally {
                if (bw != null)
                {
                    bw.Flush();
                    bw.Close();
                    bw = null;
                }

                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }
        }

        /// <summary>
        /// 保存version
        /// </summary>
        private void SaveVersion() {
            string file = Path.Combine(Utility.PersistentPath(), "version");
            FileStream fs = null;
            StreamWriter sw = null;
            try {
                // 开始写入文件 TODO 这里需要使用using?
                fs = new FileStream(file, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.Write(this.application_version);
            } catch (Exception e) {
                string error = "save version fail, path = " + file + " error: " + e.Message;
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);
                // 把错误文件删除
                File.Delete(file);
            } finally {
                if (sw != null)
                {
                    sw.Flush();
                    sw.Close();
                    sw = null;
                }

                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }
        }

        private void Clear() {
            this.downLoadTask.Clear();
            this.downLoading.Clear();
            this.assetBundles.Clear();

            this.downLoadTask = null;
            this.downLoading = null;
            this.assetBundles = null;
        }

        //<summary> 
        /// 序列化 
        /// </summary> 
        /// <param name="data">要序列化的对象</param> 
        /// <returns>返回存放序列化后的数据缓冲区</returns> 
        private byte[] Serialize(object data)
        { 
            BinaryFormatter formatter = new BinaryFormatter(); 
            MemoryStream rems = new MemoryStream(); 
            formatter.Serialize(rems, data); 
            return rems.GetBuffer(); 
        }

        /// <summary>
        /// 是否需要删除peresistent文件夹
        /// 比较stream下和peresistent下的版本号
        /// </summary>
        private void IfDelPeresistent() {
            if (this.persistent_version == null)
            {
                this.isSaveVersion = true;
                return;
            }

            string[] application_vs = this.application_version.Split('.');
            int application_0 = int.Parse(application_vs[0]);
            int application_1 = int.Parse(application_vs[1]);

            string[] persistent_vs = this.persistent_version.Split('.');
            int persistent_0 = int.Parse(persistent_vs[0]);
            int persistent_1 = int.Parse(persistent_vs[1]);

            // 需要删除persistent文件夹下的内容
            if ((application_0 != persistent_0) || (application_1 != persistent_1))
            {
                this.isSaveVersion = true;
                string persistentDataPath = Utility.PersistentPath();
                try {
                    DirectoryInfo di = new DirectoryInfo(persistentDataPath);
                    di.Delete(true);
                } catch (Exception e) {
                    string error = "delet persistent path fail, error: " + e.Message;
                    this.operation.isError = true;
                    this.operation.error = error;
                    Debug.LogError(error);
                }
            }
        }

        /// <summary>
        /// 加载persistent文件夹下的version
        /// </summary>
        private void LoadPersistentVersion() {
            string path = Path.Combine(Utility.PersistentPath(), "version");

            // 文件不存在
            if (!File.Exists(path))
            {
                return;
            }

            FileStream fs = null;
            try {
                fs = new FileStream(path, FileMode.Open);
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);

                this.persistent_version = System.Text.Encoding.Default.GetString (data);
            } catch (Exception e) {
                string error = "LoadPersistentVersion fail, error: " + e.Message;
                this.operation.isError = true;
                this.operation.error = error;
                Debug.LogError(error);
            } finally {
                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }
        }
    }
}


