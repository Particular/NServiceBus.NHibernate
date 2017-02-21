namespace NServiceBus.NHibernate.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class CallbacksTests
    {
        [Test]
        public async Task It_does_nothing_when_there_is_no_callbacks()
        {
            var callbacks = new CallbackList();

            await callbacks.InvokeAll();

            Assert.Pass();
        }

        [Test]
        public async Task It_invokes_a_single_callback_once()
        {
            var invoked = 0;
            var callbacks = new CallbackList();
            callbacks.Add(() =>
            {
                invoked++;
                return Task.FromResult(0);
            });

            await callbacks.InvokeAll();

            Assert.AreEqual(1, invoked);
        }

        [Test]
        public async Task It_invokes_multiple_callbacks_onces_each()
        {
            var firstInvoked = 0;
            var secondInvoked = 0;
            var callbacks = new CallbackList();
            callbacks.Add(() =>
            {
                firstInvoked++;
                return Task.FromResult(0);
            });
            callbacks.Add(() =>
            {
                secondInvoked++;
                return Task.FromResult(0);
            });

            await callbacks.InvokeAll();

            Assert.AreEqual(1, firstInvoked);
            Assert.AreEqual(1, secondInvoked);
        }
    }
}