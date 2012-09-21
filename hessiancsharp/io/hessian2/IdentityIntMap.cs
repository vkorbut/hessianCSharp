using System.Runtime.CompilerServices;
using System.Text;

/*
 * Copyright (c) 2001-2008 Caucho Technology, Inc.  All rights reserved.
 *
 * The Apache Software License, Version 1.1
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 *
 * 3. The end-user documentation included with the redistribution, if
 *    any, must include the following acknowlegement:
 *       "This product includes software developed by the
 *        Caucho Technology (http://www.caucho.com/)."
 *    Alternately, this acknowlegement may appear in the software itself,
 *    if and wherever such third-party acknowlegements normally appear.
 *
 * 4. The names "Burlap", "Resin", and "Caucho" must not be used to
 *    endorse or promote products derived from this software without prior
 *    written permission. For written permission, please contact
 *    info@caucho.com.
 *
 * 5. Products derived from this software may not be called "Resin"
 *    nor may "Resin" appear in their names without prior written
 *    permission of Caucho Technology.
 *
 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED.  IN NO EVENT SHALL CAUCHO TECHNOLOGY OR ITS CONTRIBUTORS
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY,
 * OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
 * OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
 * OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * @author Scott Ferguson
 */

namespace hessiancsharp.io.hessian2
{
    ///
    /// <summary> * The IntMap provides a simple hashmap from keys to integers.  The API is
    /// * an abbreviation of the HashMap collection API.
    /// *
    /// * <p>The convenience of IntMap is avoiding all the silly wrapping of
    /// * integers. </summary>
    /// 
    public class IdentityIntMap
    {
        ///  
        ///   <summary> * Encoding of a null entry.  Since NULL is equal to Integer.MIN_VALUE,
        ///   * it's impossible to distinguish between the two. </summary>
        ///   
        public const int NULL = int.MinValue + 1;

        private object[] _keys;
        private int[] _values;

        private int _size;
        private int _prime;

        ///  
        ///   <summary> * Create a new IntMap.  Default size is 16. </summary>
        ///   
        public IdentityIntMap(int capacity)
        {
            _keys = new object[capacity];
            _values = new int[capacity];

            _prime = getBiggestPrime(_keys.Length);
            _size = 0;
        }

        ///  
        ///   <summary> * Clear the hashmap. </summary>
        ///   
        public virtual void clear()
        {
            object[] keys = _keys;
            int[] values = _values;

            for (int i = keys.Length - 1; i >= 0; i--)
            {
                keys[i] = null;
                values[i] = 0;
            }

            _size = 0;
        }
        ///  
        ///   <summary> * Returns the current number of entries in the map. </summary>
        ///   
        public int size()
        {
            return _size;
        }

        ///  
        ///   <summary> * Puts a new value in the property table with the appropriate flags </summary>
        ///   
        public int get(object key)
        {
            int prime = _prime;
            int hash = RuntimeHelpers.GetHashCode(key) % prime;
            // int hash = key.hashCode() & mask;
            object[] keys = _keys;

            while (true)
            {
                object mapKey = keys[hash];

                if (mapKey == null)
                {
                    return NULL;
                }
                if (mapKey == key)
                {
                    return _values[hash];
                }

                hash = (hash + 1) % prime;
            }
        }

        ///  
        ///   <summary> * Puts a new value in the property table with the appropriate flags </summary>
        ///   
        public int put(object key, int value, bool isReplace)
        {
            int prime = _prime;
            int hash = RuntimeHelpers.GetHashCode(key) % prime;
            // int hash = key.hashCode() % prime;

            object[] keys = _keys;

            while (true)
            {
                object testKey = keys[hash];

                if (testKey == null)
                {
                    keys[hash] = key;
                    _values[hash] = value;

                    _size++;

                    if (keys.Length <= 4 * _size)
                    {
                        resize(4 * keys.Length);
                    }

                    return value;
                }
                if (key != testKey)
                {
                    hash = (hash + 1) % prime;

                    continue;
                }
                if (isReplace)
                {
                    int old = _values[hash];

                    _values[hash] = value;

                    return old;
                }
                return _values[hash];
            }
        }

        ///  
        ///   <summary> * Removes a value in the property table. </summary>
        ///   
        public void remove(object key)
        {
            if (put(key, -1, true) != -1)
            {
                _size--;
            }
        }

        ///  
        ///   <summary> * Expands the property table </summary>
        ///   
        private void resize(int newSize)
        {
            object[] keys = _keys;
            int[] values = _values;

            _keys = new object[newSize];
            _values = new int[newSize];
            _size = 0;

            _prime = getBiggestPrime(_keys.Length);

            for (int i = keys.Length - 1; i >= 0; i--)
            {
                object key = keys[i];

                if (key != null)
                {
                    put(key, values[i], true);
                }
            }
        }

        protected static int HashCode(object value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }

        public override string ToString()
        {
            StringBuilder sbuf = new StringBuilder();

            sbuf.Append("IntMap[");
            bool isFirst = true;

            for (int i = 0; i <= _keys.Length; i++)
            {
                if (_keys[i] != null)
                {
                    if (! isFirst)
                    {
                        sbuf.Append(", ");
                    }

                    isFirst = false;
                    sbuf.Append(_keys[i]);
                    sbuf.Append(":");
                    sbuf.Append(_values[i]);
                }
            }
            sbuf.Append("]");

            return sbuf.ToString();
        }

        public static readonly int[] PRIMES = { 1, 2, 3, 7, 13, 31, 61, 127, 251, 509, 1021, 2039, 4093, 8191, 16381, 32749, 65521, 131071, 262139, 524287, 1048573, 2097143, 4194301, 8388593, 16777213, 33554393, 67108859, 134217689, 268435399 };

        public static int getBiggestPrime(int value)
        {
            for (int i = PRIMES.Length - 1; i >= 0; i--)
            {
                if (PRIMES[i] <= value)
                {
                    return PRIMES[i];
                }
            }

            return 2;
        }
    }
}