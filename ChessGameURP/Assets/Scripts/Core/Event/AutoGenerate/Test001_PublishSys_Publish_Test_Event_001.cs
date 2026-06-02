//AutoGen | 发布自动生成代码
        using System;
        public partial class Test001_PublishSys : BasePublishSystem
        {
            public override string SystemID => "Test001";

            [EventAutoGenerateEntryAttribute]
            public void Publish_Test_Event_001()
            {
                //实例化业务类
                EventTest business = new EventTest();
                //定义out变量
                System.Int32 test1 = default;
        System.Int32 test2 = default;
                //调用业务方法，外部入参传入
                business.TestPublishEvent(out test1,out test2);

                //构造事件+打包
                Test_Event_001Event evt = new Test_Event_001Event();
                evt.package.Put(EventKey_Test_Event_001Event.test1, test1);
  evt.package.Put(EventKey_Test_Event_001Event.test2, test2);

                //推入对应队列
                AutoPublish(evt);
            }
        }