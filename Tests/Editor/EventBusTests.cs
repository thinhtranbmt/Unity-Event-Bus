using System;
using NUnit.Framework;

namespace UnityEventBus.Tests
{
    public sealed class EventBusTests
    {
        private struct TestEvent : IEvent
        {
            public int Value;
        }

        [SetUp]
        public void SetUp()
        {
            EventBus<TestEvent>.Clear();
        }

        [Test]
        public void Raise_InvokesTypedHandler_WithPayload()
        {
            int received = 0;
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(e => received = e.Value));

            EventBus<TestEvent>.Raise(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Raise_InvokesNoArgsHandler()
        {
            int calls = 0;
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(() => calls++));

            EventBus<TestEvent>.Raise(new TestEvent());
            EventBus<TestEvent>.Raise(new TestEvent());

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Deregister_StopsInvocation()
        {
            int calls = 0;
            var binding = new EventBinding<TestEvent>(() => calls++);
            EventBus<TestEvent>.Register(binding);

            EventBus<TestEvent>.Raise(new TestEvent());
            EventBus<TestEvent>.Deregister(binding);
            EventBus<TestEvent>.Raise(new TestEvent());

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RegisterSameBindingTwice_InvokesOnce()
        {
            int calls = 0;
            var binding = new EventBinding<TestEvent>(() => calls++);
            EventBus<TestEvent>.Register(binding);
            EventBus<TestEvent>.Register(binding);

            EventBus<TestEvent>.Raise(new TestEvent());

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void DeregisterDuringRaise_SkipsBindingInSameRaise()
        {
            int lateCalls = 0;
            var lateBinding = new EventBinding<TestEvent>(() => lateCalls++);
            var earlyBinding = new EventBinding<TestEvent>(() => EventBus<TestEvent>.Deregister(lateBinding));
            EventBus<TestEvent>.Register(earlyBinding);
            EventBus<TestEvent>.Register(lateBinding);

            EventBus<TestEvent>.Raise(new TestEvent());

            Assert.AreEqual(0, lateCalls);
        }

        [Test]
        public void RegisterDuringRaise_InvokesOnlyOnNextRaise()
        {
            int newCalls = 0;
            var newBinding = new EventBinding<TestEvent>(() => newCalls++);
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(() =>
            {
                if (newCalls == 0)
                {
                    EventBus<TestEvent>.Register(newBinding);
                }
            }));

            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(0, newCalls);

            EventBus<TestEvent>.Raise(new TestEvent());
            Assert.AreEqual(1, newCalls);
        }

        [Test]
        public void NestedRaise_CompletesWithoutCorruptingState()
        {
            int calls = 0;
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(e =>
            {
                calls++;
                if (e.Value > 0)
                {
                    EventBus<TestEvent>.Raise(new TestEvent { Value = e.Value - 1 });
                }
            }));

            EventBus<TestEvent>.Raise(new TestEvent { Value = 2 });

            Assert.AreEqual(3, calls);
        }

        [Test]
        public void ThrowingHandler_PropagatesButLeavesBusUsable()
        {
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(() => throw new InvalidOperationException("boom")));

            Assert.Throws<InvalidOperationException>(() => EventBus<TestEvent>.Raise(new TestEvent()));

            int calls = 0;
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(() => calls++));
            Assert.Throws<InvalidOperationException>(() => EventBus<TestEvent>.Raise(new TestEvent()));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void EventBindingAdd_ComposesHandlers()
        {
            int a = 0;
            int b = 0;
            var binding = new EventBinding<TestEvent>(_ => a++);
            binding.Add(() => b++);
            EventBus<TestEvent>.Register(binding);

            EventBus<TestEvent>.Raise(new TestEvent());

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void ClearAll_EmptiesEveryTouchedBus()
        {
            int calls = 0;
            EventBus<TestEvent>.Register(new EventBinding<TestEvent>(() => calls++));

            EventBusRegistry.ClearAll();
            EventBus<TestEvent>.Raise(new TestEvent());

            Assert.AreEqual(0, calls);
            CollectionAssert.Contains(EventBusRegistry.ActiveBusNames, nameof(TestEvent));
        }

        [Test]
        public void BindingCount_TracksRegistrations()
        {
            var binding = new EventBinding<TestEvent>(() => { });
            Assert.AreEqual(0, EventBus<TestEvent>.BindingCount);

            EventBus<TestEvent>.Register(binding);
            Assert.AreEqual(1, EventBus<TestEvent>.BindingCount);

            EventBus<TestEvent>.Deregister(binding);
            Assert.AreEqual(0, EventBus<TestEvent>.BindingCount);
        }
    }
}
