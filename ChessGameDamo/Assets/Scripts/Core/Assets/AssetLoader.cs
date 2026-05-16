using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

internal class AssetLoader      //资源加载器，负责具体的加载操作
{
    //同步加载
    public T Load<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"[AssetLoader]资源路径为空！");
            return null;
        } 
        
        T asset = Resources.Load<T>(path);

        if (asset == null)
        {
            Debug.LogError($"[AssetLoader]加载失败，路径不存在：{path}");

        }
        return asset;
    }

    //同步加载文件夹下的所有资源
    public T[] LoadAll<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty (path))
        {
            Debug.LogError("[AssetLoader]路径为空");
            return Array.Empty<T>();
        }
        T[] assets = Resources.LoadAll<T>(path);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError($"[AssetLoader]LoadAll无资源：{path}");
        }
        return assets ?? Array.Empty<T>();
    }

    //异步加载
    public void LoadAsync<T>(string path, System.Action<T> onLoaded) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"[AssetLoader]资源路径为空！");
            onLoaded?.Invoke(null);
            return;
        }

        ResourceRequest op = Resources.LoadAsync<T>(path);

        //异步加载完成完成回溯
        op.completed += (asyncOp) =>
        {
            T asset = (asyncOp as ResourceRequest).asset as T;
            if (asset == null)
            {
                Debug.LogError($"[AssetLoader]异步加载失败，路径不存在：{path}");
            }
            onLoaded?.Invoke(asset);
        };

    }

    //异步加载文件夹下的所有资源
    public void LoadAllAsyns<T>(string path, System.Action<T[]> onLoaded) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("[AssetLoader]路径为空");
            onLoaded?.Invoke(Array.Empty<T>());
            return;
        }
        //创建一个隐藏的GameObject跑协程
        GameObject go = new GameObject("LoadAllAsyncRunner");
        go.hideFlags = HideFlags.HideAndDontSave;
        CoroutineRunner runner = go.AddComponent<CoroutineRunner>();
        //启动协程
        runner.StartCoroutine(LoadAllCoroutine(path, onLoaded, go));
    }

    //异步加载文件夹协程
    private IEnumerator LoadAllCoroutine<T>(string path, Action<T[]> onLoaded, GameObject runnerGo) where T : Object
    {
        //这里会帧间隙执行，避免卡顿
        yield return null;
        T[] assets = Resources.LoadAll<T>(path);
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError($"[AssetLoader]LoadAllAsync 无资源：{path}");
        }
        onLoaded?.Invoke(assets ?? Array.Empty<T>());
        //销毁临时对象
        Object.DestroyImmediate(runnerGo);
    }

    //卸载单个资源
    public void Unload(string path)
    {
        //注意：UnloadAsset只卸载从Resources加载的原生资源
        //现在只简单处理，未来想用Addressable可以在这里改逻辑
        Resources.UnloadAsset(Resources.Load(path));
    }

    //卸载所有未使用的资源
    public void UnloadUnused()
    {
        Resources.UnloadUnusedAssets();
    }

    //卸载所有资源
    public void UnloadAll()
    {
        Resources.UnloadUnusedAssets();
    }
}