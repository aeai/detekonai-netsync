using Detekonai.Core.Common;
using Detekonai.Networking.NetSync.Runtime;
using NSubstitute;
using NUnit.Framework;
using System;

namespace Detekonai.Networking.NetSync.Injector.Tests.Editor
{
    class NetSyncInjectorTest
    {

        class AwsomeReferenceObject
        {
            public string AThing { get; set; } = "initialValue";
        }

        [NetSync(Name = "CoolNameSyncObject")]
        class BadNameSyncObject
        {
            public INetworkInterceptor NetSyncInterceptor { get; set; }

            public int IntValue { get; set; }
        }

        class TestNetSyncObject
        {
            public INetworkInterceptor NetSyncInterceptor { get; set; }

            public int IntValue { get; set; }
            public AwsomeReferenceObject ObjValue { get; set; } = new AwsomeReferenceObject();
            
            [NetSyncIgnore]
            public string IgnoreMeProperty { get; set; }
            
            public string GetterOnlyProperty => "alma";

            public string GetterOnlyProperty2 { get; } = "korte";

            private string back;

            public string BackedProperty
            {
                get
                {
                    return back;
                }
                set
                {
                    back = value;
                }
            }

            private int trick = 0;
            public int TrickyProperty
            {
                get
                {
                    return trick;
                }
                set
                {
                    if(value % 2 == 0)
                    {
                        trick = value;
                    }
                    else
                    {
                        trick = value * 2;
                    }
                }
            }

            [NetSync]
            public void DoStuff() 
            {
                back = "nope";
            }

            [NetSync]
            public void DoComplexStuff(int arg)
            {
                back = "nope";
                switch (arg)
                {
                    case 1:
                        back = "one";
                    break;
                    case 2:
                        back = "two";
                        break;
                    case 3:
                        back = "three";
                        break;
                    default:
                        back = arg.ToString();
                        break;
                }
            }

            [NetSync]
            public void DoStuffWithOneParam(string arg)
            {
                back = arg;
            }
            [NetSync]
            public void DoStuffWithMultiParam(string arg1, string arg2, string arg3)
            {
                back = arg1 + arg2 + arg3;
            }

            [NetSync(Name = "StuffWithInt")]
            public void DoStuffWithOneParam(int intParam)
            {
                back = "int"+intParam;
            }

            [NetSync]
            public int Add(int a, int b)
            {
                return a + b;
            }
        }


        [Test]
        public void Inject_to_property_works_with_value_type()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.IntValue = 5;

            interceptor.Received().WriteValue("TestNetSyncObject", "IntValue", 5);
        }

        [Test]
        public void Inject_to_property_dont_break_without_valid_interceptor()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();

            Assert.DoesNotThrow(() => testObject.IntValue = 5);
        }

        [Test]
        public void Inject_to_function_dont_break_without_valid_interceptor()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();

            Assert.DoesNotThrow(() => testObject.DoStuffWithOneParam(5));
        }

        [Test]
        public void Injector_respect_class_alias()
        {
            BadNameSyncObject testObject = new BadNameSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.IntValue = 5;

            interceptor.Received().WriteValue("CoolNameSyncObject", "IntValue", 5);
        }

        [Test]
        public void Inject_to_property_works_with_explicit_setters()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.BackedProperty = "a new value";

            interceptor.Received().WriteValue("TestNetSyncObject", "BackedProperty", "a new value");
            Assert.That(testObject.BackedProperty, Is.EqualTo("a new value"));
        }

        [Test]
        public void Inject_to_property_works_with_explicit_complex_setters()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.TrickyProperty = 2;

            interceptor.Received().WriteValue("TestNetSyncObject", "TrickyProperty", 2);
            Assert.That(testObject.TrickyProperty, Is.EqualTo(2));
            testObject.TrickyProperty = 3;
            interceptor.Received().WriteValue("TestNetSyncObject", "TrickyProperty", 3);
            Assert.That(testObject.TrickyProperty, Is.EqualTo(6));
        }

        [Test]
        public void Inject_to_property_works_with_reference_type()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;
            var newValue = new AwsomeReferenceObject() { AThing = "bla" };
            testObject.ObjValue = newValue;

            interceptor.ReceivedWithAnyArgs(1).WriteValue(default, default, default);
            interceptor.Received().WriteValue("TestNetSyncObject", "ObjValue", newValue);
        }

        [Test]
        public void Injector_ignores_properties_marked_with_ignore()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;
           
            testObject.IgnoreMeProperty = "cool";

            interceptor.ReceivedWithAnyArgs(0).WriteValue(default, default, default);
        }

        [Test]
        public void Inject_to_marked_parameterless_function_works()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.DoStuff();

            interceptor.ReceivedWithAnyArgs(1).CallFunction("TestNetSyncObject", "DoStuff", Array.Empty<object>());
            Assert.That(testObject.BackedProperty, Is.EqualTo("nope"));
        }

        [Test]
        public void Inject_to_marked_function_with_return_value_works()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            int res = testObject.Add(5, 3);

            interceptor.ReceivedWithAnyArgs(1).CallFunction("TestNetSyncObject", "Add", new object[] { 5, 3 });
            Assert.That(res, Is.EqualTo(8));
        }

        [Test]
        public void Inject_to_marked_one_param_function_works()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.DoStuffWithOneParam("importantStuff");

            interceptor.ReceivedWithAnyArgs(1).CallFunction("TestNetSyncObject", "DoStuff", new object[] { "importantStuff" });
            Assert.That(testObject.BackedProperty, Is.EqualTo("importantStuff"));
        }

        [Test]
        public void Inject_to_marked_multi_param_function_works()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.DoStuffWithMultiParam("importantStuff"," can't ","be done alone");

            interceptor.ReceivedWithAnyArgs(1).CallFunction("TestNetSyncObject", "DoStuff", new object[] { "importantStuff", " can't ", "be done alone" });
            Assert.That(testObject.BackedProperty, Is.EqualTo("importantStuff can't be done alone"));
        }

        [Test]
        public void Inject_to_marked_complex_function_works()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.DoComplexStuff(2);

            interceptor.ReceivedWithAnyArgs(1).CallFunction("TestNetSyncObject", "DoComplexStuff", new object[] { 2 });
            Assert.That(testObject.BackedProperty, Is.EqualTo("two"));
        }

        [Test]
        public void Injector_properly_use_method_alias()
        {
            TestNetSyncObject testObject = new TestNetSyncObject();
            var logger = Substitute.For<ILogger>();
            INetworkInterceptor interceptor = Substitute.For<INetworkInterceptor>();
            testObject.NetSyncInterceptor = interceptor;

            testObject.DoStuffWithOneParam(12);

            interceptor.ReceivedWithAnyArgs(1).CallFunction("TestNetSyncObject", "StuffWithInt", new object[] { 12 });
            Assert.That(testObject.BackedProperty, Is.EqualTo("int12"));
        }
    }
}
