using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFactory    //核心接口，所有工厂必须遵守
{
    //工厂名，用于中央工厂查找
    string FactoryName { get; }

    //初始化（注册自己到中央工厂）
    void Initialize();
}
