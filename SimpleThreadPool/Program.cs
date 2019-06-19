using CompareBySizeAndSHA246;
using SimpleThreadPool;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace SimpleThreadPool
{
    public sealed class Pool : IDisposable
    {
        public Pool(int size)
        {
            this._workers = new LinkedList<Thread>();
            for (var i = 0; i < size; ++i)
            {
                var worker = new Thread(this.Worker) { Name = string.Concat("Worker ", i) };
                worker.Start();
                this._workers.AddLast(worker);
            }
        }

        public void Dispose()
        {
            var waitForThreads = false;
            lock (this._tasks)
            {
                if (!this._disposed)
                {
                    GC.SuppressFinalize(this);

                    this._disallowAdd = true; // wait for all tasks to finish processing while not allowing any more new tasks
                    while (this._tasks.Count > 0)
                    {
                        Monitor.Wait(this._tasks);
                    }

                    this._disposed = true;
                    Monitor.PulseAll(this._tasks); // wake all workers (none of them will be active at this point; disposed flag will cause then to finish so that we can join them)
                    waitForThreads = true;
                }
            }
            if (waitForThreads)
            {
                foreach (var worker in this._workers)
                {
                    worker.Join();
                }
            }
        }

        public void QueueTask(Action task)
        {
            lock (this._tasks)
            {
                if (this._disallowAdd) { throw new InvalidOperationException("This Pool instance is in the process of being disposed, can't add anymore"); }
                if (this._disposed) { throw new ObjectDisposedException("This Pool instance has already been disposed"); }
                this._tasks.AddLast(task);
                Monitor.PulseAll(this._tasks); // pulse because tasks count changed
            }
        }

        private void Worker()
        {
            Action task = null;
            while (true) // loop until threadpool is disposed
            {
                lock (this._tasks) // finding a task needs to be atomic
                {
                    while (true) // wait for our turn in _workers queue and an available task
                    {
                        if (this._disposed)
                        {
                            return;
                        }
                        if (null != this._workers.First && object.ReferenceEquals(Thread.CurrentThread, this._workers.First.Value) && this._tasks.Count > 0) // we can only claim a task if its our turn (this worker thread is the first entry in _worker queue) and there is a task available
                        {
                            task = this._tasks.First.Value;
                            this._tasks.RemoveFirst();
                            this._workers.RemoveFirst();
                            Monitor.PulseAll(this._tasks); // pulse because current (First) worker changed (so that next available sleeping worker will pick up its task)
                            break; // we found a task to process, break out from the above 'while (true)' loop
                        }
                        Monitor.Wait(this._tasks); // go to sleep, either not our turn or no task to process
                    }
                }

                task(); // process the found task
                this._workers.AddLast(Thread.CurrentThread);
                task = null;
            }
        }

        private readonly LinkedList<Thread> _workers; // queue of worker threads ready to process actions
        private readonly LinkedList<Action> _tasks = new LinkedList<Action>(); // actions to be processed by worker threads
        private bool _disallowAdd; // set to true when disposing queue but there are still tasks pending
        private bool _disposed; // set to true when disposing queue and no more tasks are pending
    }

    public static class Program
    {

        private static string[] suffixes = new[] { " B", " KB", " MB", " GB", " TB", " PB" };

        public static string ToSize(double number, int precision = 2)
        {
            // unit's number of bytes
            const double unit = 1024;
            // suffix counter
            int i = 0;
            // as long as we're bigger than a unit, keep going
            while (number > unit)
            {
                number /= unit;
                i++;
            }
            // apply precision and current suffix
            return Math.Round(number, precision) + suffixes[i];
        }

        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(long value, int decimalPlaces = 0)
        {
            if (value < 0)
            {
                throw new ArgumentException("Bytes should not be negative", "value");
            }
            var mag = (int)Math.Max(0, Math.Log(value, 1024));
            var adjustedSize = Math.Round(value / Math.Pow(1024, mag), decimalPlaces);
            return String.Format("{0} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        private static void RunThis(Random random, int index, FileInfo fichier)
        {
            //int iSleep = random.Next(500, 10000);
            String hash = "";
            String hash2 = "";
            Stopwatch stopWatch = Stopwatch.StartNew();
            if (!data.GetRow(fichier))
            {
                Console.WriteLine("{0}: Working on index {1}/{3} (size = {4}) on file {2}", Thread.CurrentThread.Name, index, fichier.FullName, listFiles.Count, SizeSuffix(fichier.Length,2));
                hash = HashCompute.GetChecksum(fichier.FullName);
                hash2 = HashCompute.GetSHA256(fichier.FullName);
                stopWatch.Stop();
                //data.InsertRow(fichier, hash);
                Console.WriteLine("{0}: Ending on index {1} on file {2} after {3} : {4}", Thread.CurrentThread.Name, index, fichier.Name, stopWatch.Elapsed.TotalSeconds.ToString("0.000000"), hash);
            }
            else
            {
                Console.WriteLine("Fichier {0} déjà traité...", fichier.Name);
            }
        }

        static Donnees data;
        static List<FileInfo> listFiles = new List<FileInfo>();
        static void Main()
        {
            Console.WriteLine("Traitement sur " + Environment.MachineName);

            data = new Donnees();
            data.InitializeDatabase(@"HashFiles.db");

            //const String K_DIRECTORY = @"c:\Users\Alain\Documents\Recover\Recovered data 03-13 22_13_03\Résultat d'analyse approfondie\Plus de fichiers perdus(RAW)\MKV file";

            LoadDirectory(@"d:\Profiles\aleglise\source\repos\SimpleThreadPool\SimpleThreadPool\Properties");
            //LoadDirectory(@"z:\Images\2015-07-17");
            //LoadDirectory(@"y:\Films");
            //LoadDirectory(@"y:\Films HQ");
            //LoadDirectory(@"z:\Films");
            //LoadDirectory(@"z:\_Dessins Animes");
            //LoadDirectory(@"x:\Séries");
            //LoadDirectory(@"c:\Users\Alain\Videos\Films");
            //LoadDirectory(@"c:\Users\Alain\Videos\Séries");

            using (var pool = new Pool(2))
            {
                var random = new Random();
                Action<int, FileInfo> randomizer = ((index, fichier) =>
                {
                    RunThis(random, index, fichier);
                });

                for (var i = 0; i < listFiles.Count; i++)
                {
                    var i1 = i;
                    FileInfo file = listFiles[i];
                    pool.QueueTask(() => randomizer(i1, file));
                }
            }
        }

        private static void LoadDirectory(string pDirectory)
        {
            Console.WriteLine("Chargement du répertoire " + pDirectory);
            foreach (DirectoryInfo d in new DirectoryInfo(pDirectory).GetDirectories())
            {
                LoadDirectory(d.FullName);
            }
            foreach (FileInfo f in new DirectoryInfo(pDirectory).GetFiles())
            {
                listFiles.Add(f);
            }
        }
    }
}
