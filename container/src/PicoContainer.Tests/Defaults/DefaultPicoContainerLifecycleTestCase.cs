using System;
using System.Collections;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace PicoContainer.Defaults
{
    [TestFixture]
    public class DefaultPicoContainerLifecycleTestCase
    {
        public class One : IStartable, IDisposable
        {
            private IList disposing = new ArrayList();
            private IList instantiating = new ArrayList();
            private IList starting = new ArrayList();
            private IList stopping = new ArrayList();

            public One()
            {
                instantiation("One");
            }

            #region IDisposable Members

            public void Dispose()
            {
                disposeCalled("One");
            }

            #endregion

            #region IStartable Members

            public void Start()
            {
                startCalled("One");
            }

            public void Stop()
            {
                stopCalled("One");
            }

            #endregion

            public void instantiation(String s)
            {
                instantiating.Add(s);
            }

            public IList getInstantiating()
            {
                return instantiating;
            }

            public IList getStarting()
            {
                return starting;
            }

            public IList getStopping()
            {
                return stopping;
            }

            public IList getDisposing()
            {
                return disposing;
            }

            public void startCalled(String msg)
            {
                starting.Add(msg);
            }

            public void stopCalled(String msg)
            {
                stopping.Add(msg);
            }

            public void disposeCalled(String msg)
            {
                disposing.Add(msg);
            }
        }

        public class Two : IStartable, IDisposable
        {
            private One one;

            public Two(One one)
            {
                one.instantiation("Two");
                this.one = one;
            }

            #region IDisposable Members

            public void Dispose()
            {
                one.disposeCalled("Two");
            }

            #endregion

            #region IStartable Members

            public void Start()
            {
                one.startCalled("Two");
            }

            public void Stop()
            {
                one.stopCalled("Two");
            }

            #endregion
        }

        public class Three : IStartable, IDisposable
        {
            private One one;

            public Three(One one, Two two)
            {
                one.instantiation("Three");
                Assert.IsNotNull(two);
                this.one = one;
            }

            #region IDisposable Members

            public void Dispose()
            {
                one.disposeCalled("Three");
            }

            #endregion

            #region IStartable Members

            public void Start()
            {
                one.startCalled("Three");
            }

            public void Stop()
            {
                one.stopCalled("Three");
            }

            #endregion
        }

        public class Four : IStartable, IDisposable
        {
            private One one;

            public Four(Two two, Three three, One one)
            {
                one.instantiation("Four");
                Assert.IsNotNull(two);
                Assert.IsNotNull(three);
                this.one = one;
            }

            #region IDisposable Members

            public void Dispose()
            {
                one.disposeCalled("Four");
            }

            #endregion

            #region IStartable Members

            public void Start()
            {
                one.startCalled("Four");
            }

            public void Stop()
            {
                one.stopCalled("Four");
            }

            #endregion
        }

        private void StartStopDisposeLifecycleComps(IStartable start, IStartable stop, IDisposable disp, One one)
        {
            start.Start();

            // post instantiation startup
            Assert.AreEqual(4, one.getStarting().Count);
            Assert.AreEqual("One", one.getStarting()[0]);
            Assert.AreEqual("Two", one.getStarting()[1]);
            Assert.AreEqual("Three", one.getStarting()[2]);
            Assert.AreEqual("Four", one.getStarting()[3]);

            stop.Stop();

            // post instantiation shutdown - REVERSE order.
            Assert.AreEqual(4, one.getStopping().Count);
            Assert.AreEqual("Four", one.getStopping()[0]);
            Assert.AreEqual("Three", one.getStopping()[1]);
            Assert.AreEqual("Two", one.getStopping()[2]);
            Assert.AreEqual("One", one.getStopping()[3]);

            disp.Dispose();

            // post instantiation shutdown - REVERSE order.
            Assert.AreEqual(4, one.getDisposing().Count);
            Assert.AreEqual("Four", one.getDisposing()[0]);
            Assert.AreEqual("Three", one.getDisposing()[1]);
            Assert.AreEqual("Two", one.getDisposing()[2]);
            Assert.AreEqual("One", one.getDisposing()[3]);
        }

        public class FooRunnable : IStartable
        {
            private int rCount;
            private Thread thread;

            public FooRunnable()
            {
            }

            #region IStartable Members

            public void Start()
            {
                thread = new Thread(new ThreadStart(run));
                thread.Start();
            }

            public void Stop()
            {
                thread.Interrupt();
            }

            #endregion

            public int runCount()
            {
                return rCount;
            }

            // this would do something a bit more concrete
            // than counting in real life !
            public void run()
            {
                rCount++;
                try
                {
                    Thread.Sleep(10000);
                }
                catch (Exception)
                {
                }
            }
        }

        public abstract class RecordingLifecycle : IStartable, IDisposable
        {
            private StringBuilder recording;

            protected RecordingLifecycle(StringBuilder recording)
            {
                this.recording = recording;
            }

            #region IDisposable Members

            public void Dispose()
            {
                recording.Append("!" + code());
            }

            #endregion

            #region IStartable Members

            public void Start()
            {
                recording.Append("<" + code());
            }

            public void Stop()
            {
                recording.Append(code() + ">");
            }

            #endregion

            private String code()
            {
                String name = GetType().Name;
                return name.Substring(name.IndexOf('$') + 1);
            }
        }

        public class A : RecordingLifecycle
        {
            public A(StringBuilder recording) : base(recording)
            {
            }
        }

        public class B : RecordingLifecycle
        {
            public B(StringBuilder recording) : base(recording)
            {
            }
        }

        // .Net functionality if two objects are equal the ordering differs from java
        // I could not find a reason why this testcase is included in Pico. It looks like a test case for the
        // sorting of collections.
        // The StackContainersAtEndComparator is only used in this testcase. 
        // I changed it to test only the placement of the containers at the end of the list.

        public class NotStartable
        {
            public NotStartable()
            {
                Assert.Fail("Shouldn't be instantiated");
            }
        }

        [Test]
        public void ComponentsAreStartedBreadthFirstAndStoppedDepthFirst()
        {
            IMutablePicoContainer parent = new DefaultPicoContainer();
            parent.RegisterComponentImplementation("recording", typeof (StringBuilder));
            parent.RegisterComponentImplementation(typeof (A));
            IMutablePicoContainer child = parent.MakeChildContainer();
            child.RegisterComponentImplementation(typeof (B));
            parent.Start();
            parent.Stop();

            Assert.AreEqual("<A<BB>A>", parent.GetComponentInstance("recording").ToString());
        }

        [Test]
        public void ContainersArePutLastAndTheOthersAreMaintainedInSamePlace()
        {
            IList l = new ArrayList();
            l.Add(new InstanceComponentAdapter("aa", "ComponentC"));
            l.Add(new InstanceComponentAdapter("aaa", new DefaultPicoContainer()));
            l.Add(new InstanceComponentAdapter("aaa1", "ComponentV"));
            l.Add(new InstanceComponentAdapter("bbb", new DefaultPicoContainer()));
            l.Add(new InstanceComponentAdapter("aa11", "ComponentD"));
            l.Add(new InstanceComponentAdapter("ccc", new DefaultPicoContainer()));
            l.Add(new InstanceComponentAdapter("casa", "asa"));

            l = DefaultPicoContainer.OrderComponentAdaptersWithContainerAdaptersLast(l);
            Assert.IsTrue(((IComponentAdapter) l[4]).GetComponentInstance(null) is DefaultPicoContainer);
            Assert.IsTrue(((IComponentAdapter) l[5]).GetComponentInstance(null) is DefaultPicoContainer);
            Assert.IsTrue(((IComponentAdapter) l[6]).GetComponentInstance(null) is DefaultPicoContainer);
        }

        [Test]
        public void DefaultPicoContainerRegisteredAsComponentGetsHostingContainerAsParent()
        {
            IMutablePicoContainer parent = new DefaultPicoContainer();
            DefaultPicoContainer child = (DefaultPicoContainer) parent.MakeChildContainer();
            Assert.AreSame(parent, child.Parent);
        }

        [Test]
        public void DisposeDisposeCausingBarf()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();
            pico.Start();
            pico.Stop();
            pico.Dispose();
            try
            {
                pico.Dispose();
                Assert.Fail("Should have barfed");
            }
            catch (Exception)
            {
                // expected;
            }
        }

        [Test]
        public void GetComponentInstancesOnParentContainerHostedChildContainerDoesntReturnParentAdapter()
        {
            IMutablePicoContainer parent = new DefaultPicoContainer();
            DefaultPicoContainer child = (DefaultPicoContainer) parent.MakeChildContainer();
            Assert.AreEqual(0, child.ComponentInstances.Count);
        }

        [Test]
        public void OnlyStartableComponentsAreInstantiatedOnStart()
        {
            IMutablePicoContainer pico = new DefaultPicoContainer();
            pico.RegisterComponentImplementation("recording", typeof (StringBuilder));
            pico.RegisterComponentImplementation(typeof (A));
            pico.RegisterComponentImplementation(typeof (NotStartable));
            pico.Start();

            pico.Stop();
            pico.Dispose();
            Assert.AreEqual("<AA>!A", pico.GetComponentInstance("recording").ToString());
        }

        [Test]
        public void OrderOfInstantiationWithoutAdapter()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();

            pico.RegisterComponentImplementation(typeof (Four));
            pico.RegisterComponentImplementation(typeof (Two));
            pico.RegisterComponentImplementation(typeof (One));
            pico.RegisterComponentImplementation(typeof (Three));

            IList componentInstances = pico.ComponentInstances;
            Assert.AreEqual(4, componentInstances.Count);

            // instantiation - would be difficult to do these in the wrong order!!
            Assert.AreEqual(typeof (One), componentInstances[0].GetType());
            Assert.AreEqual(typeof (Two), componentInstances[1].GetType());
            Assert.AreEqual(typeof (Three), componentInstances[2].GetType());
            Assert.AreEqual(typeof (Four), componentInstances[3].GetType());
        }

        [Test]
        public void StartStartCausingBarf()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();
            pico.Start();
            try
            {
                pico.Start();
                Assert.Fail("Should have barfed");
            }
            catch (Exception)
            {
                // expected;
            }
        }

        [Test]
        public void StartStopDisposeDisposeCausingBarf()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();
            object o = pico.ComponentInstances;
            pico.Start();
            pico.Stop();
            pico.Dispose();
            try
            {
                pico.Dispose();
                Assert.Fail("Should have barfed");
            }
            catch (Exception)
            {
                // expected;
            }
        }

        [Test]
        public void StartStopOfDaemonizedThread()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();
            pico.RegisterComponentImplementation(typeof (FooRunnable));

            object i = pico.ComponentInstances;
            pico.Start();
            Thread.Sleep(100);
            pico.Stop();

            FooRunnable foo = (FooRunnable) pico.GetComponentInstance(typeof (FooRunnable));
            Assert.AreEqual(1, foo.runCount());
            pico.Start();
            Thread.Sleep(100);
            pico.Stop();
            Assert.AreEqual(2, foo.runCount());
        }

        [Test]
        public void StartStopStartStopAndDispose()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();
            pico.RegisterComponentImplementation(typeof (One));
            pico.RegisterComponentImplementation(typeof (Two));
            pico.RegisterComponentImplementation(typeof (Three));
            pico.RegisterComponentImplementation(typeof (Four));
            One one = (One) pico.GetComponentInstance(typeof (One));
            object o = pico.ComponentInstances;
            // instantiation - would be difficult to do these in the wrong order!!
            Assert.AreEqual(4, one.getInstantiating().Count);
            Assert.AreEqual("One", one.getInstantiating()[0]);
            Assert.AreEqual("Two", one.getInstantiating()[1]);
            Assert.AreEqual("Three", one.getInstantiating()[2]);
            Assert.AreEqual("Four", one.getInstantiating()[3]);
            StartStopDisposeLifecycleComps(pico, pico, pico, one);
        }

        [Test]
        public void StartStopStopCausingBarf()
        {
            DefaultPicoContainer pico = new DefaultPicoContainer();
            pico.Start();
            pico.Stop();
            try
            {
                pico.Stop();
                Assert.Fail("Should have barfed");
            }
            catch (Exception)
            {
                // expected;
            }
        }
    }
}