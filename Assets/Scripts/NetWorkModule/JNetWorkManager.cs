
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using VFrame.ABSystem;


public class JNetWorkManager : MonoBehaviour
{
    void Start()
    {
        string dir = string.Format("file://{0}/{1}", Application.streamingAssetsPath, "AssetBundles/Cube");
        Debug.Log(dir);
        //  StartCoroutine(LoadFromFileAsync(Application.streamingAssetsPath+ "/AssetBundles/Cube"));
 
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            AssetBundleManager.Instance.Init(null);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            AssetBundleManager.Instance.Load("Cube", (a) =>
            {
                GameObject prefab = a.mainObject as GameObject;
                GameObject.Instantiate(prefab);
            });
        }
    }

    IEnumerator LoadFromFileAsync(string dir)
    {
        var bundleLoadRequest = AssetBundle.LoadFromFileAsync(dir);
        yield return bundleLoadRequest;

        var myLoadedAssetBundle = bundleLoadRequest.assetBundle;
        if (myLoadedAssetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle!");
            yield break;
        }

        var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("Cube");
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.asset as GameObject;
        Instantiate(prefab);

        myLoadedAssetBundle.Unload(false);
    }

    IEnumerator LoadAssetBundle(string dir)
    {
        
        var uwr = UnityWebRequest.GetAssetBundle(dir);
        yield return uwr.SendWebRequest();
        GameObject c = DownloadHandlerAssetBundle.GetContent(uwr).LoadAsset<GameObject>("Cube");
        
        // Get the designated main asset and instantiate it.
        Instantiate(c);
    }


    private IEnumerator SendUrl(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.error != null)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                Debug.Log(www.downloadHandler.text);

                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
            }
        }
    }
    IEnumerator GetText(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log(www.downloadHandler.text);

            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;
        }
    }
}