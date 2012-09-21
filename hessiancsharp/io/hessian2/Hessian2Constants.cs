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
    public class Hessian2Constants
    {
        public const int BC_BINARY = 'B'; // final chunk
        public const int BC_BINARY_CHUNK = 'A'; // non-final chunk
        public const int BC_BINARY_DIRECT = 0x20; // 1-byte length binary
        public const int BINARY_DIRECT_MAX = 0x0f;
        public const int BC_BINARY_SHORT = 0x34; // 2-byte length binary
        public const int BINARY_SHORT_MAX = 0x3ff; // 0-1023 binary
        public const int BC_CLASS_DEF = 'C'; // object/class definition
        public const int BC_DATE = 0x4a; // 64-bit millisecond UTC date
        public const int BC_DATE_MINUTE = 0x4b; // 32-bit minute UTC date
        public const int BC_DOUBLE = 'D'; // IEEE 64-bit double
        public const int BC_DOUBLE_ZERO = 0x5b;
        public const int BC_DOUBLE_ONE = 0x5c;
        public const int BC_DOUBLE_BYTE = 0x5d;
        public const int BC_DOUBLE_SHORT = 0x5e;
        public const int BC_DOUBLE_MILL = 0x5f;
        public const int BC_FALSE = 'F'; // boolean false
        public const int BC_INT = 'I'; // 32-bit int
        public const int INT_DIRECT_MIN = -0x10;
        public const int INT_DIRECT_MAX = 0x2f;
        public const int BC_INT_ZERO = 0x90;
        public const int INT_BYTE_MIN = -0x800;
        public const int INT_BYTE_MAX = 0x7ff;
        public const int BC_INT_BYTE_ZERO = 0xc8;
        public const int BC_END = 'Z';
        public const int INT_SHORT_MIN = -0x40000;
        public const int INT_SHORT_MAX = 0x3ffff;
        public const int BC_INT_SHORT_ZERO = 0xd4;
        public const int BC_LIST_VARIABLE = 0x55;
        public const int BC_LIST_FIXED = 'V';
        public const int BC_LIST_VARIABLE_UNTYPED = 0x57;
        public const int BC_LIST_FIXED_UNTYPED = 0x58;
        public const int BC_LIST_DIRECT = 0x70;
        public const int BC_LIST_DIRECT_UNTYPED = 0x78;
        public const int LIST_DIRECT_MAX = 0x7;
        public const int BC_LONG = 'L'; // 64-bit signed integer
        public const long LONG_DIRECT_MIN = -0x08;
        public const long LONG_DIRECT_MAX = 0x0f;
        public const int BC_LONG_ZERO = 0xe0;
        public const long LONG_BYTE_MIN = -0x800;
        public const long LONG_BYTE_MAX = 0x7ff;
        public const int BC_LONG_BYTE_ZERO = 0xf8;
        public const int LONG_SHORT_MIN = -0x40000;
        public const int LONG_SHORT_MAX = 0x3ffff;
        public const int BC_LONG_SHORT_ZERO = 0x3c;
        public const int BC_LONG_INT = 0x59;
        public const int BC_MAP = 'M';
        public const int BC_MAP_UNTYPED = 'H';
        public const int BC_NULL = 'N';
        public const int BC_OBJECT = 'O';
        public const int BC_OBJECT_DEF = 'C';
        public const int BC_OBJECT_DIRECT = 0x60;
        public const int OBJECT_DIRECT_MAX = 0x0f;
        public const int BC_REF = 0x51;
        public const int BC_STRING = 'S'; // final string
        public const int BC_STRING_CHUNK = 'R'; // non-final string
        public const int BC_STRING_DIRECT = 0x00;
        public const int STRING_DIRECT_MAX = 0x1f;
        public const int BC_STRING_SHORT = 0x30;
        public const int STRING_SHORT_MAX = 0x3ff;
        public const int BC_TRUE = 'T';
        public const int P_PACKET_CHUNK = 0x4f;
        public const int P_PACKET = 'P';
        public const int P_PACKET_DIRECT = 0x80;
        public const int PACKET_DIRECT_MAX = 0x7f;
        public const int P_PACKET_SHORT = 0x70;
        public const int PACKET_SHORT_MAX = 0xfff;
    }
}