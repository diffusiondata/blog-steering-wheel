#region Copyright & License Information
/*
 * Copyright (C) 2014 Push Technology Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
#endregion

using System;
using System.Diagnostics;

namespace F1Publisher
{
    class MetricEventArgs : EventArgs
    {
        public readonly Metrics.Types Type;
        public readonly UInt64 Value;

        public MetricEventArgs(Metrics.Types type, UInt64 value)
        {
            this.Type = type;
            this.Value = value;
        }
    }

    class Metrics
    {
        public enum Types
        {
            CountOfUpdates,
            UpTimeInSeconds,
            CountOfSuccessfulTopicSourceUpdates,
            CountOfFailedTopicSourceUpdates,

            RateOfUpdatesPerSecond,
            RateOfSuccessfulTopicSourceUpdatesPerSecond,
        }

        public event EventHandler<MetricEventArgs> MetricUpdated;

        private readonly UInt64[] values = new UInt64[Enum.GetValues(typeof(Types)).Length];
        private readonly UInt64[] lastReportedValues = new UInt64[Enum.GetValues(typeof(Types)).Length];
        private readonly Stopwatch uptimeStopwatch = new Stopwatch();
        private readonly Stopwatch samplingStopwatch = new Stopwatch();

        private UInt64 CountOfUpdatesThisSecond;
        private UInt64 CountOfSuccessfulTopicSourceUpdatesThisSecond;

        public UInt64 GetValue(Types type)
        {
            lock (this)
            {
                return values[(int)type];
            } 
        }

        public void OnSuccessfulTopicSourceUpdate(string topicPath)
        {
            CountOfSuccessfulTopicSourceUpdatesThisSecond++;
            Increment(Types.CountOfSuccessfulTopicSourceUpdates);
        }

        public void OnFailedTopicSourceUpdate(string topicPath)
        {
            Increment(Types.CountOfFailedTopicSourceUpdates);
        }

        public void Update()
        {
            var newValues = new UInt64[values.Length];

            lock(this)
            {
                values[(int)Types.CountOfUpdates]++;
                values[(int)Types.UpTimeInSeconds] = (UInt64)Math.Floor((double)uptimeStopwatch.ElapsedMilliseconds / 1000.0);

                if (!uptimeStopwatch.IsRunning)
                    uptimeStopwatch.Start();

                if (!samplingStopwatch.IsRunning)
                {
                    // This is the first call to OnUpdate on this instance of Metrics.
                    samplingStopwatch.Start();
                    CountOfUpdatesThisSecond = 0;
                    CountOfSuccessfulTopicSourceUpdatesThisSecond = 0;
                }
                else
                {
                    CountOfUpdatesThisSecond++;

                    if (samplingStopwatch.ElapsedMilliseconds >= 1000)
                    {
                        // It's been at least one second.
                        values[(int)Types.RateOfUpdatesPerSecond] = CountOfUpdatesThisSecond;
                        values[(int)Types.RateOfSuccessfulTopicSourceUpdatesPerSecond] = CountOfSuccessfulTopicSourceUpdatesThisSecond;
                        samplingStopwatch.Restart();
                        CountOfUpdatesThisSecond = 0;
                        CountOfSuccessfulTopicSourceUpdatesThisSecond = 0;
                    }
                }

                Array.Copy(values, newValues, values.Length);
            } 

            Update(lastReportedValues, newValues);
            Array.Copy(newValues, lastReportedValues, newValues.Length);
        }

        private void Increment(Metrics.Types type)
        {
            lock(this)
            {
                ++values[(int)type];
            }
        }

        private void Update(UInt64[] oldValues, UInt64[] newValues)
        {
            for (int i=0; i<newValues.Length; i++)
            {
                if (oldValues[i] != newValues[i])
                {
                    var type = (Types)Enum.Parse(typeof(Types), i.ToString());
                    OnMetricUpdated(new MetricEventArgs(type, newValues[i]));
                }
            }
        }

        protected virtual void OnMetricUpdated(MetricEventArgs e)
        {
            var handler = MetricUpdated;
            if (null == handler) return;
            handler(this, e);
        }
    }
}
