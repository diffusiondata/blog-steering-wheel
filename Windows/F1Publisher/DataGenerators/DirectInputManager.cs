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
using SlimDX.DirectInput;

namespace F1Publisher.DataGenerators
{
    class DirectInputManager : ICarControlsDataGenerator, IDisposable
    {
        private enum JoystickTypes
        {
            Driving,
            Joystick,
        };

        // Events defined in ICarControlsDataGenerator
        public event EventHandler<CarButtonEventArgs> ButtonStateChanged;
        public event EventHandler<FloatingPointScalarEventArgs> SteeringValueChanged;
        public event EventHandler<FloatingPointScalarEventArgs> BrakingValueChanged;
        public event EventHandler<FloatingPointScalarEventArgs> AccelerationValueChanged;

        private readonly Joystick joystick;
        private readonly JoystickTypes joystickType;

        private bool[] lastReportedButtons;
        private int lastReportedSteeringRawValue = int.MinValue;
        private int lastReportedBrakingRawValue = int.MinValue;
        private int lastReportedAccelerationRawValue = int.MinValue;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public DirectInputManager()
        {
            var directInput = new DirectInput();

            // Prefer a Driving device but make do with fallback to a Joystick if we have to
            var deviceInstance = FindAttachedDevice(directInput, DeviceType.Driving);
            if (null == deviceInstance)
                deviceInstance = FindAttachedDevice(directInput, DeviceType.Joystick);
            if (null == deviceInstance)
                throw new Exception("No Driving or Joystick devices attached.");
            joystickType = (DeviceType.Driving == deviceInstance.Type ? JoystickTypes.Driving : JoystickTypes.Joystick);

            // A little debug spew is often good for you
            Log.Spew("First Suitable Device Selected \"" + deviceInstance.InstanceName + "\":");
            Log.Spew("\tProductName: " + deviceInstance.ProductName);
            Log.Spew("\tType: " + deviceInstance.Type);
            Log.Spew("\tSubType: " + deviceInstance.Subtype);

            // Data for both Driving and Joystick device types is received via Joystick
            joystick = new Joystick(directInput, deviceInstance.InstanceGuid);
            IntPtr consoleWindowHandle = GetConsoleWindow();
            if (IntPtr.Zero != consoleWindowHandle)
            {
                CooperativeLevel cooperativeLevel = CooperativeLevel.Background | CooperativeLevel.Nonexclusive;
                Log.Spew("Console window cooperative level: " + cooperativeLevel);
                if (!joystick.SetCooperativeLevel(consoleWindowHandle, cooperativeLevel).IsSuccess)
                    throw new Exception("Failed to set cooperative level for DirectInput device in console window.");
            }
            var result = joystick.Acquire();
            if (!result.IsSuccess)
                throw new Exception("Failed to acquire DirectInput device.");

            Log.Spew("Joystick acquired.");
        }

        private DeviceInstance FindAttachedDevice(DirectInput directInput, DeviceType deviceType)
        {
            var devices = directInput.GetDevices(deviceType, DeviceEnumerationFlags.AttachedOnly);
            return devices.Count > 0 ? devices[0] : null;
        }

        public void Update()
        {
            var joystickState = joystick.GetCurrentState();
            UpdateButtons(joystickState);
            UpdateBraking(joystickState);
            UpdateAcceleration(joystickState);
            UpdateSteering(joystickState);
        }

        private void UpdateButtons(JoystickState joystickState)
        {
            var buttons = joystickState.GetButtons();

            if (null == lastReportedButtons)
            {
                // We don't raise any events on the first call to this method.
                lastReportedButtons = buttons;
                return;
            }

            if (buttons.Length != lastReportedButtons.Length)
                throw new Exception("Button array lengths differ. This is unexpected.");

            for (int i = 0; i < buttons.Length; i++)
            {
                // Only report this button if state has flipped (on vs off).
                if (buttons[i] != lastReportedButtons[i])
                {
                    CarButtonEventArgs e = CreateCarButtonEventArgs(i, buttons[i]);
                    Log.Spew("Button " + i + ": " + (buttons[i] ? "On" : "Off") + (null==e ? " [not tracked]" : ""));

                    if (null != e)
                        OnButtonStateChanged(e);
                }
            }

            lastReportedButtons = buttons;
        }

        /// <summary>
        /// This method will only return an object if the button index is mapped for the current device.
        /// Otherwise, it will return null.
        /// </summary>
        /// <param name="i">The button index.</param>
        /// <param name="on">The button state (true being pushed in or on).</param>
        private CarButtonEventArgs CreateCarButtonEventArgs(int i, bool on)
        {
            switch (joystickType)
            {
                case JoystickTypes.Driving:
                    // based on observation of Thrustmaster Ferrari Challenge Wheel.
                    switch (i)
                    {
                        case 0: return new CarButtonEventArgs(CarButtons.RightQuadLeft, on);
                        case 1: return new CarButtonEventArgs(CarButtons.RightQuadDown, on);
                        case 2: return new CarButtonEventArgs(CarButtons.RightQuadRight, on);
                        case 3: return new CarButtonEventArgs(CarButtons.RightQuadUp, on);
                        case 4: return new CarButtonEventArgs(CarButtons.ShiftDown, on);
                        case 5: return new CarButtonEventArgs(CarButtons.ShiftUp, on);
                        case 6: return new CarButtonEventArgs(CarButtons.TopLeft2, on);
                        case 7: return new CarButtonEventArgs(CarButtons.TopRight1, on);
                        case 8: return new CarButtonEventArgs(CarButtons.SE, on);
                        case 9: return new CarButtonEventArgs(CarButtons.ST, on);
                        case 10: return new CarButtonEventArgs(CarButtons.TopLeft1, on);
                        case 11: return new CarButtonEventArgs(CarButtons.TopRight2, on);
                        case 12: return new CarButtonEventArgs(CarButtons.Home, on);
                    }
                    break;

                case JoystickTypes.Joystick:
                    // based on observation of Logitech Attack 3 Joystick.
                    switch (i)
                    {
                        case 0: return new CarButtonEventArgs(CarButtons.ShiftUp, on); // Fire button
                        case 1: return new CarButtonEventArgs(CarButtons.ShiftDown, on); // 2
                        case 2: return new CarButtonEventArgs(CarButtons.Home, on); // 3
                        case 3: return new CarButtonEventArgs(CarButtons.SE, on); // 4
                        case 4: return new CarButtonEventArgs(CarButtons.ST, on); // 5
                    }
                    break;
            }

            // no mapping found
            return null;
        }

        private double RangeSteering(int rawValue)
        {
            // As observed on the Ferrari Challenge Wheel:
            //   0 = Full Lock Left; 32767 = Centre; 65535 = Full Lock Right
            // We're presenting normalised values in the range -1.0 (180 degrees to Left) to 0.0 (Centre) to 1.0 (180 degrees to Right).
            // Full Lock on the Ferrari Challenge Wheel is (approximately) 70% (0.7x) of a half turn in either direction.
            const double ferrariChallengeWheelCalibrarion = 0.7;
            return ferrariChallengeWheelCalibrarion * ((double)(rawValue - 32767) / (double)32768);
        }

        private void UpdateSteering(JoystickState joystickState)
        {
            var rawValue = joystickState.X;
            if (rawValue == lastReportedSteeringRawValue)
                return;
            
            OnSteeringValueChanged(new FloatingPointScalarEventArgs(RangeSteering(rawValue)));
            lastReportedSteeringRawValue = rawValue;
        }

        private double RangePedal(int rawValue)
        {
            // As observed on the Ferrari Challenge Wheel:
            //   0 = Fully Pressed Down (full force); 65535 = Not In Use (no force)
            // As observed on the Logitech Attack 3 Joystick:
            //   0 = Fully Pulled Back (full braking force); 65536 = Fully Pushed Forward (full acceleration force)
            // We're presenting values in the range 0.0 (no force) to 1.0 (full force).
            return 1.0 - ((double)rawValue / (double)65535);
        }

        private void UpdateBraking(JoystickState joystickState)
        {
            var rawValue = joystickState.Y;
            if (rawValue == lastReportedBrakingRawValue)
                return;

            var value = RangePedal(rawValue);

            switch (joystickType)
            {
                case JoystickTypes.Driving:
                    OnBrakingValueChanged(new FloatingPointScalarEventArgs(value));
                    break;

                case JoystickTypes.Joystick:
                    OnBrakingValueChanged(new FloatingPointScalarEventArgs(1.0 - Math.Min(value * 2.0, 1.0)));
                    OnAccelerationValueChanged(new FloatingPointScalarEventArgs(Math.Max((value - 0.5) * 2.0, 0.0)));
                    break;
            }

            lastReportedBrakingRawValue = rawValue;
        }

        private void UpdateAcceleration(JoystickState joystickState)
        {
            var rawValue = joystickState.RotationZ;
            if (rawValue == lastReportedAccelerationRawValue)
                return;

            if (joystickType == JoystickTypes.Driving)
                OnAccelerationValueChanged(new FloatingPointScalarEventArgs(RangePedal(rawValue)));

            lastReportedAccelerationRawValue = rawValue;
        }

        public void Dispose()
        {
            if (null != joystick)
                joystick.Dispose();
        }

        public bool ButtonState(CarButtons button)
        {
            if (null == lastReportedButtons)
                return false; // not updated yet

            var index = (int)button;
            if (index < 0 || index >= lastReportedButtons.Length)
                return false; // out of range

            return lastReportedButtons[index];
        }

        public double SteeringValue
        {
            get 
            {
                return RangeSteering(lastReportedSteeringRawValue);
            }
        }

        public double BrakingValue
        {
            get 
            {
                return RangePedal(lastReportedBrakingRawValue);
            }
        }

        public double AccelerationValue
        {
            get
            {
                return RangePedal(lastReportedAccelerationRawValue);
            }
        }

        protected virtual void OnButtonStateChanged(CarButtonEventArgs e)
        {
            var handler = ButtonStateChanged;
            if (null == handler) return;
            handler(this, e);
        }

        protected virtual void OnSteeringValueChanged(FloatingPointScalarEventArgs e)
        {
            var handler = SteeringValueChanged;
            if (null == handler) return;
            handler(this, e);
        }

        protected virtual void OnBrakingValueChanged(FloatingPointScalarEventArgs e)
        {
            var handler = BrakingValueChanged;
            if (null == handler) return;
            handler(this, e);
        }

        protected virtual void OnAccelerationValueChanged(FloatingPointScalarEventArgs e)
        {
            var handler = AccelerationValueChanged;
            if (null == handler) return;
            handler(this, e);
        }
    }
}
