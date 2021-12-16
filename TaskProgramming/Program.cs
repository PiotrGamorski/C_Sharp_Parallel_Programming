using System;
using System.Threading.Tasks;

namespace Demo
{
    internal static class Program
    {
        public static void Main()
        {
            #region Creating and Starting Tasks
            Task.Factory.StartNew(() => write('.'));
            // or alternatively
            var t = new Task(() => write('?'));
            t.Start();

            var T = new Task(Write, "hello");
            T.Start();
            Task.Factory.StartNew(Write, 123);

            string text1 = "testing";
            string text2 = "this";
            var t1 = new Task<int>(TextLenght, text1);
            t1.Start();
            Task<int> t2 = Task.Factory.StartNew(TextLenght, text2);

            Console.WriteLine($"Lenght of '{text1}' is {t1.Result}");
            Console.WriteLine($"Lenght of '{text2}' is {t2.Result}");
            #endregion

            #region Cancelling Tasks
            // Single Cancellation Token Source
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            token.Register(() =>
            {
                Console.WriteLine("Cancelation has been requested.");
            });

            var t3 = new Task(() =>
            {
                int i = 0;
                while (true)
                {
                    Console.WriteLine("Press any key to cancel");
                    token.ThrowIfCancellationRequested();
                    Console.WriteLine($"{i++}\t");
                    Thread.Sleep(1000);
                }
            }, token);
            t3.Start();

            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Press any key to cancel");
                token.WaitHandle.WaitOne();
                Console.WriteLine("Wait handle has been released, canceletion was requested");
            });

            Console.ReadKey();
            cts.Cancel();

            // Multi Cancellation Source Token
            var plannedCancellation = new CancellationTokenSource();
            var preventativeCancellation = new CancellationTokenSource();
            var emergencyCancellation = new CancellationTokenSource();

            var paranoid = CancellationTokenSource.CreateLinkedTokenSource(plannedCancellation.Token,
                                                                           preventativeCancellation.Token,
                                                                           emergencyCancellation.Token);
            Task.Factory.StartNew(() =>
            {
                int i = 0;
                while (true)
                {
                    paranoid.Token.ThrowIfCancellationRequested();
                    Console.WriteLine($"{i++}\t");
                    Thread.Sleep(1000);
                }
            }, paranoid.Token);

            Console.ReadKey();
            emergencyCancellation.Cancel();
            #endregion

            #region Waiting for time to pass
            var bombCts = new CancellationTokenSource();
            var bombToken = bombCts.Token;
            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Press any key to disarm; you have 5 seconds");
                //SpinWait.SpinUntil(() => true);
                bool cancelled = bombToken.WaitHandle.WaitOne(5000);
                Console.WriteLine(cancelled ? "Bomb disarmed" : "BOOM!!!");
            }, bombToken);

            Console.ReadKey();
            bombCts.Cancel();

            var cts5 = new CancellationTokenSource();
            var token5 = cts5.Token;
            var t5 = new Task(() =>
            {
                Console.WriteLine("I take 5 seconds");
                for (int i = 0; i < 5; i++)
                {
                    token5.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }
                Console.WriteLine("I'm done");
            }, token5);
            t5.Start();
            Task t6 = Task.Factory.StartNew(() => Thread.Sleep(3000), token5);
            // Waits for all provided Tasks to complete execution
            // In this case will wait 5 seconds as t5 it is its execution time
            Task.WaitAll(t5, t6);
            // Maximum time of waiting is now set to 4 seconds
            // Task.WaitAny(new[] { t5, t6 }, 4000, token5);
            #endregion

            #region Exception Handling
            try
            {
                Test();
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.InnerExceptions)
                {
                    Console.WriteLine($"Handled elsewhere: {e.GetType()}");
                }
            }
            #endregion

            Console.WriteLine("Main progrm done.");
            Console.ReadKey();
        }

        #region Methods
        private static void write(char c)
        {
            int i = 1000;
            while (i-- > 0)
            {
                Console.Write(c);
            }
        }

        private static void Write(object c)
        {
            int i = 1000;
            while (i-- > 0)
            {
                Console.Write(c);
            }
        }

        private static int TextLenght(object o)
        {
            Console.WriteLine($"\nTask with id {Task.CurrentId} processing object {o}...");
            return o.ToString().Length;
        }


        private static void Test()
        {
            var t = Task.Factory.StartNew(() => { throw new InvalidOperationException("Can't do this!"); });
            var t2 = Task.Factory.StartNew(() => { throw new AccessViolationException("Can't access this!"); });

            try
            {
                Task.WaitAll(t, t2);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e =>
                {
                    if (e is InvalidOperationException)
                    {
                        Console.WriteLine("Invalid op!");
                        return true;
                    }
                    else return false;
                });
            }
        }
        #endregion
    }
}
