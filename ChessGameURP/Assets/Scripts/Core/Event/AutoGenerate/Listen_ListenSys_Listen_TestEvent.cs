//AutoGen | 监听自动生成代码
        using System;
        public partial class Listen_ListenSys : BaseBusinessSystem
        {
            public override string SystemID => "Listen";

            [EventAutoGenerateEntryAttribute]
            private void Listen_TestEvent(PackageEvent baseEvt)
            {
                //全局开关拦截
                if(EventRouteRegistrar.EventEnableState.TryGetValue(baseEvt.GetType(),out bool enable) && !enable)
                    return;

                TestEventEvent evt = baseEvt as TestEventEvent;
                if(evt == null || evt.package == null) return;

                //按绑定从数据包取值，未绑定参数默认空
                System.Int32 test1 = default;
        System.Int32 test2 = default;

                //调用业务监听方法，out参数方法内部自行向外输出
                EventTest business = new EventTest();
                business.TestListenEvent(test1,test2);
             }
        }