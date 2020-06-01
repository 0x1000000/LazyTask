using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LazyTask.Test
{
    public class Tests
    {
        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task TestBasic(bool asyncWait, bool asyncAction)
        {
            int counter = 0;

            var lt = LazyAction();
            if (asyncWait)
            {
                await Task.Delay(1);
            }
            Assert.AreEqual(0, counter);

            int res = await lt;

            Assert.AreEqual(1, counter);
            Assert.AreEqual(42, res);

            async LazyTask<int> LazyAction()
            {
                counter++;
                if (asyncAction)
                {
                    await Task.Delay(1);
                }
                return 42;
            }
        }


        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task TestException(bool asyncWait, bool asyncAction)
        {
            int counter = 0;

            var lt = LazyAction();
            if (asyncWait)
            {
                await Task.Delay(1);
            }
            Assert.AreEqual(0, counter);

            Exception ex = null;
            try
            {
                await lt;
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.AreEqual(1, counter);
            Assert.NotNull(ex);
            Assert.AreEqual("Err", ex.Message);

            async LazyTask<int> LazyAction()
            {
                counter++;
                if (asyncAction)
                {
                    await Task.Delay(1);
                }
                throw new Exception("Err");
            }
        }

        [Test]
        public void TestMultiThreading()
        {
            //The test shows that locks are really required in LazyTasks

            int counter = 0;

            ManualResetEvent syncStart = new ManualResetEvent(false);

            const int num = 100;
            const int threadsNum = 10;

            for (int i = 0; i < num; i++)
            {

                var lt = LazyAction();

                List<Thread> threads = new List<Thread>();

                for (int j = 0; j < threadsNum; j++)
                {
                    threads.Add(new Thread(() => ThreadBody(lt)));
                }

                foreach (var thread in threads)
                {
                    thread.Start();
                }

                syncStart.Set();

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }

            Assert.AreEqual(num, counter);

            void ThreadBody(LazyTask<int> lt)
            {
                AutoResetEvent ev = new AutoResetEvent(false);

                syncStart.WaitOne();

                lt.OnCompleted(() =>
                {
                    Assert.AreEqual(42, lt.GetResult());
                    ev.Set();
                });
                ev.WaitOne();
            }

            async LazyTask<int> LazyAction()
            {
                counter++;
                await Task.Delay(1);
                return 42;
            }
        }

    }
}