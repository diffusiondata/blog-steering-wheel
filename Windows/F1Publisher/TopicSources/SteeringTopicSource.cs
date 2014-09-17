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
    class SteeringTopicSource : CarControlsTopicSource
    {
        public SteeringTopicSource(DataGenerators.ICarControlsDataGenerator carControlsDataGenerator) 
            : base(carControlsDataGenerator) 
        { }

        protected override IContent CreateInitialContent()
        {
            return CreateContent(CarControlsDataGenerator.SteeringValue);
        }

        protected override void OnActivated()
        {
            CarControlsDataGenerator.SteeringValueChanged += carControlsDataGenerator_SteeringValueChanged;
        }

        protected override void OnDeactivated()
        {
            CarControlsDataGenerator.SteeringValueChanged -= carControlsDataGenerator_SteeringValueChanged;
        }

        void carControlsDataGenerator_SteeringValueChanged(object sender, DataGenerators.FloatingPointScalarEventArgs e)
        {
            UpdateContent(CreateContent(e.Value));
        }
    }
}
