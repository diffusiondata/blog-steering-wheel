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
using PushTechnology.ClientInterface.Client.Features;
using PushTechnology.ClientInterface.Client.Features.Control.Topics;

namespace F1Publisher.TopicSources
{
    abstract class TopicSource : ITopicSource
    {
        private string topicPath;
        private ITopicSourceUpdater topicSourceUpdater;

        public void OnActive(string topicPath, IRegisteredHandler registeredHandler, ITopicSourceUpdater topicSourceUpdater)
        {
            Log.Spew("Topic Active: \"" + topicPath + "\" (" + this.GetType().Name + ")");
            this.topicPath = topicPath;
            this.topicSourceUpdater = topicSourceUpdater;
            UpdateContent(CreateInitialContent());
            OnActivated();
        }

        protected abstract IContent CreateInitialContent();
        protected virtual void OnActivated() { }
        protected virtual void OnDeactivated() { }

        protected void UpdateContent(IContent content)
        {
            var topicSourceUpdateHandler = Handlers.TopicSourceUpdateHandler.Singleton;
            topicSourceUpdater.Update(topicPath, content, topicSourceUpdateHandler);
        }

        private void Deactivate()
        {
            if (null == topicSourceUpdater) return; // already deactive
            topicPath = null;
            topicSourceUpdater = null;
            OnDeactivated();
        }

        public void OnClosed(string topicPath)
        {
            Log.Spew(this.GetType().Name + ", Closed: " + topicPath);
            Deactivate();
        }

        public void OnStandby(string topicPath)
        {
            Log.Spew(this.GetType().Name + ", Standby: " + topicPath);
            Deactivate();
        }

        protected IContent CreateContent(double value)
        {
            return Diffusion.Content.NewContent(value.ToString("0.0#####"));
        }

        protected IContent CreateContent(UInt64 value)
        {
            return Diffusion.Content.NewContent(value.ToString());
        }

        protected IContent CreateContent(long value)
        {
            return Diffusion.Content.NewContent(value.ToString());
        }

        protected IContent CreateContent(IRecord record)
        {
            var contentFactory = Diffusion.Content;

            // Create Content wrapping the Record
            var recordContentBuilder = contentFactory.NewBuilder<IRecordContentBuilder>();
            recordContentBuilder.PutRecords(record); // because PutRecord doesn't work
            return recordContentBuilder.Build();
        }
    }
}
