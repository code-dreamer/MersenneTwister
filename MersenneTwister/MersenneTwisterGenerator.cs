using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MersenneTwister
{
    /// <summary>
    /// Mersenne Twister pseudorandom number generator implementation
    /// https://en.wikipedia.org/wiki/Mersenne_Twister
    /// </summary>
    public class MersenneTwisterGenerator
    {
        private const int N = 624;
        private const int M = 397;
        private readonly ulong[] _mt = new ulong[N]; // the array for the state vector 
        private static ulong _mti = N + 1; // mti==N+1 means mt[N] is not initialized

        const ulong MATRIX_A = 0x9908b0dfUL;    // constant vector a
        const ulong UPPER_MASK = 0x80000000UL;  // most significant w-r bits
        const ulong LOWER_MASK = 0x7fffffffUL;  // least significant r bits

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryPerformanceFrequency(out long frequency);

        /// <summary>
        /// Initializes _mt[N] with a seed
        /// </summary>
        private void InitGenrand(ulong s)
        {
            _mt[0] = s & 0xffffffffUL;
            for (_mti = 1; _mti < N; _mti++)
            {
                _mt[_mti] =
                    (1812433253UL * (_mt[_mti - 1] ^ (_mt[_mti - 1] >> 30)) + _mti);
                /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
                /* In the previous versions, MSBs of the seed affect   */
                /* only MSBs of the array _mt[].                        */
                /* 2002/01/09 modified by Makoto Matsumoto             */
                _mt[_mti] &= 0xffffffffUL;
                /* for >32 bit machines */
            }
        }

        public MersenneTwisterGenerator()
        {
            InitSeed();
        }

        /// <summary>
        /// Init seed value automatically
        /// </summary>
        public void InitSeed()
        {
            var key = GenerateKey();
            InitSeed(key);
        }

        /// <summary>
        /// Generates a random number on [0,0xffffffff]-interval
        /// </summary>
        public ulong Next()
        {
            ulong y;
            ulong[] mag01 = { 0x0UL, MATRIX_A };
            /* mag01[x] = x * MATRIX_A  for x=0,1 */

            if (_mti >= N)
            { /* generate N words at one time */
                int kk;

                if (_mti == N + 1)   /* if init_genrand() has not been called, */
                    InitGenrand(5489UL); /* a default initial seed is used */

                for (kk = 0; kk < N - M; kk++)
                {
                    y = (_mt[kk] & UPPER_MASK) | (_mt[kk + 1] & LOWER_MASK);
                    _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1UL];
                }
                for (; kk < N - 1; kk++)
                {
                    y = (_mt[kk] & UPPER_MASK) | (_mt[kk + 1] & LOWER_MASK);
                    _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1UL];
                }
                y = (_mt[N - 1] & UPPER_MASK) | (_mt[0] & LOWER_MASK);
                _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1UL];

                _mti = 0;
            }

            y = _mt[_mti++];

            /* Tempering */
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9d2c5680UL;
            y ^= (y << 15) & 0xefc60000UL;
            y ^= (y >> 18);

            //return (int)(y % 100);
            return y;
        }

        public ulong Next(ulong minValue, ulong maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentException($"{nameof(minValue)} should be less than {nameof(maxValue)}");

            return Next() % (maxValue - minValue + 1) + minValue;
        }

        public int Next(int minValue, int maxValue)
        {
            checked
            {
                return (int)Next((ulong)minValue, (ulong)maxValue);
            }
        }

        private static ulong[] GenerateKey()
        {
            var key = new List<ulong>();

            void AddStrBytesToKey(string s, List<ulong> arr)
            {
                var bytes = Encoding.ASCII.GetBytes(s);
                arr.AddRange(bytes.Select(b => (ulong)b));
            }

            AddStrBytesToKey(Environment.MachineName, key);

            var now = DateTime.Now;
            var dateTimeNow = now.ToLongDateString() + now.ToLongTimeString();
            AddStrBytesToKey(dateTimeNow, key);

            if (!QueryPerformanceCounter(out long performanceCount))
                throw new InvalidOperationException("QueryPerformanceCounter failed!");

            AddStrBytesToKey(performanceCount.ToString(), key);

            if (!QueryPerformanceFrequency(out long frequency))
                throw new InvalidOperationException("QueryPerformanceFrequency failed!");

            AddStrBytesToKey(frequency.ToString(), key);

            var drive = DriveInfo.GetDrives().First(d =>
                string.Compare(d.Name, "c:\\", StringComparison.OrdinalIgnoreCase) == 0);

            string str = drive.AvailableFreeSpace.ToString() + drive.TotalFreeSpace + drive.TotalSize;
            AddStrBytesToKey(str, key);

            return key.ToArray();
        }

        /// <summary>
        /// Initialize by an array with array-length
        /// init_key is the array for initializing keys
        /// key_length is its length
        /// </summary>
        private void InitSeed(IList<ulong> key)
        {
            var keyLength = (ulong)key.Count;
            InitGenrand(19650218UL);
            ulong i = 1;
            ulong j = 0;
            ulong k = (N > keyLength ? N : keyLength);
            for (; k != 0; k--)
            {
                _mt[i] = (_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1664525UL))
                         + key[(int)j] + j; /* non linear */
                _mt[i] &= 0xffffffffUL; /* for WORDSIZE > 32 machines */
                i++;
                j++;
                if (i >= N)
                {
                    _mt[0] = _mt[N - 1];
                    i = 1;
                }

                if (j >= keyLength) j = 0;
            }

            for (k = N - 1; k != 0; k--)
            {
                _mt[i] = (_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 30)) * 1566083941UL))
                         - i; /* non linear */
                _mt[i] &= 0xffffffffUL; /* for WORDSIZE > 32 machines */
                i++;
                if (i >= N)
                {
                    _mt[0] = _mt[N - 1];
                    i = 1;
                }
            }

            _mt[0] = 0x80000000UL; /* MSB is 1; assuring non-zero initial array */

            // AJQ, set no numbers available
            _mti = N;
        }
    }
}
