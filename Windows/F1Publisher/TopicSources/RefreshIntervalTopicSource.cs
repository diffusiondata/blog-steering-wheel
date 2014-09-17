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
using PushTechnology.ClientInterface.Client.Content;
using PushTechnology.ClientInterface.Client.Factories;
using PushTechnology.ClientInterface.Client.Content.Metadata;

namespace F1Publisher.TopicSources
{
    class RefreshIntervalTopicSource : TopicSource
    {
        private const string frequencyFieldName = "Frequency";
        private const string sleepDurationFieldName = "SleepDuration";
        static private readonly IMRecord recordMetadata;

        static RefreshIntervalTopicSource()
        {
            var metadataFactory = Diffusion.Metadata;
            var recordBuilder = metadataFactory.RecordBuilder("RefreshInterval");
            recordBuilder.Add(metadataFactory.Integer(frequencyFieldName), 1, 1);
            recordBuilder.Add(metadataFactory.Integer(sleepDurationFieldName), 1, 1);
            recordMetadata = recordBuilder.Build();
        }

        private readonly RefreshIntervalManager refreshIntervalManager;
        private readonly IRecordStructuredBuilder recordStructuredBuilder;

        public RefreshIntervalTopicSource(RefreshIntervalManager refreshIntervalManager)
        {
            this.refreshIntervalManager = refreshIntervalManager;

            var contentFactory = Diffusion.Content;
            recordStructuredBuilder = contentFactory.NewRecordBuilder(recordMetadata);
        }

        private void UpdateRecord(RefreshInterval refreshInterval)
        {
            recordStructuredBuilder.Set(frequencyFieldName, refreshInterval.Frequency.ToString());
            recordStructuredBuilder.Set(sleepDurationFieldName, refreshInterval.SleepDuration.ToString());
        }

        private IContent CreateContent()
        {
            return CreateContent(recordStructuredBuilder.Build());
        }

        protected override IContent CreateInitialContent()
        {
            UpdateRecord(refreshIntervalManager.RefreshInterval);
            return CreateContent();
        }

        protected override void OnActivated()
        {
            refreshIntervalManager.RefreshIntervalChanged += refreshIntervalManager_RefreshIntervalChanged;
        }

        protected override void OnDeactivated()
        {
            refreshIntervalManager.RefreshIntervalChanged -= refreshIntervalManager_RefreshIntervalChanged;            
        }

        void refreshIntervalManager_RefreshIntervalChanged(object sender, RefreshIntervalEventArgs e)
        {
            UpdateRecord(e.RefreshInterval);
            UpdateContent(CreateContent());
        }
    }
}
