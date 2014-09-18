using System;

namespace F1Publisher
{
    struct RefreshInterval
    {
        private const uint sleepPerSecond = 1000; // i.e. SleepDuration is in milliseconds
        private const uint minFrequency = 1;
        private const uint maxFrequency = sleepPerSecond;
        private const uint defaultFrequency = 50;

        public readonly uint Frequency;
        public readonly uint SleepDuration;

        public RefreshInterval(uint frequency)
        {
            Frequency = frequency;
            SleepDuration = (uint)((double)sleepPerSecond / (double)frequency);
        }

        private RefreshInterval? AdjustSleepDuration(int delta)
        {
            var frequency = Frequency;
            var sleepDuration = SleepDuration;

            while (Frequency == frequency)
            {
                sleepDuration = (uint)(sleepDuration + delta);
                frequency = (0==sleepDuration) ? uint.MaxValue : (uint)Math.Floor((double)sleepPerSecond / (double)sleepDuration);
            }

            if (frequency < minFrequency) return null;
            if (frequency > maxFrequency) return null;

            return new RefreshInterval(frequency);
        }

        public RefreshInterval? Increase()
        {
            return AdjustSleepDuration(-1);
        }

        public RefreshInterval? Decrease()
        {
            return AdjustSleepDuration(1);
        }

        public static RefreshInterval Default = new RefreshInterval(defaultFrequency);
        public static RefreshInterval Minimum = new RefreshInterval(minFrequency);
        public static RefreshInterval Maximum = new RefreshInterval(maxFrequency);
    }

    class RefreshIntervalEventArgs : EventArgs
    {
        public readonly RefreshInterval RefreshInterval;

        public RefreshIntervalEventArgs(RefreshInterval refreshInterval)
        {
            RefreshInterval = refreshInterval;
        }
    }

    class RefreshIntervalManager
    {
        public event EventHandler<RefreshIntervalEventArgs> RefreshIntervalChanged;

        private RefreshInterval refreshInterval = RefreshInterval.Default;

        public RefreshInterval RefreshInterval 
        { 
            get
            {
                return refreshInterval;
            }
        } 

        private void SetRefreshInterval(RefreshInterval? refreshInterval)
        {
            if (!refreshInterval.HasValue) return; // no value (request to modify to out of range)
            SetRefreshInterval(refreshInterval.Value);
        }

        private void SetRefreshInterval(RefreshInterval refreshInterval)
        {
            if (refreshInterval.Equals(this.refreshInterval)) return; // no change
            this.refreshInterval = refreshInterval;
            OnChanged(new RefreshIntervalEventArgs(refreshInterval));
        }

        public void Decrease()
        {
            SetRefreshInterval(refreshInterval.Decrease());
        }

        public void Increase()
        {
            SetRefreshInterval(refreshInterval.Increase());
        }

        public void Reset()
        {
            SetRefreshInterval(RefreshInterval.Default);
        }

        public void Minimum()
        {
            SetRefreshInterval(RefreshInterval.Minimum);
        }

        public void Maximum()
        {
            SetRefreshInterval(RefreshInterval.Maximum);
        }

        protected virtual void OnChanged(RefreshIntervalEventArgs e)
        {
            var handler = RefreshIntervalChanged;
            if (null == handler) return;
            handler(this, e);
        }
    }
}
