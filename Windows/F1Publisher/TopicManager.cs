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
using System.Collections.Generic;
using PushTechnology.ClientInterface.Client.Features;
using PushTechnology.ClientInterface.Client.Features.Control.Topics;
using PushTechnology.ClientInterface.Client.Session;
using PushTechnology.ClientInterface.Client.Topics;

namespace F1Publisher
{
    class TopicManager
    {
        private const string rootTopicPath = "F1Publisher";

        private const string steeringTopicPath = rootTopicPath + "/Steering";
        private const string brakingTopicPath = rootTopicPath + "/Braking";
        private const string accelerationTopicPath = rootTopicPath + "/Acceleration";
        private const string gearTopicPath = rootTopicPath + "/Gear";
        private const string refreshIntervalTopicPath = rootTopicPath + "/RefreshInterval";

        private const string buttonsTopicPath = rootTopicPath + "/Buttons";
        private const string buttonStatesTopicPath = buttonsTopicPath + "/States";
        private const string buttonNamesTopicPath = buttonsTopicPath + "/Names";

        private const string metricsTopicPath = rootTopicPath + "/Metrics";
        private const string countOfUpdatesMetricTopicPath = metricsTopicPath + "/CountOfUpdates";
        private const string upTimeInSecondsMetricTopicPath = metricsTopicPath + "/UpTimeInSeconds";
        private const string countOfSuccessfulTopicSourceUpdatesMetricTopicPath = metricsTopicPath + "/CountOfSuccessfulTopicSourceUpdates";
        private const string countOfFailedTopicSourceUpdatesMetricTopicPath = metricsTopicPath + "/CountOfFailedTopicSourceUpdates";
        private const string rateOfUpdatesPerSecondMetricTopicPath = metricsTopicPath + "/RateOfUpdatesPerSecond";
        private const string rateOfSuccessfulTopicSourceUpdatesPerSecond = metricsTopicPath + "/RateOfSuccessfulTopicSourceUpdatesPerSecond";

        private readonly DataGenerators.ICarControlsDataGenerator carControlsDataGenerator;
        private readonly DataGenerators.ICarStateDataGenerator carStateDataGenerator;
        private readonly RefreshIntervalManager refreshIntervalManager;
        private readonly Metrics metrics;

        private readonly ITopics topics;
        private readonly ITopicControl topicControl;
        private readonly ITopicUpdateControl topicUpdateControl;

        private readonly List<string> topicPathsPendingAddition;

        public TopicManager(ISession session, DataGenerators.ICarControlsDataGenerator carControlsDataGenerator, DataGenerators.ICarStateDataGenerator carStateDataGenerator, RefreshIntervalManager refreshIntervalManager, Metrics metrics)
        {
            this.carControlsDataGenerator = carControlsDataGenerator;
            this.carStateDataGenerator = carStateDataGenerator;
            this.refreshIntervalManager = refreshIntervalManager;
            this.metrics = metrics;

            topics = session.GetTopicsFeature();
            topicControl = session.GetTopicControlFeature();
            topicUpdateControl = session.GetTopicUpdateControlFeature();

            topicPathsPendingAddition = new List<string>();

            // The first thing we need to do is kick of an asynchronous request to see
            // whether our root topic path already exists.
            var topicDetailsHandler = new Handlers.TopicDetailsHandler();
            topicDetailsHandler.Success += topicDetailsHandler_Success;
            topics.GetTopicDetails(rootTopicPath, TopicDetailsLevel.BASIC, topicDetailsHandler);
        }

        private void AddTopic(Handlers.AddTopicHandler handler, string path)
        {
            AddTopic(handler, path, TopicType.SINGLE_VALUE);
        }

        private void AddTopic(Handlers.AddTopicHandler handler, string path, TopicType topicType)
        {
            topicPathsPendingAddition.Add(path);
            topicControl.AddTopic(path, topicType, handler);
        }

        private void topicDetailsHandler_Success(object sender, Handlers.TopicDetailsEventArgs e)
        {
            Log.Spew("Queried Topic: \"" + e.TopicPath + "\"");

            if (!rootTopicPath.Equals(e.TopicPath))
                throw new InvalidOperationException("Topic details received for unexpected topic path.");

            if (null == e.TopicDetails)
            {
                // The root topic path does not yet exist so we need to create it implicitly
                // by creating the child nodes.
                Log.Spew("Creating topic tree...");

                var addTopicHandler = new Handlers.AddTopicHandler();
                addTopicHandler.Success += addTopicHandler_Success;

                AddTopic(addTopicHandler, steeringTopicPath);
                AddTopic(addTopicHandler, brakingTopicPath);
                AddTopic(addTopicHandler, accelerationTopicPath);
                AddTopic(addTopicHandler, gearTopicPath);
                AddTopic(addTopicHandler, refreshIntervalTopicPath, TopicType.RECORD);
                AddTopic(addTopicHandler, buttonStatesTopicPath, TopicType.RECORD);
                AddTopic(addTopicHandler, buttonNamesTopicPath, TopicType.RECORD);

                AddTopic(addTopicHandler, countOfUpdatesMetricTopicPath);
                AddTopic(addTopicHandler, upTimeInSecondsMetricTopicPath);
                AddTopic(addTopicHandler, countOfSuccessfulTopicSourceUpdatesMetricTopicPath);
                AddTopic(addTopicHandler, countOfFailedTopicSourceUpdatesMetricTopicPath);
                AddTopic(addTopicHandler, rateOfUpdatesPerSecondMetricTopicPath);
                AddTopic(addTopicHandler, rateOfSuccessfulTopicSourceUpdatesPerSecond);
            }
            else
            {
                // The root topic path exists so we need to start updating it.
                // (I am making the assumption that all of the topics I need to have added
                // below this root will be there... this could break if I add a new topic to the
                // tree in this codebase without restarting the Diffusion server or removing the
                // root topic first.)
                AddTopicSources();
            }
        }

        private void addTopicHandler_Success(object sender, Handlers.TopicEventArgs e)
        {
            // At least one of our topics has been added.
            Log.Spew("Topic Added: \"" + e.TopicPath + "\"");
            while (topicPathsPendingAddition.Remove(e.TopicPath));
            if (0 == topicPathsPendingAddition.Count)
                AddTopicSources();
        }

        private void AddTopicSources()
        {
            Log.Spew("Adding topic sources...");

            topicUpdateControl.AddTopicSource(steeringTopicPath, new TopicSources.SteeringTopicSource(carControlsDataGenerator));
            topicUpdateControl.AddTopicSource(brakingTopicPath, new TopicSources.BrakingTopicSource(carControlsDataGenerator));
            topicUpdateControl.AddTopicSource(accelerationTopicPath, new TopicSources.AccelerationTopicSource(carControlsDataGenerator));
            topicUpdateControl.AddTopicSource(gearTopicPath, new TopicSources.GearTopicSource(carStateDataGenerator));
            topicUpdateControl.AddTopicSource(refreshIntervalTopicPath, new TopicSources.RefreshIntervalTopicSource(refreshIntervalManager));
            topicUpdateControl.AddTopicSource(buttonStatesTopicPath, new TopicSources.ButtonStatesTopicSource(carControlsDataGenerator));
            topicUpdateControl.AddTopicSource(buttonNamesTopicPath, new TopicSources.ButtonNamesTopicSource());

            Handlers.TopicSourceUpdateHandler.Singleton.Metrics = metrics;

            topicUpdateControl.AddTopicSource(countOfUpdatesMetricTopicPath, new TopicSources.MetricsTopicSource(metrics, Metrics.Types.CountOfUpdates));
            topicUpdateControl.AddTopicSource(upTimeInSecondsMetricTopicPath, new TopicSources.MetricsTopicSource(metrics, Metrics.Types.UpTimeInSeconds));
            topicUpdateControl.AddTopicSource(countOfSuccessfulTopicSourceUpdatesMetricTopicPath, new TopicSources.MetricsTopicSource(metrics, Metrics.Types.CountOfSuccessfulTopicSourceUpdates));
            topicUpdateControl.AddTopicSource(countOfFailedTopicSourceUpdatesMetricTopicPath, new TopicSources.MetricsTopicSource(metrics, Metrics.Types.CountOfFailedTopicSourceUpdates));
            topicUpdateControl.AddTopicSource(rateOfUpdatesPerSecondMetricTopicPath, new TopicSources.MetricsTopicSource(metrics, Metrics.Types.RateOfUpdatesPerSecond));
            topicUpdateControl.AddTopicSource(rateOfSuccessfulTopicSourceUpdatesPerSecond, new TopicSources.MetricsTopicSource(metrics, Metrics.Types.RateOfSuccessfulTopicSourceUpdatesPerSecond));
        }
    }
}
