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
    enum CarButtons
    {
        RightQuadLeft,
        RightQuadDown,
        RightQuadRight,
        RightQuadUp,
        ShiftDown,
        ShiftUp,
        TopLeft2,
        TopRight1,
        SE,
        ST,
        TopLeft1,
        TopRight2,
        Home,
    }

    class CarButtonEventArgs : EventArgs
    {
        public readonly CarButtons Button;
        public readonly bool On;

        public CarButtonEventArgs(CarButtons button, bool on)
        {
            this.Button = button;
            this.On = on;
        }
    }

    class FloatingPointScalarEventArgs : EventArgs
    {
        public readonly double Value;

        public FloatingPointScalarEventArgs(double value)
        {
            this.Value = value;
        }
    }

    class IntegerScalarEventArgs : EventArgs
    {
        public readonly long Value;

        public IntegerScalarEventArgs(long value)
        {
            this.Value = value;
        }
    }
}