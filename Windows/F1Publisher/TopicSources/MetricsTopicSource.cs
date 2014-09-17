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

using PushTechnology.ClientInterface.Client.Content;

namespace F1Publisher.TopicSources
{
    class MetricsTopicSource : TopicSource
    {
        private readonly Metrics metrics;
        private readonly Metrics.Types metricType;

        public MetricsTopicSource(Metrics metrics, Metrics.Types metricType)
        {
            this.metrics = metrics;
            this.metricType = metricType;
        }

        protected override IContent CreateInitialContent()
        {
            return CreateContent(metrics.GetValue(metricType));
        }

        protected override void OnActivated()
        {
            metrics.MetricUpdated += metrics_MetricUpdated;
        }

        protected override void OnDeactivated()
        {
            metrics.MetricUpdated -= metrics_MetricUpdated;
        }

        void metrics_MetricUpdated(object sender, MetricEventArgs e)
        {
            if (e.Type != metricType) return;
            UpdateContent(CreateContent(e.Value));
        }
    }
}
