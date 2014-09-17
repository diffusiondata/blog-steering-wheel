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

namespace F1Publisher.DataGenerators
{
    class Car : ICarStateDataGenerator
    {
        public event EventHandler<IntegerScalarEventArgs> GearValueChanged;

        private readonly RefreshIntervalManager refreshIntervalManager;
        private uint _gear = 1;

        public Car(ICarControlsDataGenerator carControlsDataGenerator, RefreshIntervalManager refreshIntervalManager)
        {
            this.refreshIntervalManager = refreshIntervalManager;
            carControlsDataGenerator.ButtonStateChanged += carControlsDataGenerator_ButtonStateChanged;
        }

        void carControlsDataGenerator_ButtonStateChanged(object sender, CarButtonEventArgs e)
        {
            if (!e.On) return;
            switch (e.Button)
            {
                case CarButtons.ShiftUp:
                    GearShiftUp();
                    break;

                case CarButtons.ShiftDown:
                    GearShiftDown();
                    break;

                case CarButtons.SE:
                    refreshIntervalManager.Decrease();
                    break;

                case CarButtons.ST:
                    refreshIntervalManager.Increase();
                    break;

                case CarButtons.Home:
                    refreshIntervalManager.Reset();
                    break;
            }
        }

        private void GearShiftUp()
        {
            if (8 == _gear) return;
            OnGearValueChanged(new IntegerScalarEventArgs(++_gear));
        }

        private void GearShiftDown()
        {
            if (1 == _gear) return;
            OnGearValueChanged(new IntegerScalarEventArgs(--_gear));
        }

        public long gearValue
        {
            get 
            {
                return _gear;
            }
        }

        protected virtual void OnGearValueChanged(IntegerScalarEventArgs e)
        {
            var handler = GearValueChanged;
            if (null == handler) return;
            handler(this, e);
        }
    }
}
