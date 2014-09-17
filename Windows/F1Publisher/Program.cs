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
using PushTechnology.ClientInterface.Client.Factories;
using PushTechnology.ClientInterface.Client.Session;
using PushTechnology.DiffusionCore.Logging;

namespace F1Publisher
{
    class Program : IDisposable
    {
        static void Main(string[] args)
        {
            (new Program()).Run();
        }

        private readonly DataGenerators.DirectInputManager directInputManager;
        private readonly Metrics metrics;
        private readonly DataGenerators.Car car;
        private readonly RefreshIntervalManager refreshIntervalManager;
        private readonly Stopwatch sleepStopwatch = new Stopwatch();

        private TopicManager topicManager;

        Program()
        {
            directInputManager = new DataGenerators.DirectInputManager();
            metrics = new Metrics();
            refreshIntervalManager = new RefreshIntervalManager();
            car = new DataGenerators.Car(directInputManager, refreshIntervalManager);

            // In order for us to see Exception instances thrown from our own (application) code
            // executing within a callback from the Diffusion SDK, we need to direct log output
            // to the Console.
            LogService.ActiveLoggerType = LoggerType.Console;
            LogService.SetThresholdForLogger(LoggerType.Console, LogSeverity.Error);

            var sessionFactory = Diffusion.Sessions
                .ConnectionTimeout(5000) // milliseconds
                .SessionErrorHandler(session_Error)
                .SessionStateChangedHandler(session_StateChanged);

            // I get SessionStateChanged event before this method returns
            string diffusionServerURL = Properties.Settings.Default.DiffusionServerURL;
            Log.Spew("Connecting to Diffusion Server at \"" + diffusionServerURL + "\"...");
            sessionFactory.Open(diffusionServerURL).Start();
        }

        private void Run()
        { 
            while (true)
            {
                Sleep(refreshIntervalManager.RefreshInterval.SleepDuration);
                directInputManager.Update();
                metrics.Update();
            }
        }

        private void Sleep(uint milliseconds)
        {
            // Using a StopWatch with Thread.Yield is a naive but adequate workaround for
            // the lack of precision offered by Thread.Sleep.
            sleepStopwatch.Restart();
            while (sleepStopwatch.ElapsedMilliseconds < milliseconds)
                System.Threading.Thread.Yield();
        }

        private void session_StateChanged(object sender, SessionListenerEventArgs e)
        {
            Log.Spew("session_StateChanged:\n\tfrom " + e.OldState + "\n\tto   " + e.NewState);

            var justConnected = e.NewState.Connected && !e.OldState.Connected;
            var justDisconnected = !e.NewState.Connected && e.OldState.Connected;

            if (justConnected)
                topicManager = new TopicManager(e.Session, directInputManager, car, refreshIntervalManager, metrics); 
            
            if (justDisconnected)
                topicManager = null; 
        }

        private void session_Error(object sender, SessionErrorHandlerEventArgs e)
        {
            Log.Spew("session_Error: " + e.Error);
        }

        public void Dispose()
        {
            if (null != directInputManager)
                directInputManager.Dispose();
        }
    }
}
