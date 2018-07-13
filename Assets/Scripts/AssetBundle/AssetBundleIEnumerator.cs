using UnityEngine;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

namespace Virivers
{
    
    public abstract class AssetBundleOperation : IEnumerator
    {
        public object Current
        {
            get
            {
                return null;
            }
        }
        public bool MoveNext()
        {
            return !IsDone();
        }

        public void Reset()
        {
        }

        abstract public bool Update ();

        abstract public bool IsDone ();
    }

    /// <summary>
    /// 下载时使用
    /// TODO
    /// </summary>
    public class AssetBundleDownLoadOperation : AssetBundleOperation {

        /// <summary>
        /// 是否有错误
        /// </summary>
        public bool isError = false;

        public string error;

        public override bool Update ()
        {
            return false;
        }

        public override bool IsDone ()
        {       
            return AssetDownLoadManager.Instance.IsDone;
        }
    }

    /// <summary>
    /// 加载的基础累
    /// </summary>
    public abstract class AssetBundleLoadAssetOperation : AssetBundleOperation
    {
        public abstract T GetAsset<T>() where T : UnityEngine.Object;
    }

    #if UNITY_EDITOR
    /// <summary>
    /// 模拟加载场景
    /// </summary>
    public class AssetBundleLoadSceneSimulationOperation : AssetBundleOperation
    {
        AsyncOperation m_Operation = null;

        private Action<string> _callBack;

        private string _assetBundleName;

        public AssetBundleLoadSceneSimulationOperation (string assetBundleName, string levelName, bool isAdditive, Action<string> callBack)
        {
            this._callBack = callBack;
            this._assetBundleName = assetBundleName;

            string[] levelPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, levelName);
            if (levelPaths.Length == 0)
            {
                ///@TODO: The error needs to differentiate that an asset bundle name doesn't exist
                //        from that there right scene does not exist in the asset bundle...

                Debug.LogError("There is no scene with name \"" + levelName + "\" in " + assetBundleName);
                return;
            }

            if (isAdditive)
            {
                m_Operation = UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(levelPaths[0]);
            }
            else
            {
                m_Operation = UnityEditor.EditorApplication.LoadLevelAsyncInPlayMode(levelPaths[0]);
            }
        }

        public override bool Update ()
        {
            return false;
        }

        public override bool IsDone ()
        {       
            if (m_Operation != null &&
                m_Operation.isDone &&
                (this._callBack != null))
            {
                this._callBack(this._assetBundleName);
            }
            return m_Operation == null || m_Operation.isDone;
        }
    }
    #endif

    /// <summary>
    /// 模拟加载一个ab
    /// </summary>
    public class AssetBundleLoadAssetOperationSimulation : AssetBundleLoadAssetOperation
    {
        private UnityEngine.Object m_SimulatedObject;

        public AssetBundleLoadAssetOperationSimulation (UnityEngine.Object simulatedObject)
        {
            m_SimulatedObject = simulatedObject;
        }

        public override T GetAsset<T>()
        {
            return m_SimulatedObject as T;
        }

        public override bool Update ()
        {
            return false;
        }

        public override bool IsDone ()
        {
            return true;
        }
    }

    /// <summary>
    /// 单个加载ab使用
    /// </summary>
    public class AssetBundleLoadAssetOperationSingle : AssetBundleLoadAssetOperation {

        private string _assetBundleName;

        private string _assetName;

        private Action<string, string> _callBack;

        private System.Type _type;

        private AssetBundleRequest _request = null;

        private string _scopeName;

        private Action<string,string,string,Action<string, string>> _baseCallBack;
        public AssetBundleLoadAssetOperationSingle(string scopeName,string abName, string assetName, Action<string, string> callBack, System.Type type,Action<string,string,string,Action<string,string>> baseCallBack) {

            this._assetBundleName = abName;
            this._assetName = assetName;
            this._callBack = callBack;
            this._type = type;
            _scopeName = scopeName;
            _baseCallBack = baseCallBack;
        }

        public override T GetAsset<T>()
        {
            if (_request != null && _request.isDone)
            {
                return _request.asset as T;
            }
            else
            {
                return null;
            }
        }

        // Returns true if more Update calls are required.
        public override bool Update ()
        {
            if (_request != null)
            {
                return false;
            }

            AssetBundle bundle = AssetBundleManager.Instance.GetAssetBundle(this._assetBundleName);
            if (bundle != null)
            {
                if (this._baseCallBack != null)
                {
                    this._baseCallBack(_scopeName,this._assetBundleName, this._assetName,_callBack);
                }
                _request = bundle.LoadAssetAsync(this._assetName, this._type);
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool IsDone ()
        {
            if (_request == null)
            {
                return false;
            }

            return _request.isDone;
        }
    }

    /// <summary>
    /// 批量加载完成
    /// 这个需要用户自己初始化
    /// </summary>
    public class AssetBundleLoadAssetOperationBatch : AssetBundleOperation {

        private Action _callBack;

        public AssetBundleLoadAssetOperationBatch(Action callBack) {
            this._callBack = callBack;
        }

        // Returns true if more Update calls are required.
        public override bool Update ()
        {
            return false;
        }

        public override bool IsDone ()
        {
			int count = AssetBundleManager.Instance.downList.Count + AssetBundleManager.Instance.dicLoadings.Count;
            if ((this._callBack != null) && (count == 0))
            {
                this._callBack();
            }

            // 加载任务全部完成
			return count == 0;
        }
    }

    /// <summary>
    /// 批量加载完成item
    /// </summary>
    public class AssetBundleLoadAssetOperationBatchItem : AssetBundleOperation {

        private string _assetBundleName;

        private Action<string> _callBack;

        private AssetBundle bundle;

        private string _scopeName;

        private Action<string, string, Action<string>> _baseCallBack;

        public AssetBundleLoadAssetOperationBatchItem(string scopeName,string assetBundleName, Action<string> callBack,Action<string,string,Action<string>> baseCallBack) {
            this._assetBundleName = assetBundleName;
            this._callBack = callBack;
            _baseCallBack = baseCallBack;
            _scopeName = scopeName;
        }

        // Returns true if more Update calls are required.
        public override bool Update ()
        {
            if (bundle != null)
            {
                return false;
            }

            bundle = AssetBundleManager.Instance.GetAssetBundle(this._assetBundleName);
            if (bundle != null)
            {
                // 执行回调
                if (this._baseCallBack != null)
                {
                    this._baseCallBack(_scopeName,this._assetBundleName,_callBack);
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool IsDone ()
        {
            if (bundle == null)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 加载场景使用
    /// </summary>
    public class AssetBundleLoadSceneOperation : AssetBundleOperation
    {
        private string                _assetBundleName;
        private string                _levelName;
        private bool                  _isAdditive;
        private AsyncOperation        _request;
        private Action<string> _callBack;
        private string _scopeName;
        Action<string, string, Action<string>> _baseCallBack;

        public AssetBundleLoadSceneOperation (string scopeName, string assetbundleName, string levelName, bool isAdditive, Action<string> callBack,Action<string, string, Action<string>> baseCallBack )
        {
            _assetBundleName = assetbundleName;
            _levelName = levelName;
            _isAdditive = isAdditive;
            _callBack = callBack;
            _scopeName = scopeName;
            _baseCallBack = baseCallBack;
        }

        public override bool Update ()
        {
            if (_request != null)
            {
                return false;
            }

            AssetBundle bundle = AssetBundleManager.Instance.GetAssetBundle(_assetBundleName);
            if (bundle != null)
            {
                if (_isAdditive)
                {
                    _request = SceneManager.LoadSceneAsync(this._levelName, LoadSceneMode.Additive);
                }
                else
                {
                    _request = SceneManager.LoadSceneAsync(this._levelName, LoadSceneMode.Single);
                }

                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool IsDone ()
        {
            // Return if meeting downloading error.
            // m_DownloadingError might come from the dependency downloading.
            if (_request == null)
            {
                return false;
            }

            // 如果要使用回调函数，那么就要使用StartCoroutine(AssetBundleLoadSceneOperation)的方式
            if (_request.isDone && this._baseCallBack != null)
            {
                this._baseCallBack(_scopeName,this._assetBundleName,_callBack);
            }

            return _request != null && _request.isDone;
        }
    }
}