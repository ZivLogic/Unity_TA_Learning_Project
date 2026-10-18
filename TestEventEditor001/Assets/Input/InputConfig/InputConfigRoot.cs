using System;

//输入配置顶级聚合容器
//不继承 InputConfigDefinition，只做打包
[Serializable]
public class InputConfigRoot
{
    public InputGlobalConfig GlobalConfig;
    public InputBindContainer BindContainer;
    public InputInterceptorContainer InterceptorContainer;
    public InputStateContainer StateContainer;
    public InputEventMapContainer EventMapContainer;
    public InputIntentContainer IntentContainer;
    public InputAreaModelContainer AreaModelContainer;
    public InputScopeContainer ScopeContainer;


    //确保所有子容器非空
    public void EnsureNotNull()
    {
        GlobalConfig ??= new InputGlobalConfig();
        BindContainer ??= new InputBindContainer();
        InterceptorContainer ??= new InputInterceptorContainer();
        StateContainer ??= new InputStateContainer();
        EventMapContainer ??= new InputEventMapContainer();
        IntentContainer ??= new InputIntentContainer();
        AreaModelContainer ??= new InputAreaModelContainer();
        ScopeContainer ??= new InputScopeContainer();
    }
}