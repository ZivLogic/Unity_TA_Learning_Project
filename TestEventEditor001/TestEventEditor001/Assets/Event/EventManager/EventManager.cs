using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace EventSystemV2
{
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }
        //static表示静态，代表这一个实例整个游戏只有一个，后面的括号get表示可引用；private set表示外部只可读不可改，内部可以改，合起来就是一个入口

        //事件监听字典
        private Dictionary<string, List<Action<object[]>>> listeners = new Dictionary<string, List<Action<object[]>>>();
        //list表示列表，存储订阅者信息，Action<object>表示任意对象，相当于可以把要接受事件的对象信息录用进去,但实际存的是方法

        //事件处理队列

        //逻辑队列
        private Queue<(string eventName, object[] data)> logicQueue = new Queue<(string, object[])>();

        //渲染队列
        private Queue<(string eventName, object[] data)> renderQueue = new Queue<(string, object[])>();

        //物理队列
        private Queue<(string eventName, object[] data)> physicsQueue = new Queue<(string, object[])>();

        //音频队列
        private Queue<(string eventName, object[] data)> audioQueue = new Queue<(string, object[])>();

        private Queue<(string eventName, object[] data)> inputQueue = new Queue<(string, object[])>();

        //拓展强类型队列
        private Dictionary<Type, List<Delegate>> typedListeners = new Dictionary<Type, List<Delegate>>();

        private Queue<PackageEvent> logicTypedQueue = new Queue<PackageEvent>();

        private Queue<PackageEvent> renderTypedQueue = new Queue<PackageEvent>();

        private Queue<PackageEvent> physicsTypedQueue = new Queue<PackageEvent>();

        private Queue<PackageEvent> audioTypedQueue = new Queue<PackageEvent>();

        private Queue<PackageEvent> inputTypedQueue = new Queue<PackageEvent>();

        private Queue<PackageEvent> networkTypedQueue = new Queue<PackageEvent>();


        private void Awake()
        {
            if (Instance == null)                            //如果不存在该实例，则创建
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);               //确保切换场景时不会销毁
            }
            else Destroy(gameObject);                        //如果实例已存在，后续增加的实例直接删除

            //先清空缓存
            EventGlobalCache.ClearAllMemorCache();

            //初始化全局缓存
            EventGlobalCache.ReloadAllFromDisk();

            //初始化系统层配置
            EventSystemManager.SysManageInit();

            //初始化生成代码实例化机制(基于全局缓存的配置加载)
            EventRuntimeInitializer.Initialize();
        }

        //订阅
        public void On(string eventName, Action<object[]> callback)
        {
            if (!listeners.ContainsKey(eventName))                      //如果没有这个事件
                listeners[eventName] = new List<Action<object[]>>();      //则添加事件信息，订阅者为事件接受对象
            listeners[eventName].Add(callback);                         //如果有这个事件，则添加订阅者信息
        }

        //取消订阅
        public void Off(string eventName, Action<object[]> callback)
        {
            if (listeners.TryGetValue(eventName, out var list))          //判断是否有这个事件，有则赋值给list，out表示赋值给外部数据
            {
                list.Remove(callback);                                  //消除订阅者信息
                if (list.Count == 0)                                    //如果该事件订阅者为零
                    listeners.Remove(eventName);                        //清处事件信息
            }
        }

        //强类型订阅拓展
        public void Listen<T>(Action<T> callback) where T : PackageEvent
        {
            Type type = typeof(T);
            if (!typedListeners.ContainsKey(type))
                typedListeners[type] = new List<Delegate>();
            typedListeners[type].Add(callback);
        }

        public void Unlisten<T>(Action<T> callback) where T : PackageEvent
        {
            Type type = typeof(T);
            if (typedListeners.TryGetValue(type, out var list))
            {
                list.Remove(callback);
                if (list.Count == 0)
                    typedListeners.Remove(type);
            }
        }


        //强类型派发
        public void EmitLogic<T>(T e) where T : PackageEvent => logicTypedQueue.Enqueue(e);
        public void EmitRender<T>(T e) where T : PackageEvent => renderTypedQueue.Enqueue(e);
        public void EmitPhysics<T>(T e) where T : PackageEvent => physicsTypedQueue.Enqueue(e);
        public void EmitAudio<T>(T e) where T : PackageEvent => audioTypedQueue.Enqueue(e);
        public void EmitInput<T>(T e) where T : PackageEvent => inputTypedQueue.Enqueue(e);
        public void EmitNetwork<T>(T e) where T : PackageEvent => networkTypedQueue.Enqueue(e);


        //派发事件
        private void Dispatch(string eventName, object[] data)
        {
            if (!listeners.TryGetValue(eventName, out var list)) return;         //如果不存在该事件，返回空
            foreach (var action in list)                                         //遍历订阅事件的对象
            {
                action?.Invoke(data);                                            //通知订阅者，？表示安全取值，如果action为空，则跳过。Invoke表示应用
            }
        }

        //强类型派发
        private void DispatchTyped(PackageEvent msg)
        {
            Type type = msg.GetType();
            if (!typedListeners.TryGetValue(type, out var list)) return;

            foreach (var action in list)
            {
                if (action is Delegate d)
                    d.DynamicInvoke(msg);
            }
        }

        //强类型迭代
        public IEnumerator ProcessLogic()
        {
            while (true)
            {
                // 旧事件
                while (logicQueue.Count > 0)
                {
                    var (name, data) = logicQueue.Dequeue();
                    Dispatch(name, data);
                }
                // 强类型事件
                while (logicTypedQueue.Count > 0)
                {
                    var msg = logicTypedQueue.Dequeue();
                    Debug.Log($"事件队列：存在事件{msg}");
                    DispatchTyped(msg);
                }
                yield return null;
            }
        }

        public IEnumerator ProcessRender()
        {
            while (true)
            {
                while (renderQueue.Count > 0)
                {
                    var (name, data) = renderQueue.Dequeue();
                    Dispatch(name, data);
                }
                while (renderTypedQueue.Count > 0)
                {
                    var msg = renderTypedQueue.Dequeue();
                    DispatchTyped(msg);
                }
                yield return new WaitForSeconds(0.016f);
            }
        }

        public IEnumerator ProcessPhysics()
        {
            while (true)
            {
                while (physicsQueue.Count > 0)
                {
                    var (name, data) = physicsQueue.Dequeue();
                    Dispatch(name, data);
                }
                while (physicsTypedQueue.Count > 0)
                {
                    var msg = physicsTypedQueue.Dequeue();
                    DispatchTyped(msg);
                }
                yield return new WaitForSeconds(0.01f);
            }
        }

        public IEnumerator ProcessAudio()
        {
            while (true)
            {
                while (audioQueue.Count > 0)
                {
                    var (name, data) = audioQueue.Dequeue();
                    Dispatch(name, data);
                }
                while (audioTypedQueue.Count > 0)
                {
                    var msg = audioTypedQueue.Dequeue();
                    DispatchTyped(msg);
                }
                yield return new WaitForSeconds(0.033f);
            }
        }

        public IEnumerator ProcessInput()
        {
            while (true)
            {
                while (inputQueue.Count > 0)
                {
                    var (name, data) = inputQueue.Dequeue();
                    Dispatch(name, data);
                }
                while (inputTypedQueue.Count > 0)
                {
                    var msg = inputTypedQueue.Dequeue();
                    DispatchTyped(msg);
                }
                yield return null;
            }
        }

        public IEnumerator ProcessNetwork()
        {
            while (true)
            {
                while (inputTypedQueue.Count > 0)
                {
                    var msg = networkTypedQueue.Dequeue();
                    DispatchTyped(msg);
                }
                yield return null;
            }
        }

        //主循环
        private void Start()
        {
            //启动主循环
            StartCoroutine(ProcessLogic());
            StartCoroutine(ProcessRender());
            StartCoroutine(ProcessPhysics());
            StartCoroutine(ProcessAudio());
            StartCoroutine(ProcessInput());
            StartCoroutine(ProcessNetwork());
        }

        // Update is called once per frame
        void Update()
        {

        }

    }
}

