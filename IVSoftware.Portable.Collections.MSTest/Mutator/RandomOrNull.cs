using System.Collections;

namespace IVSoftware.Portable.Collections.MSTest.Mutator
{
    public class RandomOrNull : Random
    {
        private readonly Random _nullLottery;
        public RandomOrNull()
            : base() => _nullLottery = new();

        /// <summary>
        /// Deterministic where null lottery is also deterministic
        /// </summary>
        public RandomOrNull(int seed)
            : base(seed) => _nullLottery = new(seed + 1); // make null lottery deterministic also.

        public event EventHandler<OnNullEventArgs>? OnNull;
        /// <summary>
        /// Chances of null are 1 on N.
        /// </summary>
        public int OddsOfNullSample { get; set; } = 10;
        public new int? Next()
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.Next();
            }
        }
        public new int? Next(int maxValue)
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.Next(maxValue);
            }
        }
        public int? Next(int maxValue, bool inclusive = false) => Next(maxValue: maxValue + 1);

        public new int? Next(int minValue, int maxValue)
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.Next(minValue, maxValue);
            }
        }
        public int? Next(int minValue, int maxValue, bool inclusive = false) => Next(minValue: minValue, maxValue: maxValue + 1);
        public override void NextBytes(byte[] buffer) => throw new NotImplementedException("Buffer must be nullable byte.");
        public void NextBytes(byte?[] buffer)
        {
            byte[] tmp = new byte[buffer.Length];
            base.NextBytes(tmp);
            for (int i = 0; i < buffer.Length; i++)
            {
                if(RunNullLottery())
                {
                    buffer[i] = null;
                    var e = new OnNullEventArgs(i);
                    OnNull?.Invoke(this, e);
                    buffer[i] = e.BufferValueAtIndex;
                }
                else
                {
                    buffer[i] = tmp[i];
                }
            }
        }
        public override void NextBytes(Span<byte> buffer) => throw new NotImplementedException("Span must be nullable byte.");
        public void NextBytes(Span<byte?> buffer)
        {
            byte[] tmp = new byte[buffer.Length];
            base.NextBytes(tmp);
            for (int i = 0; i < buffer.Length; i++)
            {
                if(RunNullLottery())
                {
                    buffer[i] = null;
                    var e = new OnNullEventArgs(i);
                    OnNull?.Invoke(this, e);
                    buffer[i] = e.BufferValueAtIndex;
                }
                else
                {
                    buffer[i] = tmp[i];
                }
            }
        }
        public new double? NextDouble()
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.NextDouble();
            }
        }
        public new long? NextInt64()
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.NextInt64();
            }
        }
        public new long? NextInt64(long maxValue)
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.NextInt64(maxValue);
            }
        }
        public new long? NextInt64(long minValue, long maxValue)
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.NextInt64(minValue, maxValue);
            }
        }
        public new float? NextSingle()
        {
            if(RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.NextSingle();
            }
        }
        protected new double? Sample()
        {
            if (RunNullLottery())
            {
                OnNull?.Invoke(this, OnNullEventArgs.Empty);
                return null;
            }
            else
            {
                return base.Sample();
            }
        }
        private bool RunNullLottery() => _nullLottery.Next(OddsOfNullSample) == 0;

        // --------------------------------------------------------------
        // Next: Non-null counterparts (direct base calls)
        // --------------------------------------------------------------

        public int NextNotNull()
            => base.Next();

        public int NextNotNull(int maxValue, bool inclusive = false)
            => base.Next(maxValue: inclusive ? maxValue + 1 : maxValue);

        public int NextNotNull(int minValue, int maxValue, bool inclusive = false)
            => base.Next(minValue: minValue, maxValue: inclusive ? maxValue + 1 : maxValue);

        public double NextDoubleNotNull()
            => base.NextDouble();

        public long NextInt64NotNull()
            => base.NextInt64();

        public long NextInt64NotNull(long maxValue)
            => base.NextInt64(maxValue);

        public long NextInt64NotNull(long minValue, long maxValue)
            => base.NextInt64(minValue, maxValue);

        public float NextSingleNotNull()
            => base.NextSingle();

        protected double SampleNotNull()
            => base.Sample();

        public void NextBytesNotNull(byte[] buffer)
            => base.NextBytes(buffer);

        public void NextBytesNotNull(byte?[] buffer)
        {
            // Fill a temporary non-nullable buffer, then copy directly.
            byte[] tmp = new byte[buffer.Length];
            base.NextBytes(tmp);
            for (int i = 0; i < tmp.Length; i++)
            {
                buffer[i] = tmp[i];
            }
        }

        public void NextBytesNotNull(Span<byte?> buffer)
        {
            byte[] tmp = new byte[buffer.Length];
            base.NextBytes(tmp);
            for (int i = 0; i < tmp.Length; i++)
            {
                buffer[i] = tmp[i];
            }
        }

    }

    public class OnNullEventArgs : EventArgs
    {
        public OnNullEventArgs(int? bufferIndex = null)
        {
            BufferIndex = bufferIndex;
        }

        public static new OnNullEventArgs Empty { get; } = new OnNullEventArgs();
        public int? BufferIndex { get; }
        public byte? BufferValueAtIndex { get; set; } = null;
    }
}
