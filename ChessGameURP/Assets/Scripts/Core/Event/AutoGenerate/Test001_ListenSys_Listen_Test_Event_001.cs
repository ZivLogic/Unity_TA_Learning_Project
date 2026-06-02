//AutoGen | 监听自动生成代码
        using System;
        public partial class Test001_ListenSys : BaseBusinessSystem
        {
            public override string SystemID => "Test001";

            [EventAutoGenerateEntryAttribute]
            private void Listen_Test_Event_001(PackageEvent baseEvt)
            {
                //全局开关拦截
                if(EventRouteRegistrar.EventEnableState.TryGetValue(baseEvt.GetType(),out bool enable) && !enable)
                    return;

                Test_Event_001Event evt = baseEvt as Test_Event_001Event;
                if(evt == null || evt.package == null) return;

                //按绑定从数据包取值，未绑定参数默认空
                System.Int32 test1 = evt.package.GetSafe<System.Int32>("test1");
        System.Int32 test2 = evt.package.GetSafe<System.Int32>("test2");

                //调用业务监听方法，out参数方法内部自行向外输出
                EventTest business = new EventTest();
                business.TestListenEvent(test1,test2);
             }
        }