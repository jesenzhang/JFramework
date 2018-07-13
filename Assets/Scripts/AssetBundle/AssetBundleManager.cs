using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace Virivers
{
    /// <summary>
    /// 下载任务
    /// </summary>
    public class LoadTask
    {
        public string name;
        public string variants;
        public Action<string> callBack;
        public string scope;
        public Action<string, string, Action<string>> baseCallBack;

        public LoadTask(string scope, string abName)
        {
            this.scope = scope;
			this.name = abName;
        }

        public LoadTask(string s, string n, string v)
        {
            this.scope = s;
            this.name = n;
            this.variants = v;
        }

        public LoadTask(string s, string n, string v, Action<string> callBack)
        {
            this.scope = s;
            this.name = n;
            this.variants = v;
            this.callBack = callBack;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s">作用域</param>
        /// <param name="n">assetBundle名字</param>
        /// <param name="v">变体</param>
        /// <param name="callBack">回调</param>
		public LoadTask(string s, string n, string v, Action<string> callBack,
            Action<string, string, Action<string>> baseCallBack)
        {
            this.scope = s;
            this.name = n;
            this.variants = v;
            this.callBack = callBack;
            this.baseCallBack = baseCallBack;
        }
    }

    /// <summary>
    /// AssetBundle保存类
    /// </summary>
    public class AssetBundleRef
    {
        public AssetBundle assetBundle = null;
        public string name;

        /// <summary>
        /// 引用这个AB的次数，如果是0考虑卸载
        /// </summary>
        public int m_ReferencedCount;

        public AssetBundleRef(string assetBundleName, AssetBundle ab)
        {
            this.name = assetBundleName;
            this.assetBundle = ab;
            this.m_ReferencedCount = 1;
        }
    }

    /// <summary>
    /// 资源加载
    /// 从StreamingAssetsPath或persistentDataPath中加载
    /// </summary>
    public class AssetBundleManager : MonoBehaviour
    {
#if UNITY_EDITOR
        // 模拟模式是否开启
        const string kSimulationMode = "Tools/AssetBundleManager/Simulation Mode";
        [MenuItem(kSimulationMode)]
        public static bool ToggleSimulationModeValidate()
        {
            SimulateAssetBundleInEditor = !SimulateAssetBundleInEditor;
            Menu.SetChecked(kSimulationMode, SimulateAssetBundleInEditor);
            return SimulateAssetBundleInEditor;
        }
#endif

        private static AssetBundleManager _instance;

        /// <summary>
        /// 最新的manifest文件
        /// 从来没有更新的时候，这个文件是null。所以要使用stream_manifest
        /// </summary>
        [HideInInspector]
        public AssetBundleManifest peresistent_manifest;

        /// <summary>
        /// 打包的manifest文件
        /// 这个文件一定存在
        /// </summary>
        [HideInInspector]
        public AssetBundleManifest stream_manifest;

        /// <summary>
        /// 加载完成
        /// key:assetBundleName + variants
        /// </summary>
        private Dictionary<string, AssetBundleRef> dictAssetBundleRefs;

        /// <summary>
        /// 将要加载的任务
        /// </summary>
        public List<LoadTask> downList;

        /// <summary>
        /// 依赖关系
        /// key:assetBundleName + variants
        /// value:保存的都是加入变体的ab名字
        /// </summary>
        private Dictionary<string, string[]> dependencies;

        /// <summary>
        /// 正在加载的operation
        /// </summary>
        private List<AssetBundleOperation> inProgressOperations;

        /// <summary>
        /// 正在加载的
        /// key:assetBundleName + variants
        /// </summary>
        public Dictionary<string, AssetBundleCreateRequest> dicLoadings;

        /**
         * 存储作用域的字典映射
         * */
		private Dictionary<string, List<string>> dictScope = new Dictionary<string, List<string>> ();

        /// <summary>
        /// 最大下载任务数
        /// </summary>
        private int _maxSize = 5;

        /// <summary>
        /// 是否模拟
        /// </summary>
		private static bool SimulateAssetBundleInEditor = true;

        /// <summary>
        /// 变体的集合
        /// </summary>
        private string[] bundlesWithVariant;

        /// <summary>
        /// 模拟时使用
        /// </summary>
		private Dictionary<string, Dictionary<string, UnityEngine.Object>> simulateAssetDic;

        /// <summary>
        /// 记录正在加载的ab的关联引用数
        /// </summary>
        private Dictionary<string, int> abRefCntLoadingDic; 

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }

            this.dictAssetBundleRefs = new Dictionary<string, AssetBundleRef>();
            this.downList = new List<LoadTask>();
            this.dependencies = new Dictionary<string, string[]>();
            this.inProgressOperations = new List<AssetBundleOperation>();
            this.dicLoadings = new Dictionary<string, AssetBundleCreateRequest>();
            this.dictScope = new Dictionary<string, List<string>>();
			this.simulateAssetDic = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();
            this.abRefCntLoadingDic = new Dictionary<string, int>();
        }

        public static AssetBundleManager Instance
        {
            get
            {				
                return _instance;
            }
        }

        void Update()
        {
            #if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                return;
            }
            #endif

            if (dicLoadings == null || dicLoadings.Count == 0)
                return;
            // Collect all the finished WWWs.
            List<string> keysToRemove = new List<string>();
            foreach (var keyValue in this.dicLoadings)
            {
                AssetBundleCreateRequest request = keyValue.Value;

                // isDone也有可能在系统错误的时候返回
                if (request.isDone)
                {
                    AssetBundle bundle = request.assetBundle;
                    if (bundle == null)
                    {
                        Debug.LogError(string.Format("Failed load bundle {0} : bundle is null", keyValue.Key));
                        keysToRemove.Add(keyValue.Key);

                        if (this.abRefCntLoadingDic.ContainsKey(keyValue.Key))
                        {
                            this.abRefCntLoadingDic[keyValue.Key] --;
                            if (this.abRefCntLoadingDic[keyValue.Key] == 0)
                            {
                                this.abRefCntLoadingDic.Remove(keyValue.Key);
                            }
                        }

                        continue;
                    }

                    int rCount = 1;
                    if (this.abRefCntLoadingDic.ContainsKey(keyValue.Key))
                    {
                        rCount = this.abRefCntLoadingDic[keyValue.Key];
                        this.abRefCntLoadingDic.Remove(keyValue.Key);
                    }

                    AssetBundleRef abr = new AssetBundleRef(keyValue.Key, bundle);
                    abr.m_ReferencedCount = rCount;
                    dictAssetBundleRefs.Add(keyValue.Key, abr);
                    keysToRemove.Add(keyValue.Key);
                }
            }

            // Remove the finished WWWs.
            foreach (var key in keysToRemove)
            {
                AssetBundleCreateRequest request = this.dicLoadings[key];
                this.dicLoadings.Remove(key);
                request = null;
            }

            // Update all in progress operations
            for (int i = 0; i < inProgressOperations.Count;)
            {
                AssetBundleOperation operation = inProgressOperations[i];
                if (!operation.Update())
                {
                    inProgressOperations.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // 可以继续加载
            while (this.downList.Count > 0 && (this.dicLoadings.Count <= this._maxSize))
            {
                LoadTask task = this.downList[0];
                this.LoadAssetAsync(task.scope,task.name, task.variants, task.callBack,task.baseCallBack);

                this.downList.RemoveAt(0);
            }
        }

        /// <summary>
        /// 加载stream文件夹下的manifest文件
        /// </summary>
        /// <returns>The stream manifest.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        /// <param name="assetName">一般取值都是"AssetBundleManifest" </param>
        public void LoadStreamManifest()
        {
            if (this.stream_manifest != null)
            {
                return;
            }

            string platformName = Utility.GetPlatformName();
            string assetName = "AssetBundleManifest";

            string path = Path.Combine(Utility.GetStreamingAssetsPath(), platformName + "/" + platformName);
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                string error = "Failed to load stream manifest !";
                Debug.LogError(error);
                return;
            }

            this.stream_manifest = bundle.LoadAsset<AssetBundleManifest>(assetName);
            bundle.Unload(false);
        }

        /// <summary>
        /// 获得实际的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="abAssetName"></param>
        /// <param name="split"></param>
        /// <param name="variants"></param>
        /// <returns></returns>
        public T GetAsset<T>(string abAssetName, string variants = "", char split = '.') where T : UnityEngine.Object
        {
			string[] s = abAssetName.Split(split);
            if (s.Length != 2)
            {
                return null;
            }
            string assetBundleName = s[0];
            string assetName = s[1];

            #if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                string tmp_assetBundleName = this.GetVariants(assetBundleName, variants);
                Dictionary<string, UnityEngine.Object> dic = null;
                if (this.simulateAssetDic.TryGetValue(tmp_assetBundleName, out dic))
                {
                    UnityEngine.Object ob = null;
                    if (dic.TryGetValue(assetName, out ob))
                    {
                        return ob as T;
                    }
                }

                return null;
            }
            else
            #endif
            {
                AssetBundle ab = GetAssetBundle(assetBundleName, variants);
                return ab.LoadAsset<T>(assetName);
            }
        }

        /**
         * 获得实际的对象
         * */
        public T GetAsset<T>(string assetBundleName,string assetName,string variants = "") where T:UnityEngine.Object
        {
            #if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                string tmp_assetBundleName = this.GetVariants(assetBundleName, variants);
				Dictionary<string, UnityEngine.Object> dic = null;
				if (this.simulateAssetDic.TryGetValue(tmp_assetBundleName, out dic))
                {
					UnityEngine.Object ob = null;
					if (dic.TryGetValue(assetName, out ob)) {
						return ob as T;
					}
                }

                return null;
            }
            else
            #endif
            {
                AssetBundle ab = GetAssetBundle(assetBundleName, variants);
                return ab.LoadAsset<T>(assetName);
            }
        }

        /// <summary>
        /// 获取一个ab
        /// </summary>
        /// <returns>The asset bundle.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        public AssetBundle GetAssetBundle(string assetBundleName, string variants = "")
        {
            AssetBundleRef abRef;
            string variantsName = this.GetVariants(assetBundleName, variants);
            if (!dictAssetBundleRefs.TryGetValue(variantsName, out abRef))
            {
                return null;
            }
            // No dependencies are recorded, only the bundle itself is required.
            string[] deps = null;
            if (!dependencies.TryGetValue(variantsName, out deps))
            {
                return abRef.assetBundle;
            }

            // 如果依赖没有加载完成也无法获取
            foreach (var dependency in deps)
            {
                // Wait all the dependent assetBundles being loaded.
                AssetBundleRef dependentBundle;
                dictAssetBundleRefs.TryGetValue(dependency, out dependentBundle);
                if (dependentBundle == null)
                {
                    return null;
                }
            }

            return abRef.assetBundle;
        }

        /// <summary>
        /// 多任务加载
        /// </summary>
        /// <param name="list"></param>
		public void MultiLoadAssetBundle(List<LoadTask> list, Action onCompleteCallBack)
        {

			if(SimulateAssetBundleInEditor==false){
				LoadStreamManifest ();
			}

			for (int i = 0; i < list.Count; i ++)
            {
                LoadTask lt = list[i];
                string abName = lt.name;
                string variants = lt.variants;
                string scopeName = lt.scope;
                Action<string> callBack = lt.callBack;

                #if UNITY_EDITOR
                if (SimulateAssetBundleInEditor)
                {
                    string assetBundleName = this.GetVariants(abName, variants);
                    string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                    if (assetPaths.Length == 0)
                    {
                        Debug.LogError("There is no asset with assetBundleName \"" + assetBundleName);
                        return;
                    }

					Dictionary<string, UnityEngine.Object> dic = new Dictionary<string, UnityEngine.Object>();
					for (int j = 0; j < assetPaths.Length; j ++) {
						string name = assetPaths[j];
						UnityEngine.Object target = AssetDatabase.LoadMainAssetAtPath(name);
						string tmp = Path.GetFileNameWithoutExtension(name);
						if (dic.ContainsKey(tmp)) {
							Debug.LogWarning("注意：存在重名资源 tmp = " + tmp + " name = " + name);
							continue;
						}
						dic.Add(tmp, target);
					}
					this.simulateAssetDic.Add(abName, dic);
                    // @TODO: Now we only get the main object from the first asset. Should consider type also.

                    // 存储到字典中
                    if (dictScope.ContainsKey(scopeName) == false)
                        dictScope.Add(scopeName, new List<string>());
                    dictScope[scopeName].Add(assetBundleName);

                    if (callBack != null)
                    {
                        callBack(assetBundleName);
                    }
                }
                else
                #endif
                {
                    // 回调函数的基础
                    Action<string, string, Action<string>> baseCallBack = (string ScopeName, string AssetBundleName, Action<string> CallBack) => {
                        // 存储到字典中
                        if (dictScope.ContainsKey(ScopeName) == false)
                            dictScope.Add(ScopeName, new List<string>());
                        dictScope[ScopeName].Add(AssetBundleName);
                        if (CallBack != null)
                            CallBack(AssetBundleName);
                    };

                    // 队列没有达上限，可以直接下载
                    if (this.dicLoadings.Count < this._maxSize)
                    {
                        this.LoadAssetAsync(scopeName, abName, variants, callBack, baseCallBack);
                    }
                    else
                    {
                        lt.baseCallBack = baseCallBack;
                        downList.Add(lt);
                    }
                }
            }

            if (onCompleteCallBack != null)
            {
                AssetBundleLoadAssetOperationBatch batch = new AssetBundleLoadAssetOperationBatch(onCompleteCallBack);
                StartCoroutine(batch);
            }
        }

        /// <summary>
        /// 添加加载任务
        /// TODO 只能在外部调用时，自己初始化AssetBundleLoadAssetOperationBatch来控制，这里可以改掉
        /// </summary>
        /// <param name="abName">Ab name.</param>
        /// <param name="callBack">Call back.</param>
		public void MultiLoadAssetBundle(string scopeName,string abName, Action<string> callBack, string variants = "")
        {
			if(SimulateAssetBundleInEditor==false){
				LoadStreamManifest ();
			}
            #if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                string assetBundleName = this.GetVariants(abName, variants);
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
                if (assetPaths.Length == 0)
                {
                    Debug.LogError("There is no asset with assetBundleName \"" + assetBundleName);
                    return;
                }

				Dictionary<string, UnityEngine.Object> dic = new Dictionary<string, UnityEngine.Object>();
				for (int j = 0; j < assetPaths.Length; j ++) {
					string name = assetPaths[j];
					UnityEngine.Object target = AssetDatabase.LoadMainAssetAtPath(name);
					string tmp = Path.GetFileNameWithoutExtension(name);
					if (dic.ContainsKey(tmp)) {
						Debug.LogWarning("注意：存在重名资源 tmp = " + tmp + " name = " + name);
						continue;
					}
					dic.Add(tmp, target);
				}
				this.simulateAssetDic.Add(abName, dic);
                // @TODO: Now we only get the main object from the first asset. Should consider type also.
//                UnityEngine.Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[assetPaths.Length - 1]);
//                this.simulateAssetDic.Add(abName, target);

                // 存储到字典中
                if (dictScope.ContainsKey(scopeName) == false)
                    dictScope.Add(scopeName, new List<string>());
                dictScope[scopeName].Add(assetBundleName);

                if (callBack != null) {
                    callBack(assetBundleName);
                }
            }
            else
            #endif
            {
                // 回调函数的基础
                Action<string,string,Action<string>> baseCallBack = (string ScopeName, string AssetBundleName, Action<string> CallBack) => {
                    // 存储到字典中
                    if (dictScope.ContainsKey(ScopeName) == false)
                        dictScope.Add(ScopeName, new List<string>());
                    dictScope[ScopeName].Add(AssetBundleName);
                    if (CallBack != null)
                        CallBack(AssetBundleName);
                };

                // 队列没有达上限，可以直接下载
                if (this.dicLoadings.Count < this._maxSize)
                {
                    this.LoadAssetAsync(scopeName,abName, variants, callBack,baseCallBack);
                }
                else
                {
                    // 需要等待
                    LoadTask task = new LoadTask(scopeName, abName, variants, callBack, baseCallBack);
                    //LoadTask task = new LoadTask();
                    //task.scope = scopeName;
                    //task.name = abName;
                    //task.variants = variants;
                    //task.callBack = callBack;
                    //task.baseCallBack = baseCallBack;
                    downList.Add(task);
                }
            }
        }

        /// <summary>
        /// 加载单一的ab
        /// TODO 这里没有再进行加载队列个数的判定，所以可能会超过最大加载数的限制
        /// </summary>
        /// <returns>The asset async single.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        /// <param name="variants">Variants.</param>
        /// <param name="assetName">Asset name.</param>
        /// <param name="callBack">Call back.</param>
        /// <param name="type">Type.</param>
        public AssetBundleLoadAssetOperation LoadAssetAsyncSingle (string scopeName,string assetBundleName,
            string assetName, Action<string, string> callBack, System.Type type, string variants = "") {

            AssetBundleLoadAssetOperation operation = null;
            #if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                assetBundleName = this.GetVariants(assetBundleName, variants);
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
                if (assetPaths.Length == 0)
                {
                    Debug.LogError("There is no asset with name \"" + assetName + "\" in " + assetBundleName);
                    return null;
                }

				Dictionary<string, UnityEngine.Object> dic = new Dictionary<string, UnityEngine.Object>();
				UnityEngine.Object target = null;
				for (int j = 0; j < assetPaths.Length; j ++) {
					string name = assetPaths[j];
					UnityEngine.Object ob = AssetDatabase.LoadMainAssetAtPath(name);
					string tmp = Path.GetFileNameWithoutExtension(name);
					if (tmp == assetName) {
						target = ob;
					}
					if (dic.ContainsKey(tmp)) {
						Debug.LogWarning("注意：存在重名资源 tmp = " + tmp + " name = " + name);
						continue;
					}
					dic.Add(tmp, ob);
				}
				this.simulateAssetDic.Add(assetBundleName, dic);

                // @TODO: Now we only get the main object from the first asset. Should consider type also.
//                UnityEngine.Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
//                this.simulateAssetDic.Add(assetBundleName, target);
                // 存储到字典中
                if (dictScope.ContainsKey(scopeName) == false)
                    dictScope.Add(scopeName, new List<string>());
                dictScope[scopeName].Add(assetBundleName);

                operation = new AssetBundleLoadAssetOperationSimulation(target);
                if (callBack != null)
                {
                    callBack(assetBundleName, assetName);
                }
            }
            else
            #endif
            {
                // 回调函数的基础
                Action<string, string,string, Action<string, string>> baseCallBack = (string ScopeName, string AssetBundleName,string AssetName, Action<string, string> CallBack) => {
                    // 存储到字典中
                    if (dictScope.ContainsKey(ScopeName) == false)
                        dictScope.Add(ScopeName, new List<string>());
                    dictScope[ScopeName].Add(AssetBundleName);
                    if (CallBack != null)
                        CallBack(AssetBundleName,AssetName);
                };

                this.LoadAssetBundle(scopeName, assetBundleName, variants);
                string variantsName = this.GetVariants(assetBundleName, variants);
                operation = new AssetBundleLoadAssetOperationSingle(scopeName,variantsName, assetName, callBack, type,baseCallBack);
                this.inProgressOperations.Add (operation);

                if (callBack != null)
                {
                    StartCoroutine(operation);
                }
            }

            return operation;
        }

        /// <summary>
        /// 加载场景
        /// TODO 这里没有再进行加载队列个数的判定，所以可能会超过最大加载数的限制
        /// </summary>
        /// <returns>The level async.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        /// <param name="levelName">Level name.</param>
        /// <param name="isAdditive">If set to <c>true</c> is additive.</param>
        public AssetBundleOperation LoadSceneAsync (string scopeName,string assetBundleName, string levelName, bool isAdditive, Action<string> callBack, string variants = "")
        {
            AssetBundleOperation operation = null;
            #if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                assetBundleName = this.GetVariants(assetBundleName, variants);
                // 存储到字典中
                if (dictScope.ContainsKey(scopeName) == false)
                    dictScope.Add(scopeName, new List<string>());
                dictScope[scopeName].Add(assetBundleName);

                operation = new AssetBundleLoadSceneSimulationOperation(assetBundleName, levelName, isAdditive, callBack);

				if (callBack != null)
				{
					StartCoroutine(operation);
				}
            }
            else
            #endif
            {
                // 回调函数的基础
                Action<string, string, Action<string>> baseCallBack = (string ScopeName, string AssetBundleName, Action<string> CallBack) => {
                    // 存储到字典中
                    if (dictScope.ContainsKey(ScopeName) == false)
                        dictScope.Add(ScopeName, new List<string>());
                    dictScope[ScopeName].Add(AssetBundleName);
                    if (CallBack != null)
                        CallBack(AssetBundleName);
                };

                this.LoadAssetBundle (scopeName, assetBundleName, variants);
                string variantsName = this.GetVariants(assetBundleName, variants);
                operation = new AssetBundleLoadSceneOperation (scopeName,variantsName, levelName, isAdditive, callBack, baseCallBack);

                this.inProgressOperations.Add (operation);

                if (callBack != null)
                {
                    StartCoroutine(operation);
                }
            }

            return operation;
        }

        /**
         * 移除某个作用域下的所有载入
         * */
        public void RemoveScopeAll(string scopeName,bool v = true)
        {
            if (dictScope.ContainsKey(scopeName) == false)
                return;
            List<string> scopeList = dictScope[scopeName];
            for (int i = 0; i < scopeList.Count; i++)
            {
                UnloadAssetBundle(scopeList[i],v);
            }
        }

        /// <summary>
        /// 卸载ab
        /// </summary>
        /// <param name="assetBundleName">Asset bundle name.</param>
        /// <param name="v">If set to <c>true</c> v.</param>
        public void UnloadAssetBundle(string scopeName, string assetBundleName, bool v = true, string variants = "")
        {
            if (dictScope.ContainsKey(scopeName) == false)
                return;

            List<string> scopeList = dictScope[scopeName];
            string variantsName = this.GetVariants(assetBundleName, variants);
            if (scopeList.Remove(variantsName))
            {
                this.UnloadAssetBundleInternal(variantsName, v);
                this.UnloadDependencies(variantsName, v);
            }
        }

        /**
         * 卸载ab，通过已经联合过的名字
         * */
        private void UnloadAssetBundle(string variantsName, bool v = true)
        {
            this.UnloadAssetBundleInternal(variantsName, v);
            this.UnloadDependencies(variantsName, v);

#if UNITY_EDITOR
            // 删除模拟时使用的集合数据，要不然后续的再加载时会报错
            if (SimulateAssetBundleInEditor)
            {
                if (this.simulateAssetDic.ContainsKey(variantsName))
                {
                    this.simulateAssetDic.Remove(variantsName);
                }
            }
#endif
        }

        /// <summary>
        /// 多个任务一起加载时使用
        /// 异步加载
        /// </summary>
        /// <returns>The asset async.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        /// <param name="callBack">Call back.</param>
        private AssetBundleLoadAssetOperationBatchItem LoadAssetAsync(string scopeName,string assetBundleName, string variants, Action<string> callBack,Action<string,string,Action<string>> baseCallBack)
        {
            this.LoadAssetBundle(scopeName, assetBundleName, variants);

            string variantsName = this.GetVariants(assetBundleName, variants);
            AssetBundleLoadAssetOperationBatchItem item = new AssetBundleLoadAssetOperationBatchItem(scopeName,variantsName, callBack,baseCallBack);
            inProgressOperations.Add(item);

            return item;
        }

        /// <summary>
        /// 加载资源
        /// 包括主题资源与依赖资源
        /// </summary>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private void LoadAssetBundle(string scope, string assetBundleName, string variants)
        {
            // Check if the assetBundle has already been processed.
            bool isAlreadyProcessed = this.LoadAssetBundleInternal(assetBundleName, variants);

            // Load dependencies.
            if (!isAlreadyProcessed)
            {
                LoadDependencies(scope, assetBundleName, variants);
            }
        }

        /// <summary>
        /// 开始真正加载
        /// </summary>
        /// <returns><c>true</c>, if asset bundle internal was loaded, <c>false</c> otherwise.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private bool LoadAssetBundleInternal(string assetBundleName, string variants)
        {
            // Already loaded.
            AssetBundleRef bundle = null;
            string variantsName = this.GetVariants(assetBundleName, variants);
            // 已经下载
            dictAssetBundleRefs.TryGetValue(variantsName, out bundle);
            if (bundle != null)
            {
                bundle.m_ReferencedCount++;
                return true;
            }

            // 正在加载
            if (dicLoadings.ContainsKey(variantsName))
            {
                if (this.abRefCntLoadingDic.ContainsKey(variantsName))
                {
                    this.abRefCntLoadingDic[variantsName]++;
                }
                else
                {
                    this.abRefCntLoadingDic[variantsName] = 2;
                }

                return true;
            }
            
            // 加载的时候带着variants，但是保存的时候使用assetBundleName
            string path = this.FindPath(variantsName);
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path, 0);

            dicLoadings.Add(variantsName, request);

            return false;
        }

        /// <summary>
        /// 加载依赖项
        /// </summary>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private void LoadDependencies(string scope, string assetBundleName, string variants)
        {
            AssetBundleManifest manifest = this.GetPersistentManifest();
            string variantsName = this.GetVariants(assetBundleName, variants);
            string[] deps = manifest.GetAllDependencies(variantsName);
            if (deps.Length == 0)
            {
                return;
            }

            if (variants != "")
            {
                for (int i = 0; i < deps.Length; i ++)
                {
                    string itemVariantsName = this.GetVariants(deps[i], variants);
                    deps[i] = itemVariantsName;
                }
            }

            // Record and load all dependencies.
            dependencies.Add(variantsName, deps);

            for (int i = 0; i < deps.Length; i++)
            {
                // 这个abname已经是变体了
                string itemVariantsName = deps[i];

                // 加载依赖时，如果队列不足，要先等待
                if (dicLoadings.Count >= this._maxSize)
                {
                    // 需要等待
                    LoadTask task = new LoadTask(scope, itemVariantsName);
                    //LoadTask task = new LoadTask();
                    //task.scope = scope;
                    //task.name = itemVariantsName;
                    downList.Add(task);
                }
                else
                {
                    // 这个之后要获取
                    this.LoadAssetBundleInternal(itemVariantsName, "");
                }
            }
        }

        private void UnloadDependencies(string variantsName, bool v)
        {
            string[] deps = null;
            if (!dependencies.TryGetValue(variantsName, out deps))
            {
                return;
            }

            // Loop dependencies.
            foreach (var dependency in deps)
            {
                UnloadAssetBundleInternal(dependency, v);
            }

            dependencies.Remove(variantsName);
        }

        private void UnloadAssetBundleInternal(string variantsName, bool v)
        {
            AssetBundleRef abr = this.GetAssetBundleRef(variantsName);
            if (abr == null)
            {
                return;
            }

            abr.m_ReferencedCount--;
            if (abr.m_ReferencedCount == 0)
            {
                abr.assetBundle.Unload(v);
                abr.assetBundle = null;
                dictAssetBundleRefs.Remove(variantsName);

                Debug.Log(variantsName + " has been unloaded successfully");
            }
        }

        /// <summary>
        /// 获取一个ab
        /// </summary>
        /// <returns>The asset bundle.</returns>
        /// <param name="variantsName">这个参数已经加入了变体</param>
        private AssetBundleRef GetAssetBundleRef(string variantsName)
        {
            AssetBundleRef abRef;
            if (!dictAssetBundleRefs.TryGetValue(variantsName, out abRef))
            {
                return null;
            }

            // No dependencies are recorded, only the bundle itself is required.
            string[] deps = null;
            if (!dependencies.TryGetValue(variantsName, out deps))
            {
                return abRef;
            }

            // 如果依赖没有加载完成也无法获取
            foreach (var dependency in deps)
            {
                // Wait all the dependent assetBundles being loaded.
                AssetBundleRef dependentBundle;
                dictAssetBundleRefs.TryGetValue(dependency, out dependentBundle);
                if (dependentBundle == null)
                {
                    return null;
                }
            }

            return abRef;
        }

        /// <summary>
        /// 查找加载ab时的路径
        /// </summary>
        /// <returns>The path.</returns>
        /// <param name="assetBundleName">Asset bundle name.</param>
        private string FindPath(string assetBundleName) {
            if (this.peresistent_manifest == null)
            {
                return Utility.GetStreamingAssetsPath() + "/" + Utility.GetPlatformName() + "/" + assetBundleName;
            }

            Hash128 new_hash = this.peresistent_manifest.GetAssetBundleHash(assetBundleName);
            Hash128 old_hash = this.stream_manifest.GetAssetBundleHash(assetBundleName);

            if (new_hash.Equals(old_hash))
            {
                return Utility.GetStreamingAssetsPath() + "/" + Utility.GetPlatformName() + "/" + assetBundleName;
            }
            else
            {
                return Path.Combine(Utility.PersistentPath(), assetBundleName);
            }
        }

        /// <summary>
        /// 找到可用的manifest文件
        /// </summary>
        /// <returns>The persistent manifest.</returns>
        private AssetBundleManifest GetPersistentManifest() {
            if (this.peresistent_manifest != null)
            {
                return this.peresistent_manifest;
            }
            else
            {
                return this.stream_manifest;
            }
        }

		/**是否启用本地模拟*/
		public bool isSimulate{
			get{
				return SimulateAssetBundleInEditor;
			}
		}

        /// <summary>
        /// 获取变体
        /// </summary>
        /// <returns>The variants.</returns>
        private string GetVariants(string assetBundleName, string variants) {
            
            if ((variants == "") || (variants == null))
            {
                return assetBundleName;
            }
            else
            {
                if (this.bundlesWithVariant == null)
                {
                    AssetBundleManifest manifest = this.GetPersistentManifest();
                    bundlesWithVariant = manifest.GetAllAssetBundlesWithVariant();
                }

                string[] split = assetBundleName.Split ('.');

                string tmp = split[0] + "." + variants;
                int index = System.Array.IndexOf (bundlesWithVariant, tmp);

                if (index == -1)
                {
                    return assetBundleName;
                }
                else
                {
                    return tmp;
                }
            }
        }
    }
}
