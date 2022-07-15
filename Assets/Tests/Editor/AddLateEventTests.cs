using NUnit.Framework;
using UnityEngine.Events;

namespace Mirage.Events.Tests
{
    public abstract class AddLateEventTestsBase
    {
        private int listenerCallCount;
        protected void TestListener() => this.listenerCallCount++;

        protected abstract void Init();
        protected abstract void Invoke();
        protected abstract void AddListener();
        protected abstract void RemoveListener();
        protected abstract void Reset();
        protected abstract void RemoveAllListeners();


        [SetUp]
        public void Setup()
        {
            this.listenerCallCount = 0;
            this.Init();
        }

        [Test]
        public void EventCanBeInvokedOnce()
        {
            this.AddListener();
            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void EventCanBeInvokedTwice()
        {
            this.AddListener();

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(2));
        }

        [Test]
        public void EventCanBeInvokedEmpty()
        {
            Assert.DoesNotThrow(() =>
            {
                this.Invoke();
            });
        }

        [Test]
        public void AddingListenerLateInvokesListener()
        {
            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(0));
            this.AddListener();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void AddingListenerLateInvokesListenerOnce()
        {
            this.Invoke();
            this.Invoke();
            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(0));
            this.AddListener();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void AddingListenerLateCanBeInvokedMultipleTimes()
        {
            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(0));

            this.AddListener();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(2));
        }

        [Test]
        public void ResetThenAddListenerDoesntInvokeRightAway()
        {
            this.AddListener();

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));

            this.Reset();

            this.AddListener();
            Assert.That(this.listenerCallCount, Is.EqualTo(1), "Event should not auto invoke after reset");

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(3), "old and new listeners should have been invoked");
        }

        [Test]
        public void RemoveListenersShouldRemove1Listner()
        {
            this.AddListener();

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));

            this.RemoveListener();
            this.Invoke();
            // listener removed so no increase to count
            Assert.That(this.listenerCallCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAllRemovesListeners()
        {
            this.AddListener();

            this.Invoke();
            Assert.That(this.listenerCallCount, Is.EqualTo(1));

            this.RemoveAllListeners();

            Assert.DoesNotThrow(() =>
            {
                this.Invoke();
            });
            // listener removed so no increase to count
            Assert.That(this.listenerCallCount, Is.EqualTo(1));
        }
    }


    public class AddLateEvent0ArgTest : AddLateEventTestsBase
    {
        private AddLateEvent allLate;
        protected override void Init()
        {
            this.allLate = new AddLateEvent();
        }

        protected override void Invoke()
        {
            this.allLate.Invoke();
        }

        protected override void AddListener()
        {
            this.allLate.AddListener(this.TestListener);
        }

        protected override void RemoveListener()
        {
            this.allLate.RemoveListener(this.TestListener);
        }

        protected override void Reset()
        {
            this.allLate.Reset();
        }

        protected override void RemoveAllListeners()
        {
            this.allLate.RemoveAllListeners();
        }
    }


    public class IntUnityEvent : UnityEvent<int> { }
    public class IntAddLateEvent : AddLateEvent<int, IntUnityEvent> { }
    public class AddLateEvent1ArgTest : AddLateEventTestsBase
    {
        private IntAddLateEvent allLate;

        private void TestListener1Arg(int a)
        {
            this.TestListener();
        }

        protected override void Init()
        {
            this.allLate = new IntAddLateEvent();
        }

        protected override void Invoke()
        {
            this.allLate.Invoke(default);
        }

        protected override void AddListener()
        {
            this.allLate.AddListener(this.TestListener1Arg);
        }

        protected override void RemoveListener()
        {
            this.allLate.RemoveListener(this.TestListener1Arg);
        }

        protected override void Reset()
        {
            this.allLate.Reset();
        }

        protected override void RemoveAllListeners()
        {
            this.allLate.RemoveAllListeners();
        }

        [Test]
        public void ListenerIsInvokedWithCorrectArgs()
        {
            const int arg0 = 10;

            var callCount = 0;

            this.allLate.AddListener((a0) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
            });


            this.allLate.Invoke(arg0);
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerIsInvokedLateWithCorrectArgs()
        {
            const int arg0 = 10;

            var callCount = 0;

            // invoke before adding handler
            this.allLate.Invoke(arg0);

            this.allLate.AddListener((a0) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
            });

            Assert.That(callCount, Is.EqualTo(1));
        }
    }


    public class IntStringUnityEvent : UnityEvent<int, string> { }
    public class IntStringAddLateEvent : AddLateEvent<int, string, IntStringUnityEvent> { }
    public class AddLateEvent2ArgTest : AddLateEventTestsBase
    {
        private IntStringAddLateEvent allLate;

        private void TestListener2Arg(int a, string b)
        {
            this.TestListener();
        }

        protected override void Init()
        {
            this.allLate = new IntStringAddLateEvent();
        }

        protected override void Invoke()
        {
            this.allLate.Invoke(default, default);
        }

        protected override void AddListener()
        {
            this.allLate.AddListener(this.TestListener2Arg);
        }

        protected override void RemoveListener()
        {
            this.allLate.RemoveListener(this.TestListener2Arg);
        }

        protected override void Reset()
        {
            this.allLate.Reset();
        }

        protected override void RemoveAllListeners()
        {
            this.allLate.RemoveAllListeners();
        }

        [Test]
        public void ListenerIsInvokedWithCorrectArgs()
        {
            const int arg0 = 10;
            const string arg1 = "hell world";

            var callCount = 0;

            this.allLate.AddListener((a0, a1) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
                Assert.That(a1, Is.EqualTo(arg1));
            });


            this.allLate.Invoke(arg0, arg1);
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ListenerIsInvokedLateWithCorrectArgs()
        {
            const int arg0 = 10;
            const string arg1 = "hell world";

            var callCount = 0;

            // invoke before adding handler
            this.allLate.Invoke(arg0, arg1);

            this.allLate.AddListener((a0, a1) =>
            {
                callCount++;
                Assert.That(a0, Is.EqualTo(arg0));
                Assert.That(a1, Is.EqualTo(arg1));
            });

            Assert.That(callCount, Is.EqualTo(1));
        }
    }
}
