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
    class ButtonStatesTopicSource : CarControlsTopicSource
    {
        private static readonly string[] sortedButtonNames;
        private static readonly IMRecord recordMetadata;

        static ButtonStatesTopicSource()
        {
            var metadataFactory = Diffusion.Metadata;
            var recordBuilder = metadataFactory.RecordBuilder("ButtonsState");

            sortedButtonNames = Enum.GetNames(typeof(DataGenerators.CarButtons));
            foreach (var buttonName in sortedButtonNames)
            {
                var integerString = metadataFactory.Integer(buttonName);
                recordBuilder.Add(integerString, 1, 1);
            }
            recordMetadata = recordBuilder.Build();
        }

        public static string[] SortedButtonNames
        {
            get
            {
                return sortedButtonNames;
            }
        }

        private readonly IRecordStructuredBuilder recordStructuredBuilder;

        public ButtonStatesTopicSource(DataGenerators.ICarControlsDataGenerator carControlsDataGenerator)
            : base(carControlsDataGenerator)
        {
            var contentFactory = Diffusion.Content;
            recordStructuredBuilder = contentFactory.NewRecordBuilder(recordMetadata);
        }

        private IContent CreateContent()
        {
            return CreateContent(recordStructuredBuilder.Build());
        }

        private void UpdateButtonField(DataGenerators.CarButtons button, bool on)
        {
            var buttonName = Enum.GetName(typeof(DataGenerators.CarButtons), button);
            recordStructuredBuilder.Set(buttonName, on ? "1" : "0");
        }

        protected override IContent CreateInitialContent()
        {
            // Fully Populate the Record
            foreach (DataGenerators.CarButtons buttonValue in Enum.GetValues(typeof(DataGenerators.CarButtons)))
                UpdateButtonField(buttonValue, CarControlsDataGenerator.ButtonState(buttonValue));

            return CreateContent();
        }

        protected override void OnActivated()
        {
            CarControlsDataGenerator.ButtonStateChanged += CarControlsDataGenerator_ButtonStateChanged;
        }

        protected override void OnDeactivated()
        {
            CarControlsDataGenerator.ButtonStateChanged -= CarControlsDataGenerator_ButtonStateChanged;
        }

        void CarControlsDataGenerator_ButtonStateChanged(object sender, DataGenerators.CarButtonEventArgs e)
        {
            UpdateButtonField(e.Button, e.On);
            UpdateContent(CreateContent());
        }
    }
}
