using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

//事件窗口静态工具层
public static class EventEditorUtil
{
    //统一输出目录：Scripts/Core/Event/AutoGenerate/
    public const string AutoGenPath = "Assets/Scripts/Core/Event/AutoGenerate/";
    static EventEditorUtil()
    {
        if (!Directory.Exists(AutoGenPath))
            Directory.CreateDirectory(AutoGenPath);
    }
    #region 数据结构：单个方法参数拆分结果
    //拆分发布方法参数结果
    public class PublishParamSplitResult
    {
        public List<ParameterInfo> InArgs = new();     //外部注入入参（不打包）
        public List<ParameterInfo> OutArgs = new();    //out打包字段（进Package）
    }
    //拆分监听方法参数结果
    public class ListenParamSplitResult
    {
        public List<ParameterInfo> CanInjectArgs = new();  //可从包注入的普通入参
        public List<ParameterInfo> OutReturnArgs = new();  //向外输出out，不参与包映射
    }
    #endregion

    #region 1.全工程扫描带标记方法
    public static List<MethodInfo> GetAllPublishMethods()
    {
        List<MethodInfo> res = new List<MethodInfo>();
        Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in ass)
        {
            foreach (Type type in asm.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var m in methods)
                {
                    if (m.GetCustomAttribute<EventPublishMethodAttribute>() == null) continue;
                    res.Add(m);
                }
            }
        }
        return res;
    }

    public static List<MethodInfo> GetAllListenMethods()
    {
        List<MethodInfo> res = new List<MethodInfo>();
        Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in ass)
        {
            foreach (Type type in asm.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var m in methods)
                {
                    if (m.GetCustomAttribute<EventListenMethodAttribute>() == null) continue;
                    res.Add(m);
                }
            }
        }
        return res;
    }
    #endregion

    #region 2.参数拆分工具（核心：区分in/out,替代原来强校验）
    //拆分发布参数：入参（外部注入）/ out(打包进包)
    public static PublishParamSplitResult SplitPublishParam(MethodInfo pubMethod)
    {
        PublishParamSplitResult rst = new PublishParamSplitResult();
        foreach (var p in pubMethod.GetParameters())
        {
            if (p.IsOut)
                rst.OutArgs.Add(p);
            else 
                rst.InArgs.Add(p);
        }
        return rst;
    }
    //拆分监听参数：可注入普通入参/ out 输出参数
    public static ListenParamSplitResult SplitListenParam(MethodInfo lstMethod)
    {
        ListenParamSplitResult rst = new ListenParamSplitResult();
        foreach (var p in lstMethod.GetParameters())
        {
            if (p.IsOut || p.ParameterType.IsByRef)
                rst.OutReturnArgs.Add(p);
            else
                rst.CanInjectArgs.Add(p);
        }
        return rst;
    }
    #endregion

    #region 3.字段绑定类型安全校验（编辑器绑定阶段校验）
    ///mapItem:包Key配置;pkgValueType:打包存入的数据类型;paramType:监听参数类型
    public static bool CheckMapTypeSafe(FieldMapItem mapItem, Type pkgValueType, Type paramType)
    {
        //剥掉&引用
        Type pkgRaw = GetRawType(pkgValueType);
        Type paramRaw = GetRawType(paramType);
        if (pkgRaw == paramRaw) return true;
        EditorUtility.DisplayDialog("类型不匹配",
            $"数据包Key:{mapItem.packageKey}【{pkgValueType.Name}】→参数:{mapItem.paramName}【{paramType.Name}】类型不一致", "确定");
        return false;
    }
    #endregion

    #region 4.全套自动生成代码【补齐：事件类、Key常量、发布转发、监听转发全模板】
    ///一键生成四类代码：事件实体+静态Key+发布系统代码+监听系统代码
    ///sysPublishId：发布所属系统ID；sysListenId：监听所属系统ID
    public static void AutoGenAllCode(EventDefine evtDef,
        MethodInfo pubMethod, PublishParamSplitResult pubSplit, string pubSysId,
        MethodInfo lstMethod, ListenParamSplitResult lstSplit, string lstSysId,
        List<FieldMapItem> bindMaps)
    {
        //1.事件实体类
        GenEventClass(evtDef);
        //2.静态Key常量类
        GenKeyConstClass(evtDef);
        //3.发布System代码(打包+调用业务+AutoPublish入队列)
        GenPublishSystemCode(evtDef, pubMethod, pubSplit, pubSysId);
        //4.监听System代码(开关拦截+拆包+按绑定注入参数+调用业务+注册)
        GenListenSystemCode(evtDef, lstMethod, lstSplit, lstSysId, bindMaps);
        AssetDatabase.Refresh();
    }

    //生成事件基类实例
    private static void GenEventClass(EventDefine cfg)
    {
        string code = 
        $@"//AutoGen | 禁止手动修改
        public class {cfg.eventClassName} : PackageEvent
        {{
            public {cfg.eventClassName}()
            {{
                QueueType = EventQueueType.{cfg.queueType};
                package = new Package();
            }}
        }}";
        File.WriteAllText($"{AutoGenPath}{cfg.eventClassName}.cs", code);
    }

    //生成数据包Key静态常量
    private static void GenKeyConstClass(EventDefine cfg)
    {
        string keyCls = $"EventKey_{cfg.eventClassName}";
        StringBuilder sb = new StringBuilder();
        foreach (var key in cfg.packageKeys)
        {
            sb.AppendLine($"    public const string {key} = \"{key}\";");
        }
        string code = 
        $@"//AutoGen
        public static class {keyCls}
        {{
            {sb.ToString()}
        }}";
        File.WriteAllText($"{AutoGenPath}{keyCls}.cs", code);
    }

    //生成发布System代码：接收外部传入in实参→调用业务→out打包→发布入对应队列
    private static void GenPublishSystemCode(EventDefine cfg, MethodInfo pubMth, PublishParamSplitResult split, string sysId)
    {
        string clsName = $"{sysId}_PublishSys";
        string entryFuncName = $"Publish_{cfg.eventName}";
        string keyCls = $"EventKey_{cfg.eventClassName}";

        //拼接方法入参（全部普通in参数作为转发方法入参，由外部注入）
        List<string> inputParamStr = new List<string>();
        List<string> callArgStr = new List<string>();
        foreach (var p in split.InArgs)
        {
            inputParamStr.Add($"{p.ParameterType.FullName} {p.Name}");
            callArgStr.Add(p.Name);
        }
        string paramDecl = inputParamStr.Count > 0 ? string.Join(",", inputParamStr) : "";
        string callArg = callArgStr.Count > 0 ? string.Join(",", callArgStr) : "";

        //out打包逻辑
        List<string> putCode = new List<string>();
        foreach (var outP in split.OutArgs)
        {
            putCode.Add($"evt.package.Put({keyCls}.{outP.Name}, {outP.Name});");
        }
        string inArgStr = callArgStr.Count > 0 ? string.Join(",", callArgStr) : "";
        string outArgStr = split.OutArgs.Count > 0 ? GenOutCallArg(split.OutArgs) : "";

        List<string> callAll = new List<string>();
        if (!string.IsNullOrWhiteSpace(inArgStr)) callAll.Add(inArgStr);
        if (!string.IsNullOrWhiteSpace(outArgStr)) callAll.Add(outArgStr);

        string fullCall = string.Join(",", callAll);

        string code = 
        $@"//AutoGen | 发布自动生成代码
        using System;
        public partial class {clsName} : BasePublishSystem
        {{
            public override string SystemID => ""{sysId}"";

            [EventAutoGenerateEntryAttribute]
            public void {entryFuncName}({paramDecl})
            {{
                //实例化业务类
                {pubMth.DeclaringType.FullName} business = new {pubMth.DeclaringType.FullName}();
                //定义out变量
                {GenOutVarDefine(split.OutArgs)}
                //调用业务方法，外部入参传入
                business.{pubMth.Name}({fullCall});

                //构造事件+打包
                {cfg.eventClassName} evt = new {cfg.eventClassName}();
                {string.Join("\r\n  ",putCode)}

                //推入对应队列
                AutoPublish(evt);
            }}
        }}";
        File.WriteAllText($"{AutoGenPath}{clsName}_{entryFuncName}.cs", code);
    }

    //辅助：out变量定义
    private static string GenOutVarDefine(List<ParameterInfo> outs)
    {
        List<string> list = new List<string>();
        foreach (var p in outs)
        {
            Type raw = GetRawType(p.ParameterType);
            list.Add($"{raw.FullName} {p.Name} = default;");
        }
        return string.Join("\r\n        ", list);
    }
    //辅助：out实参调用
    private static string GenOutCallArg(List<ParameterInfo> outs)
    {
        List<string> list = new List<string>();
        foreach (var p in outs) list.Add($"out {p.Name}");
        return string.Join(",", list);
    }

    //剥离ref/out引用，获取原生类型
    private static Type GetRawType(Type t)
    {
        if (t.IsByRef) return t.GetElementType();
        return t;
    }


    //生成监听System代码：开关拦截→拆包→按绑定映射填充可选参数→调用业务
    public static void GenListenSystemCode(EventDefine cfg, MethodInfo lstMth, ListenParamSplitResult split, string sysId, List<FieldMapItem> maps)
    {
        string clsName = $"{sysId}_ListenSys";
        string entryFunc = $"Listen_{cfg.eventName}";
        string evtType = cfg.eventClassName;

        //根据绑定映射，从包取值
        List<string> paramVarCode = new List<string>();
        Dictionary<string, FieldMapItem> paramBindDict = new Dictionary<string, FieldMapItem>();
        foreach (var map in maps) paramBindDict[map.paramName] = map;

        List<string> invokeArgs = new List<string>();
        foreach (var arg in split.CanInjectArgs)
        {
            if (paramBindDict.ContainsKey(arg.Name))
            {
                var map = paramBindDict[arg.Name];
                //需要从包注入
                paramVarCode.Add($"{arg.ParameterType.FullName} {arg.Name} = evt.package.GetSafe<{arg.ParameterType.FullName}>(\"{map.packageKey}\");");
                invokeArgs.Add(arg.Name);
            }
            else
            {
                //未勾选注入，默认构造空值
                paramVarCode.Add($"{arg.ParameterType.FullName} {arg.Name} = default;");
                invokeArgs.Add(arg.Name);
            }
        }
        //out参数直接忽略，不做处理
        string varBody = string.Join("\r\n        ", paramVarCode);
        string invokeArgStr = string.Join(",", invokeArgs);

        string code = 
        $@"//AutoGen | 监听自动生成代码
        using System;
        public partial class {clsName} : BaseBusinessSystem
        {{
            public override string SystemID => ""{sysId}"";

            [EventAutoGenerateEntryAttribute]
            private void {entryFunc}(PackageEvent baseEvt)
            {{
                //全局开关拦截
                if(EventRouteRegistrar.EventEnableState.TryGetValue(baseEvt.GetType(),out bool enable) && !enable)
                    return;

                {evtType} evt = baseEvt as {evtType};
                if(evt == null || evt.package == null) return;

                //按绑定从数据包取值，未绑定参数默认空
                {varBody}

                //调用业务监听方法，out参数方法内部自行向外输出
                {lstMth.DeclaringType.FullName} business = new {lstMth.DeclaringType.FullName}();
                business.{lstMth.Name}({invokeArgStr});
             }}
        }}";
        File.WriteAllText($"{AutoGenPath}{clsName}_{entryFunc}.cs", code);
    }
    #endregion
}
