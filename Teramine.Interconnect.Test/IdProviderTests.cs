#region License
// Copyright (c) 2019 Teramine Ltd
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using NUnit.Framework;
using System;

namespace Teramine.Interconnect.Test
{
    [TestFixture]
    public class IdProviderTests
    {
        [Test]
        public void SimpleNextRemoveTest()
        {
            IdProvider provider = new IdProvider(0);

            Assert.AreEqual(0, provider.Next());
            Assert.AreEqual(1, provider.Next());
            Assert.AreEqual(2, provider.Next());
            Assert.AreEqual(3, provider.Next());
            Assert.AreEqual(4, provider.Next());

            provider.Remove(1);
            provider.Remove(3);

            Assert.AreEqual(1, provider.Next());
            Assert.AreEqual(3, provider.Next());
            Assert.AreEqual(5, provider.Next());
            Assert.AreEqual(6, provider.Next());
            Assert.AreEqual(7, provider.Next());

            provider.Remove(5);
            provider.Remove(7);

            Assert.AreEqual(5, provider.Next());
            Assert.AreEqual(7, provider.Next());
            Assert.AreEqual(8, provider.Next());
            Assert.AreEqual(9, provider.Next());
            Assert.AreEqual(10, provider.Next());
        }

        [Test]
        public void IncrementTest()
        {
            IdProvider provider = new IdProvider(0, 5);

            Assert.AreEqual(0, provider.Next());
            Assert.AreEqual(5, provider.Next());
            Assert.AreEqual(10, provider.Next());
            Assert.AreEqual(15, provider.Next());
            Assert.AreEqual(20, provider.Next());

            // should do nothing
            provider.Remove(1);

            Assert.AreEqual(25, provider.Next());
            Assert.AreEqual(30, provider.Next());

            provider.Remove(5);
            provider.Remove(25);

            Assert.AreEqual(5, provider.Next());
            Assert.AreEqual(25, provider.Next());
            Assert.AreEqual(35, provider.Next());
            Assert.AreEqual(40, provider.Next());
        }

        [Test]
        public void LimitTest()
        {
            IdProvider provider = new IdProvider(0, 1, 5);

            Assert.AreEqual(0, provider.Next());
            Assert.AreEqual(1, provider.Next());
            Assert.AreEqual(2, provider.Next());
            Assert.AreEqual(3, provider.Next());
            Assert.AreEqual(4, provider.Next());
            Assert.AreEqual(5, provider.Next());

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Next());

            provider.Remove(2);
            provider.Remove(3);

            Assert.DoesNotThrow(() => _ = provider.Next());
            Assert.DoesNotThrow(() => _ = provider.Next());

            Assert.Throws<ArgumentOutOfRangeException>(() => provider.Next());
        }
    }
}
