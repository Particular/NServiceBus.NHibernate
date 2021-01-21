namespace Runner
{
    using System;

    class Statistics
    {
        public static DateTimeOffset? First;
        public static DateTimeOffset Last;
        public static DateTimeOffset StartTime;
        public static long NumberOfMessages;
        public static long NumberOfRetries;
        public static TimeSpan SendTimeNoTx = TimeSpan.Zero;
        public static TimeSpan SendTimeWithTx = TimeSpan.Zero;
        public static TimeSpan PublishTimeNoTx = TimeSpan.Zero;
        public static TimeSpan PublishTimeWithTx = TimeSpan.Zero;

        public static void Dump()
        {
            Console.Out.WriteLine("");
            Console.Out.WriteLine("---------------- Statistics ----------------");

            PrintStats("NumberOfMessages", NumberOfMessages, "#");

            if (First.HasValue)
            {
                var durationSeconds = (Last - First.Value).TotalSeconds;
                var throughput = Convert.ToDouble(NumberOfMessages) / durationSeconds;

                PrintStats("Throughput", throughput, "msg/s");

                Console.Out.WriteLine("##teamcity[buildStatisticValue key='ReceiveThroughputSagas' value='{0}']", Math.Round(throughput));

                PrintStats("NumberOfRetries", NumberOfRetries, "#");
                PrintStats("TimeToFirstMessage", (First - StartTime).Value.TotalSeconds, "s");
            }


            if (SendTimeNoTx != TimeSpan.Zero)
            {
                PrintStats("Sending", Convert.ToDouble(NumberOfMessages / 2) / SendTimeNoTx.TotalSeconds, "msg/s");
            }

            if (SendTimeWithTx != TimeSpan.Zero)
            {
                PrintStats("SendingInsideTX", Convert.ToDouble(NumberOfMessages / 2) / SendTimeWithTx.TotalSeconds, "msg/s");
            }

            if (PublishTimeNoTx != TimeSpan.Zero)
            {
                PrintStats("PublishTimeNoTx", Convert.ToDouble(NumberOfMessages / 2) / PublishTimeNoTx.TotalSeconds, "msg/s");
            }

            if (PublishTimeWithTx != TimeSpan.Zero)
            {
                PrintStats("PublishTimeWithTx", Convert.ToDouble(NumberOfMessages / 2) / PublishTimeWithTx.TotalSeconds, "msg/s");
            }
        }

        static void PrintStats(string key, double value, string unit)
        {
            Console.Out.WriteLine("{0}: {1:0.0} ({2})", key, value, unit);
        }
    }
}