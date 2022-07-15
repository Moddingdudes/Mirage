namespace Mirage
{
    // implementation of N-day EMA
    // it calculates an exponential moving average roughly equivalent to the last n observations
    // https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average
    public class ExponentialMovingAverage
    {
        private readonly float alpha;
        private bool initialized;

        public ExponentialMovingAverage(int n)
        {
            // standard N-day EMA alpha calculation
            this.alpha = 2.0f / (n + 1);
        }

        public void Reset()
        {
            this.initialized = false;
            this.Value = 0;
            this.Var = 0;
        }

        public void Add(double newValue)
        {
            // simple algorithm for EMA described here:
            // https://en.wikipedia.org/wiki/Moving_average#Exponentially_weighted_moving_variance_and_standard_deviation
            if (this.initialized)
            {
                var delta = newValue - this.Value;
                this.Value += this.alpha * delta;
                this.Var = (1 - this.alpha) * (this.Var + this.alpha * delta * delta);
            }
            else
            {
                this.Value = newValue;
                this.initialized = true;
            }
        }

        public double Value { get; private set; }

        public double Var { get; private set; }
    }
}
