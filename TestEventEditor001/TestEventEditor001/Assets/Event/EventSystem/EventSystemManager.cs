using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventSystemV2
{
    public static class EventSystemManager
    {
        //定义配置容器
        private static SysIdConfigRoot SysIdConfig = new();
        private static EventConfigRoot EventConfig = new();

        //现有的所有ID
        private static Dictionary<string, SysIdItem> IsEnableID = new();
        private static Dictionary<string, SysIdItem> NoEnableID = new();

        //发布事件与系统ID的关系映射
        private static Dictionary<string, string> PublishEventDict = new();

        //监听事件与系统ID的关系映射
        private static Dictionary<string, string> ListenEventDict = new();

        //是否可发布
        private static Dictionary<PackageEvent, bool> CanPublishDict = new();
        //是否可监听
        private static Dictionary<PackageEvent, bool> CanListenDict = new();


        //初始化配置
        public static void SysManageInit()
        {
            //先清空
            ClearAllSysConfig();
            //再加载
            SysIdConfig = new SysIdConfigRoot();
            EventConfig = new EventConfigRoot();
            //加载配置
            SysIdConfig = EventConfigHelper.LoadSysConfig();
            EventConfig = EventConfigHelper.LoadEventConfig();

            foreach (var ID in SysIdConfig.ItemsIsEnable)
            {
                IsEnableID[ID.Value.SysId] = ID.Value;
                //Debug.Log($"启用的系统名：{ID.Value.SysId}");
            }
            foreach (var ID in SysIdConfig.ItemsNoEnable)
            {
                NoEnableID[ID.Value.SysId] = ID.Value;
            }
            foreach (var evt in EventConfig.Items)
            {
                foreach (var ID in IsEnableID)
                {
                    foreach (var kv in evt.Value.PublishSysId)
                    {
                        if (kv != ID.Key || !evt.Value.GlobalEnable)
                        { continue; }
                        PublishEventDict[evt.Value.EventClassFullName] = ID.Key;
                        Debug.Log($"系统层注册发布事件：{evt.Value.EventClassFullName}, 系统ID：{ID.Key}");
                    }
                }
                foreach (var ID in IsEnableID)
                {
                    foreach (var kv in evt.Value.ListenSysId)
                    {
                        if (kv != ID.Key || !evt.Value.GlobalEnable)
                        { continue; }
                        ListenEventDict[evt.Value.EventClassFullName] = ID.Key;
                        Debug.Log($"系统层注册监听事件：{evt.Value.EventClassFullName}, 系统ID：{ID.Key}");
                    }
                }
            }

        }

        //清空配置/缓存
        public static void ClearAllSysConfig()
        {
            SysIdConfig = null;
            EventConfig = null;
            IsEnableID.Clear();
            NoEnableID.Clear();
            PublishEventDict.Clear();
            ListenEventDict.Clear();
            CanPublishDict.Clear();
            CanListenDict.Clear();
        }

        //校验是否可发布
        public static bool CanPublish(PackageEvent evt)
        {
            //如果有缓存
            if (CanPublishDict.TryGetValue(evt, out bool canPublish))
                return canPublish;
            //没有缓存
            if (PublishEventDict.TryGetValue(evt.EventClassFullName, out var SysID))
            {
                if (IsEnableID.TryGetValue(SysID, out var value))
                {
                    if (value.PublishEvent.TryGetValue(evt.EventClassFullName, out bool publish))
                    {
                        //刷新缓存
                        CanPublishDict[evt] = publish;
                        return publish;
                    }
                }
            }
            //没有配置默认不发布
            return false;
        }

        //校验是否可监听
        public static bool CanListen(PackageEvent evt)
        {
            //如果有缓存
            if (CanListenDict.TryGetValue(evt, out bool canListen))
                return canListen;
            //没有缓存
            if (ListenEventDict.TryGetValue(evt.EventClassFullName, out var SysID))
            {
                if (IsEnableID.TryGetValue(SysID, out var value))
                {
                    if (value.ListenEvent.TryGetValue(evt.EventClassFullName, out bool listen))
                    {
                        //刷新缓存
                        CanListenDict[evt] = listen;
                        return listen;
                    }
                }
            }
            //没有配置默认不监听
            return false;
        }
    }
}
