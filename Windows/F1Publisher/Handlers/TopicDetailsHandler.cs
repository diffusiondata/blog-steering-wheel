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
using PushTechnology.ClientInterface.Client.Features;
using PushTechnology.ClientInterface.Client.Topics;
using PushTechnology.ClientInterface.Utils;

namespace F1Publisher.Handlers
{
    class TopicDetailsHandler : ITopicDetailsContextCallback<Nothing>
    {
        public event EventHandler<TopicDetailsEventArgs> Success;
        public event EventHandler Failure;

        public void OnTopicDetails(Nothing context, string topicPath, ITopicDetails details)
        {
            OnSuccess(new TopicDetailsEventArgs(topicPath, details));
        }

        public void OnTopicUnknown(Nothing context, string topicPath)
        {
            OnSuccess(new TopicDetailsEventArgs(topicPath, null));
        }

        public void OnDiscard(Nothing context)
        {
            OnFailure(EventArgs.Empty);
        }

        protected virtual void OnSuccess(TopicDetailsEventArgs e)
        {
            var handler = Success;
            if (null == handler) return;
            handler(this, e);
        }

        protected virtual void OnFailure(EventArgs e)
        {
            var handler = Failure;
            if (null == handler) return;
            handler(this, e);
        }
    }
}
