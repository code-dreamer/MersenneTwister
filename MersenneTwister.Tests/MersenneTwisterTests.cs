using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace MersenneTwister.Tests
{
    [TestClass]
    public class MersenneTwisterTests
    {
        private const int TestCount = 1000;

        [TestMethod]
        public void ValuesShouldBeInRange()
        {
            var generator = new MersenneTwisterGenerator();

            var rand = new Random();
            int minVal = rand.Next(1, int.MaxValue);

            int maxVal;
            do
            {
                maxVal = rand.Next(minVal, int.MaxValue);
            } while (maxVal <= minVal);

            for (int i = 0; i < TestCount; i++)
            {
                var val = generator.Next((ulong)minVal, (ulong)maxVal);
                Assert.IsTrue(minVal <= (int)val && (int)val <= maxVal);
            }
        }

        [TestMethod]
        public void AllValuesShouldBeGenerated()
        {
            ulong minVal = 0;
            ulong maxVal = (ulong)new Random().Next((int)(minVal + 1), 500);

            var possibleValues = Enumerable.Range((int)minVal, (int)maxVal).ToList();
            var generator = new MersenneTwisterGenerator();
            ulong attempts = 0;
            const int maxAttempts = int.MaxValue;
            while (possibleValues.Count != 0)
            {
                var val = (int)generator.Next(minVal, maxVal);

                Assert.IsTrue((int)minVal <= val && val <= (int)maxVal);
                int ind = possibleValues.IndexOf(val);
                if (ind != -1)
                    possibleValues.RemoveAt(ind);

                if (attempts++ > maxAttempts)
                    throw new Exception("Max attempts exceeds");
            }
        }

        [TestMethod]
        public void SeedTest()
        {
            for (int i = 0; i < TestCount; i++)
            {
                var generator = new MersenneTwisterGenerator();
                var val1 = generator.Next();

                generator = new MersenneTwisterGenerator();
                var val2 = generator.Next();

                Assert.IsTrue(val1 != val2);
            }

            for (int i = 0; i < TestCount; i++)
            {
                var generator = new MersenneTwisterGenerator();
                var val1 = generator.Next();

                generator.InitSeed();
                var val2 = generator.Next();

                Assert.IsTrue(val1 != val2);
            }
        }
    }
}
