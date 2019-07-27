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

using System;
using System.Collections.Generic;
using System.Text;

namespace Teramine.Interconnect
{
    public class IdProvider
    {
        private uint _start;
        private uint _increment;
        private uint _max;
        private SortedList<uint, uint> _list = new SortedList<uint, uint>();


        public IdProvider(uint start, uint increment = 1, uint max = uint.MaxValue)
        {
            _start = 0;
            _increment = increment;
            _max = max;
        }

        public uint Next()
        {
            uint value = _start;

            for(uint i = 0; i < _list.Count && value <= _max; value += _increment)
            {
                if(!_list.ContainsKey(value))
                {
                    _list.Add(value, value);
                    return value;
                }
            }

            if (value > _max)
                throw new ArgumentOutOfRangeException();

            _list.Add(value, value);
            return value;
        }

        public void Reset()
        {
            _list.Clear();
        }

        public void Remove(uint value)
        {
            _list.Remove(value);
            _list.TrimExcess();
        }
    }
}
