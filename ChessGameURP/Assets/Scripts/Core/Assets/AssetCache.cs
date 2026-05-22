using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class AssetCache    //资源缓存器，专门处理缓存
{
    //全局缓存字典(单个资源)
    private Dictionary<string, Object> _assetDict = new Dictionary<string, Object>();
    //文件夹资源数组缓存(专门存LoadAll结果)
    private Dictionary<string, Object[]> _folderCache = new Dictionary<string, Object[]>();


    //获取缓存
    public T Get<T>(string path) where T : Object
    {
        if (_assetDict.TryGetValue(path,out Object asset))
        {
            return asset as T;
        }
        return null;
    }

    //添加缓存
    public void Add(string path,Object asset)
    {
        if (string.IsNullOrEmpty(path) || asset == null)
            return;
        if(!_assetDict.ContainsKey(path))
        {
            _assetDict.Add(path, asset);
        }
    }

    //移除缓存
    public void Remove(string path)
    {
        if (_assetDict.ContainsKey(path))
        {
            _assetDict.Remove(path);
        }
    }

    //文件夹数组
    public T[] GetFolderAssets<T>(string folderPath) where T : Object
    {
        if (_folderCache.TryGetValue(folderPath,out var assets))
        {
            return assets as T[];
        }
        return null;
    }    

    public void AddFolderAssets(string folderPath, Object[] assets)
    {
        if (string.IsNullOrEmpty(folderPath) || assets == null)
            return;
        if(!_folderCache.ContainsKey(folderPath))
        {
            _folderCache.Add(folderPath, assets);
        }
    }

    //清理未使用资源
    public void ClearUnused()
    {
        //这里可以做更复杂的引用计数逻辑
        //简单版本，只要在字典里都是需要的
        //复杂版本，可以加引用计数，当计数为零时才卸载
    }

    //清理所有缓存
    public void ClearAll()
    {
        _assetDict.Clear();
    }
}
