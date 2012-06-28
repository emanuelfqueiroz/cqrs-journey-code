using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Azure;
using Infrastructure.Azure.BlobStorage;
using Infrastructure.Serialization;
using Microsoft.WindowsAzure;
using Registration;
using Registration.Events;
using Registration.Handlers;

namespace PerfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var warmupPeriod = TimeSpan.FromSeconds(15);
            var parallelLoopCount = 1000;
            var maxDegreeOfParallelism = 40;
            var workerLoopCount = 5;

            var azureSettings = InfrastructureSettings.Read("Settings.xml");
            azureSettings.BlobStorage.RootContainerName += "-test";
            var blobStorageAccount = CloudStorageAccount.Parse(azureSettings.BlobStorage.ConnectionString);
            var blobStorage = new CloudBlobStorage(blobStorageAccount, azureSettings.BlobStorage.RootContainerName);

            var generator = new PricedOrderViewModelGenerator(blobStorage, new JsonTextSerializer());
            Guid conferenceId = Guid.Parse("6F7D9B61-853A-49C9-8AC8-632F82140A90");
            Guid seatTypeId = Guid.Parse("0fc77074-02f7-4e52-b378-7e24d12828f9");

            var stopwatch = new Stopwatch();
            var resetEvent = new ManualResetEvent(false);
            var cts = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
                {
                    var warmupWait = DateTime.UtcNow.Add(warmupPeriod);
                    while (DateTime.UtcNow < warmupWait)
                    {
                        if (cts.IsCancellationRequested) break; ;
                        Thread.Sleep(1000);
                    }
                    resetEvent.Set();
                    stopwatch.Start();
                });
            Task.Factory.StartNew(() =>
            {
                Console.ReadLine();
                Console.WriteLine("Cancelling...");
                cts.Cancel();
            });

            try
            {
                Parallel.For(0, parallelLoopCount, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = cts.Token }, i =>
                    {
                        resetEvent.WaitOne();

                        for (int j = 0; j < workerLoopCount; j++)
                        {
                            if (cts.IsCancellationRequested) return;

                            var orderId = Guid.NewGuid();
                            var individualStopwatch = Stopwatch.StartNew();
                            try
                            {
                                generator.Handle(
                                    new OrderPlaced
                                    {
                                        AccessCode = "test",
                                        ConferenceId = conferenceId,
                                        ReservationAutoExpiration =
                                        DateTime.UtcNow.AddMinutes(10),
                                        Seats = new[] { new SeatQuantity { Quantity = 1, SeatType = seatTypeId } },
                                        SourceId = orderId,
                                        Version = 1
                                    });
                                generator.Handle(
                                    new OrderTotalsCalculated
                                    {
                                        Lines = new[] { new SeatOrderLine { Quantity = 1, SeatType = seatTypeId, LineTotal = 10 } },
                                        SourceId = orderId,
                                        Version = 2
                                    });
                                individualStopwatch.Stop();
                                Console.WriteLine("Item processed. Ellapsed: " + individualStopwatch.ElapsedMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                individualStopwatch.Stop();
                                Console.Error.WriteLine("Error at {0} milliseconds: {1}", individualStopwatch.ElapsedMilliseconds, ex.Message);
                            }
                        }
                    });
            }
            catch (OperationCanceledException)
            {
            }

            stopwatch.Stop();
            Console.WriteLine("Finished. Ellapsed: " + stopwatch.ElapsedMilliseconds);
            Console.ReadLine();
        }
    }
}
