using System;
using System.Collections.Generic;
using System.IO;

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
    /// <summary> * Output stream for Hessian 2 requests.
    /// *
    /// * <p>Since HessianOutput does not depend on any classes other than
    /// * in the JDK, it can be extracted independently into a smaller package.
    /// *
    /// * <p>HessianOutput is unbuffered, so any client needs to provide
    /// * its own buffering.
    /// *
    /// * <pre>
    /// * OutputStream os = ...; // from http connection
    /// * Hessian2Output out = new Hessian2Output(os);
    /// * String value;
    /// *
    /// * out.startCall("hello", 1); // start hello call
    /// * out.WriteString("arg1");   // write a string argument
    /// * out.completeCall();        // complete the call
    /// * </pre> </summary>
    /// 
    public class Hessian2Output : AbstractHessianOutput
    {
        public const int SIZE = 4096;

        // the output stream/
        protected internal Stream _os;

        // map of references
        private readonly IdentityIntMap _refs = new IdentityIntMap(256);

        private bool _isCloseStreamOnClose;

        // map of classes
        private readonly IdentityIntMap _classRefs = new IdentityIntMap(256);

        // map of types
        private Dictionary<string, int?> _typeRefs;

        private readonly byte[] _buffer = new byte[SIZE];
        private int _offset;

        private bool _isPacket;


        ///  
        ///   <summary> * Creates a new Hessian output stream, initialized with an
        ///   * underlying output stream.
        ///   * </summary>
        ///   * <param name="os"> the underlying output stream. </param>
        ///   
        public Hessian2Output(Stream os)
        {
            Init(os);
        }

        public override void Init(Stream os)
        {
            Reset();

            _os = os;
        }

        public virtual bool CloseStreamOnClose
        {
            set
            {
                _isCloseStreamOnClose = value;
            }
            get
            {
                return _isCloseStreamOnClose;
            }
        }


        ///  
        ///   <summary> * Writes a complete method call. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void call(String method, Object [] args) throws IOException
        public override void Call(string method, object[] args)
        {
            WriteVersion();

            int length = args != null ? args.Length : 0;

            StartCall(method, length);

            for (int i = 0; i < length; i++)
            {
                WriteObject(args[i]);
            }

            CompleteCall();

            Flush();
        }

        ///  
        ///   <summary> * Starts the method call.  Clients would use <code>startCall</code>
        ///   * instead of <code>call</code> if they wanted finer control over
        ///   * writing the arguments, or needed to write headers.
        ///   *
        ///   * <code><pre>
        ///   * C
        ///   * string # method name
        ///   * int    # arg count
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="method"> the method name to call. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void startCall(String method, int length) throws IOException
        public override void StartCall(string method, int length)
        {
            int offset = _offset;

            if (SIZE < offset + 32)
            {
                FlushBuffer();
                offset = _offset;
            }

            byte[] buffer = _buffer;

            buffer[_offset++] = (byte)'C';

            WriteString(method);
            WriteInt(length);
        }

        ///  
        ///   <summary> * Writes the call tag.  This would be followed by the
        ///   * method and the arguments
        ///   *
        ///   * <code><pre>
        ///   * C
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="method"> the method name to call. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void startCall() throws IOException
        public override void StartCall()
        {
            flushIfFull();

            _buffer[_offset++] = (byte)'C';
        }

        ///  
        ///   <summary> * Starts an envelope.
        ///   *
        ///   * <code><pre>
        ///   * E major minor
        ///   * m b16 b8 method-name
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="method"> the method name to call. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void startEnvelope(String method) throws IOException
        public virtual void StartEnvelope(string method)
        {
            int offset = _offset;

            if (SIZE < offset + 32)
            {
                FlushBuffer();
                offset = _offset;
            }

            _buffer[_offset++] = (byte)'E';

            WriteString(method);
        }

        ///  
        ///   <summary> * Completes an envelope.
        ///   *
        ///   * <p>A successful completion will have a single value:
        ///   *
        ///   * <pre>
        ///   * Z
        ///   * </pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void completeEnvelope() throws IOException
        public virtual void CompleteEnvelope()
        {
            flushIfFull();

            _buffer[_offset++] = (byte)'Z';
        }

        ///  
        ///   <summary> * Writes the method tag.
        ///   *
        ///   * <code><pre>
        ///   * string
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="method"> the method name to call. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeMethod(String method) throws IOException
        public override void WriteMethod(string method)
        {
            WriteString(method);
        }

        ///  
        ///   <summary> * Completes.
        ///   *
        ///   * <code><pre>
        ///   * z
        ///   * </pre></code> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void completeCall() throws IOException
        public override void CompleteCall()
        {
            /*
            flushIfFull();

            _buffer[_offset++] = (byte) 'Z';
            */
        }

        ///  
        ///   <summary> * Starts the reply
        ///   *
        ///   * <p>A successful completion will have a single value:
        ///   *
        ///   * <pre>
        ///   * R
        ///   * </pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void startReply() throws IOException
        public override void StartReply()
        {
            WriteVersion();

            flushIfFull();

            _buffer[_offset++] = (byte)'R';
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeVersion() throws IOException
        public virtual void WriteVersion()
        {
            flushIfFull();

            _buffer[_offset++] = (byte)'H';
            _buffer[_offset++] = 2;
            _buffer[_offset++] = 0;
        }

        ///  
        ///   <summary> * Completes reading the reply
        ///   *
        ///   * <p>A successful completion will have a single value:
        ///   *
        ///   * <pre>
        ///   * z
        ///   * </pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void completeReply() throws IOException
        public override void CompleteReply()
        {
        }

        ///  
        ///   <summary> * Starts a packet
        ///   *
        ///   * <p>A message contains several objects encapsulated by a length</p>
        ///   *
        ///   * <pre>
        ///   * p x02 x00
        ///   * </pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void startMessage() throws IOException
        public virtual void StartMessage()
        {
            flushIfFull();

            _buffer[_offset++] = (byte)'p';
            _buffer[_offset++] = 2;
            _buffer[_offset++] = 0;
        }

        ///  
        ///   <summary> * Completes reading the message
        ///   *
        ///   * <p>A successful completion will have a single value:
        ///   *
        ///   * <pre>
        ///   * z
        ///   * </pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void completeMessage() throws IOException
        public virtual void CompleteMessage()
        {
            flushIfFull();

            _buffer[_offset++] = (byte)'z';
        }

        ///  
        ///   <summary> * Writes a fault.  The fault will be written
        ///   * as a descriptive string followed by an object:
        ///   *
        ///   * <code><pre>
        ///   * F map
        ///   * </pre></code>
        ///   *
        ///   * <code><pre>
        ///   * F H
        ///   * \x04code
        ///   * \x10the fault code
        ///   *
        ///   * \x07message
        ///   * \x11the fault message
        ///   *
        ///   * \x06detail
        ///   * M\xnnjavax.ejb.FinderException
        ///   *     ...
        ///   * Z
        ///   * Z
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="code"> the fault code, a three digit </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeFault(String code, String message, Object detail) throws IOException
        public override void WriteFault(string code, string message, object detail)
        {
            flushIfFull();

            WriteVersion();

            _buffer[_offset++] = (byte)'F';
            _buffer[_offset++] = (byte)'H';

            _refs.put(new object(), _refs.size(), false);

            WriteString("code");
            WriteString(code);

            WriteString("message");
            WriteString(message);

            if (detail != null)
            {
                WriteString("detail");
                WriteObject(detail);
            }

            flushIfFull();
            _buffer[_offset++] = (byte)'Z';
        }

        ///  
        ///   <summary> * Writes any object to the output stream. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteObject(Object object) throws IOException
        public override void WriteObject(object @object)
        {
            if (@object == null)
            {
                WriteNull();
                return;
            }

            AbstractSerializer serializer = findSerializerFactory().GetObjectSerializer(@object.GetType());

            serializer.WriteObject(@object, this);
        }

        ///  
        ///   <summary> * Writes the list header to the stream.  List writers will call
        ///   * <code>writeListBegin</code> followed by the list contents and then
        ///   * call <code>writeListEnd</code>.
        ///   *
        ///   * <code><pre>
        ///   * list ::= V type value* Z
        ///   *      ::= v type int value*
        ///   * </pre></code>
        ///   * </summary>
        ///   * <returns> true for variable lists, false for fixed lists </returns>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public boolean writeListBegin(int length, String type) throws IOException
        public override bool WriteListBegin(int length, string type)
        {
            flushIfFull();

            if (length < 0)
            {
                if (type != null)
                {
                    _buffer[_offset++] = Hessian2Constants.BC_LIST_VARIABLE;
                    writeType(type);
                }
                else
                {
                    _buffer[_offset++] = Hessian2Constants.BC_LIST_VARIABLE_UNTYPED;
                }

                return true;
            }
            if (length <= Hessian2Constants.LIST_DIRECT_MAX)
            {
                if (type != null)
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_LIST_DIRECT + length);
                    writeType(type);
                }
                else
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_LIST_DIRECT_UNTYPED + length);
                }

                return false;
            }
            if (type != null)
            {
                _buffer[_offset++] = Hessian2Constants.BC_LIST_FIXED;
                writeType(type);
            }
            else
            {
                _buffer[_offset++] = Hessian2Constants.BC_LIST_FIXED_UNTYPED;
            }

            WriteInt(length);

            return false;
        }

        ///  
        ///   <summary> * Writes the tail of the list to the stream for a variable-length list. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeListEnd() throws IOException
        public override void WriteListEnd()
        {
            flushIfFull();

            _buffer[_offset++] = Hessian2Constants.BC_END;
        }

        ///  
        ///   <summary> * Writes the map header to the stream.  Map writers will call
        ///   * <code>writeMapBegin</code> followed by the map contents and then
        ///   * call <code>writeMapEnd</code>.
        ///   *
        ///   * <code><pre>
        ///   * map ::= M type (<value> <value>)* Z
        ///   *     ::= H (<value> <value>)* Z
        ///   * </pre></code> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeMapBegin(String type) throws IOException
        public override void WriteMapBegin(string type)
        {
            if (SIZE < _offset + 32)
            {
                FlushBuffer();
            }

            if (type != null)
            {
                _buffer[_offset++] = Hessian2Constants.BC_MAP;

                writeType(type);
            }
            else
            {
                _buffer[_offset++] = Hessian2Constants.BC_MAP_UNTYPED;
            }
        }

        ///  
        ///   <summary> * Writes the tail of the map to the stream. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeMapEnd() throws IOException
        public override void WriteMapEnd()
        {
            if (SIZE < _offset + 32)
            {
                FlushBuffer();
            }

            _buffer[_offset++] = Hessian2Constants.BC_END;
        }

        ///  
        ///   <summary> * Writes the object definition
        ///   *
        ///   * <code><pre>
        ///   * C &lt;string> &lt;int> &lt;string>*
        ///   * </pre></code> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public int WriteObjectBegin(String type) throws IOException
        public override int WriteObjectBegin(string type)
        {
            int newRef = _classRefs.size();
            int @ref = _classRefs.put(type, newRef, false);

            if (newRef != @ref)
            {
                if (SIZE < _offset + 32)
                {
                    FlushBuffer();
                }

                if (@ref <= Hessian2Constants.OBJECT_DIRECT_MAX)
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_OBJECT_DIRECT + @ref);
                }
                else
                {
                    _buffer[_offset++] = (byte)'O';
                    WriteInt(@ref);
                }

                return @ref;
            }
            if (SIZE < _offset + 32)
            {
                FlushBuffer();
            }

            _buffer[_offset++] = (byte)'C';

            WriteString(type);

            return -1;
        }

        ///  
        ///   <summary> * Writes the tail of the class definition to the stream. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeClassFieldLength(int len) throws IOException
        public override void WriteClassFieldLength(int len)
        {
            WriteInt(len);
        }

        ///  
        ///   <summary> * Writes the tail of the object definition to the stream. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteObjectEnd() throws IOException
        public override void WriteObjectEnd()
        {
        }

        ///  
        ///   <summary> * <code><pre>
        ///   * type ::= string
        ///   *      ::= int
        ///   * </code></pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void writeType(String type) throws IOException
        private void writeType(string type)
        {
            flushIfFull();

            int len = type.Length;
            if (len == 0)
            {
                throw new ArgumentException("empty type is not allowed");
            }

            if (_typeRefs == null)
            {
                _typeRefs = new Dictionary<string, int?>();
            }

            int? typeRefV = _typeRefs[type];

            if (typeRefV != null)
            {
                int typeRef = (int)typeRefV;

                WriteInt(typeRef);
            }
            else
            {
                _typeRefs.Add(type, Convert.ToInt32(_typeRefs.Count));

                WriteString(type);
            }
        }

        ///  
        ///   <summary> * Writes a boolean value to the stream.  The boolean will be written
        ///   * with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * T
        ///   * F
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the boolean value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeBoolean(boolean value) throws IOException
        public override void WriteBoolean(bool value)
        {
            if (SIZE < _offset + 16)
            {
                FlushBuffer();
            }

            if (value)
            {
                _buffer[_offset++] = (byte)'T';
            }
            else
            {
                _buffer[_offset++] = (byte)'F';
            }
        }

        ///  
        ///   <summary> * Writes an integer value to the stream.  The integer will be written
        ///   * with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * I b32 b24 b16 b8
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the integer value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteInt(int value) throws IOException
        public override void WriteInt(int value)
        {
            int offset = _offset;
            byte[] buffer = _buffer;

            if (SIZE <= offset + 16)
            {
                FlushBuffer();
                offset = _offset;
            }

            if (Hessian2Constants.INT_DIRECT_MIN <= value && value <= Hessian2Constants.INT_DIRECT_MAX)
            {
                buffer[offset++] = (byte)(value + Hessian2Constants.BC_INT_ZERO);
            }
            else if (Hessian2Constants.INT_BYTE_MIN <= value && value <= Hessian2Constants.INT_BYTE_MAX)
            {
                buffer[offset++] = (byte)(Hessian2Constants.BC_INT_BYTE_ZERO + (value >> 8));
                buffer[offset++] = (byte)(value);
            }
            else if (Hessian2Constants.INT_SHORT_MIN <= value && value <= Hessian2Constants.INT_SHORT_MAX)
            {
                buffer[offset++] = (byte)(Hessian2Constants.BC_INT_SHORT_ZERO + (value >> 16));
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value);
            }
            else
            {
                buffer[offset++] = (byte)('I');
                buffer[offset++] = (byte)(value >> 24);
                buffer[offset++] = (byte)(value >> 16);
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value);
            }

            _offset = offset;
        }

        ///  
        ///   <summary> * Writes a long value to the stream.  The long will be written
        ///   * with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * L b64 b56 b48 b40 b32 b24 b16 b8
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the long value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeLong(long value) throws IOException
        public override void WriteLong(long value)
        {
            int offset = _offset;
            byte[] buffer = _buffer;

            if (SIZE <= offset + 16)
            {
                FlushBuffer();
                offset = _offset;
            }

            if (Hessian2Constants.LONG_DIRECT_MIN <= value && value <= Hessian2Constants.LONG_DIRECT_MAX)
            {
                buffer[offset++] = (byte)(value + Hessian2Constants.BC_LONG_ZERO);
            }
            else if (Hessian2Constants.LONG_BYTE_MIN <= value && value <= Hessian2Constants.LONG_BYTE_MAX)
            {
                buffer[offset++] = (byte)(Hessian2Constants.BC_LONG_BYTE_ZERO + (value >> 8));
                buffer[offset++] = (byte)(value);
            }
            else if (Hessian2Constants.LONG_SHORT_MIN <= value && value <= Hessian2Constants.LONG_SHORT_MAX)
            {
                buffer[offset++] = (byte)(Hessian2Constants.BC_LONG_SHORT_ZERO + (value >> 16));
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value);
            }
            else if (-0x80000000L <= value && value <= 0x7fffffffL)
            {
                buffer[offset + 0] = Hessian2Constants.BC_LONG_INT;
                buffer[offset + 1] = (byte)(value >> 24);
                buffer[offset + 2] = (byte)(value >> 16);
                buffer[offset + 3] = (byte)(value >> 8);
                buffer[offset + 4] = (byte)(value);

                offset += 5;
            }
            else
            {
                buffer[offset + 0] = (byte)'L';
                buffer[offset + 1] = (byte)(value >> 56);
                buffer[offset + 2] = (byte)(value >> 48);
                buffer[offset + 3] = (byte)(value >> 40);
                buffer[offset + 4] = (byte)(value >> 32);
                buffer[offset + 5] = (byte)(value >> 24);
                buffer[offset + 6] = (byte)(value >> 16);
                buffer[offset + 7] = (byte)(value >> 8);
                buffer[offset + 8] = (byte)(value);

                offset += 9;
            }

            _offset = offset;
        }

        ///  
        ///   <summary> * Writes a double value to the stream.  The double will be written
        ///   * with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * D b64 b56 b48 b40 b32 b24 b16 b8
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the double value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteDouble(double value) throws IOException
        public override void WriteDouble(double value)
        {
            throw new NotImplementedException("WriteDouble");
            int offset = _offset;
            byte[] buffer = _buffer;

            if (SIZE <= offset + 16)
            {
                FlushBuffer();
                offset = _offset;
            }

            int intValue = (int)value;

            if (intValue == value)
            {
                if (intValue == 0)
                {
                    buffer[offset++] = (byte)Hessian2Constants.BC_DOUBLE_ZERO;

                    _offset = offset;

                    return;
                }
                else if (intValue == 1)
                {
                    buffer[offset++] = (byte)Hessian2Constants.BC_DOUBLE_ONE;

                    _offset = offset;

                    return;
                }
                else if (-0x80 <= intValue && intValue < 0x80)
                {
                    buffer[offset++] = (byte)Hessian2Constants.BC_DOUBLE_BYTE;
                    buffer[offset++] = (byte)intValue;

                    _offset = offset;

                    return;
                }
                else if (-0x8000 <= intValue && intValue < 0x8000)
                {
                    buffer[offset + 0] = (byte)Hessian2Constants.BC_DOUBLE_SHORT;
                    buffer[offset + 1] = (byte)(intValue >> 8);
                    buffer[offset + 2] = (byte)intValue;

                    _offset = offset + 3;

                    return;
                }
            }

            int mills = (int)(value * 1000);

            if (0.001 * mills == value)
            {
                buffer[offset + 0] = (byte)(Hessian2Constants.BC_DOUBLE_MILL);
                buffer[offset + 1] = (byte)(mills >> 24);
                buffer[offset + 2] = (byte)(mills >> 16);
                buffer[offset + 3] = (byte)(mills >> 8);
                buffer[offset + 4] = (byte)(mills);

                _offset = offset + 5;

                return;
            }

            long bits = 0;
            //TODO bits = double.doubleToLongBits(value);

            buffer[offset + 0] = (byte)'D';
            buffer[offset + 1] = (byte)(bits >> 56);
            buffer[offset + 2] = (byte)(bits >> 48);
            buffer[offset + 3] = (byte)(bits >> 40);
            buffer[offset + 4] = (byte)(bits >> 32);
            buffer[offset + 5] = (byte)(bits >> 24);
            buffer[offset + 6] = (byte)(bits >> 16);
            buffer[offset + 7] = (byte)(bits >> 8);
            buffer[offset + 8] = (byte)(bits);

            _offset = offset + 9;
        }

        ///  
        ///   <summary> * Writes a date to the stream.
        ///   *
        ///   * <code><pre>
        ///   * date ::= d   b7 b6 b5 b4 b3 b2 b1 b0
        ///   *      ::= x65 b3 b2 b1 b0
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="time"> the date in milliseconds from the epoch in UTC </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeUTCDate(long time) throws IOException
        public override void WriteUTCDate(long time)
        {
            if (SIZE < _offset + 32)
            {
                FlushBuffer();
            }

            int offset = _offset;
            byte[] buffer = _buffer;

            if (time % 60000L == 0)
            {
                // compact date ::= x65 b3 b2 b1 b0

                long minutes = time / 60000L;

                if ((minutes >> 31) == 0 || (minutes >> 31) == -1)
                {
                    buffer[offset++] = Hessian2Constants.BC_DATE_MINUTE;
                    buffer[offset++] = ((byte)(minutes >> 24));
                    buffer[offset++] = ((byte)(minutes >> 16));
                    buffer[offset++] = ((byte)(minutes >> 8));
                    buffer[offset++] = ((byte)(minutes >> 0));

                    _offset = offset;
                    return;
                }
            }

            buffer[offset++] = Hessian2Constants.BC_DATE;
            buffer[offset++] = ((byte)(time >> 56));
            buffer[offset++] = ((byte)(time >> 48));
            buffer[offset++] = ((byte)(time >> 40));
            buffer[offset++] = ((byte)(time >> 32));
            buffer[offset++] = ((byte)(time >> 24));
            buffer[offset++] = ((byte)(time >> 16));
            buffer[offset++] = ((byte)(time >> 8));
            buffer[offset++] = ((byte)(time));

            _offset = offset;
        }

        ///  
        ///   <summary> * Writes a null value to the stream.
        ///   * The null will be written with the following syntax
        ///   *
        ///   * <code><pre>
        ///   * N
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the string value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeNull() throws IOException
        public override void WriteNull()
        {
            int offset = _offset;
            byte[] buffer = _buffer;

            if (SIZE <= offset + 16)
            {
                FlushBuffer();
                offset = _offset;
            }

            buffer[offset++] = (byte)'N';

            _offset = offset;
        }

        ///  
        ///   <summary> * Writes a string value to the stream using UTF-8 encoding.
        ///   * The string will be written with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * S b16 b8 string-value
        ///   * </pre></code>
        ///   *
        ///   * If the value is null, it will be written as
        ///   *
        ///   * <code><pre>
        ///   * N
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the string value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteString(String value) throws IOException
        public override void WriteString(string value)
        {
            int offset = _offset;
            byte[] buffer = _buffer;

            if (SIZE <= offset + 16)
            {
                FlushBuffer();
                offset = _offset;
            }

            if (value == null)
            {
                buffer[offset++] = (byte)'N';

                _offset = offset;
            }
            else
            {
                int length = value.Length;
                int strOffset = 0;

                while (length > 0x8000)
                {
                    int sublen = 0x8000;

                    offset = _offset;

                    if (SIZE <= offset + 16)
                    {
                        FlushBuffer();
                        offset = _offset;
                    }

                    // chunk can't end in high surrogate
                    char tail = value[strOffset + sublen - 1];

                    if (0xd800 <= tail && tail <= 0xdbff)
                    {
                        sublen--;
                    }

                    buffer[offset + 0] = Hessian2Constants.BC_STRING_CHUNK;
                    buffer[offset + 1] = (byte)(sublen >> 8);
                    buffer[offset + 2] = (byte)(sublen);

                    _offset = offset + 3;

                    printString(value, strOffset, sublen);

                    length -= sublen;
                    strOffset += sublen;
                }

                offset = _offset;

                if (SIZE <= offset + 16)
                {
                    FlushBuffer();
                    offset = _offset;
                }

                if (length <= Hessian2Constants.STRING_DIRECT_MAX)
                {
                    buffer[offset++] = (byte)(Hessian2Constants.BC_STRING_DIRECT + length);
                }
                else if (length <= Hessian2Constants.STRING_SHORT_MAX)
                {
                    buffer[offset++] = (byte)(Hessian2Constants.BC_STRING_SHORT + (length >> 8));
                    buffer[offset++] = (byte)(length);
                }
                else
                {
                    buffer[offset++] = (byte)('S');
                    buffer[offset++] = (byte)(length >> 8);
                    buffer[offset++] = (byte)(length);
                }

                _offset = offset;

                printString(value, strOffset, length);
            }
        }

        private void printString(string value, int offset, int sublen)
        {
            if (value == null) printString(null);
            else printString(value.ToCharArray(), offset, sublen);
        }

        ///  
        ///   <summary> * Writes a string value to the stream using UTF-8 encoding.
        ///   * The string will be written with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * S b16 b8 string-value
        ///   * </pre></code>
        ///   *
        ///   * If the value is null, it will be written as
        ///   *
        ///   * <code><pre>
        ///   * N
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the string value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteString(char [] buffer, int offset, int length) throws IOException
        public override void WriteString(char[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                if (SIZE < _offset + 16)
                {
                    FlushBuffer();
                }

                _buffer[_offset++] = (byte)('N');
            }
            else
            {
                while (length > 0x8000)
                {
                    int sublen = 0x8000;

                    if (SIZE < _offset + 16)
                    {
                        FlushBuffer();
                    }

                    // chunk can't end in high surrogate
                    char tail = buffer[offset + sublen - 1];

                    if (0xd800 <= tail && tail <= 0xdbff)
                    {
                        sublen--;
                    }

                    _buffer[_offset++] = Hessian2Constants.BC_STRING_CHUNK;
                    _buffer[_offset++] = (byte)(sublen >> 8);
                    _buffer[_offset++] = (byte)(sublen);

                    printString(buffer, offset, sublen);

                    length -= sublen;
                    offset += sublen;
                }

                if (SIZE < _offset + 16)
                {
                    FlushBuffer();
                }

                if (length <= Hessian2Constants.STRING_DIRECT_MAX)
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_STRING_DIRECT + length);
                }
                else if (length <= Hessian2Constants.STRING_SHORT_MAX)
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_STRING_SHORT + (length >> 8));
                    _buffer[_offset++] = (byte)length;
                }
                else
                {
                    _buffer[_offset++] = (byte)('S');
                    _buffer[_offset++] = (byte)(length >> 8);
                    _buffer[_offset++] = (byte)(length);
                }

                printString(buffer, offset, length);
            }
        }

        ///  
        ///   <summary> * Writes a byte array to the stream.
        ///   * The array will be written with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * B b16 b18 bytes
        ///   * </pre></code>
        ///   *
        ///   * If the value is null, it will be written as
        ///   *
        ///   * <code><pre>
        ///   * N
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the string value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteBytes(byte [] buffer) throws IOException
        public override void WriteBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                if (SIZE < _offset + 16)
                {
                    FlushBuffer();
                }

                _buffer[_offset++] = (byte)'N';
            }
            else
            {
                WriteBytes(buffer, 0, buffer.Length);
            }
        }

        ///  
        ///   <summary> * Writes a byte array to the stream.
        ///   * The array will be written with the following syntax:
        ///   *
        ///   * <code><pre>
        ///   * B b16 b18 bytes
        ///   * </pre></code>
        ///   *
        ///   * If the value is null, it will be written as
        ///   *
        ///   * <code><pre>
        ///   * N
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the string value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteBytes(byte [] buffer, int offset, int length) throws IOException
        public override void WriteBytes(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                if (SIZE < _offset + 16)
                {
                    FlushBuffer();
                }

                _buffer[_offset++] = (byte)'N';
            }
            else
            {
                while (SIZE - _offset - 3 < length)
                {
                    int sublen = SIZE - _offset - 3;

                    if (sublen < 16)
                    {
                        FlushBuffer();

                        sublen = SIZE - _offset - 3;

                        if (length < sublen)
                        {
                            sublen = length;
                        }
                    }

                    _buffer[_offset++] = Hessian2Constants.BC_BINARY_CHUNK;
                    _buffer[_offset++] = (byte)(sublen >> 8);
                    _buffer[_offset++] = (byte)sublen;

                    Array.Copy(buffer, offset, _buffer, _offset, sublen);
                    _offset += sublen;

                    length -= sublen;
                    offset += sublen;

                    FlushBuffer();
                }

                if (SIZE < _offset + 16)
                {
                    FlushBuffer();
                }

                if (length <= Hessian2Constants.BINARY_DIRECT_MAX)
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_BINARY_DIRECT + length);
                }
                else if (length <= Hessian2Constants.BINARY_SHORT_MAX)
                {
                    _buffer[_offset++] = (byte)(Hessian2Constants.BC_BINARY_SHORT + (length >> 8));
                    _buffer[_offset++] = (byte)(length);
                }
                else
                {
                    _buffer[_offset++] = (byte)'B';
                    _buffer[_offset++] = (byte)(length >> 8);
                    _buffer[_offset++] = (byte)(length);
                }

                Array.Copy(buffer, offset, _buffer, _offset, length);

                _offset += length;
            }
        }

        ///  
        ///   <summary> * Writes a byte buffer to the stream.
        ///   *
        ///   * <code><pre>
        ///   * </pre></code> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteByteBufferStart() throws IOException
        public override void WriteByteBufferStart()
        {
        }

        ///  
        ///   <summary> * Writes a byte buffer to the stream.
        ///   *
        ///   * <code><pre>
        ///   * b b16 b18 bytes
        ///   * </pre></code> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteByteBufferPart(byte [] buffer, int offset, int length) throws IOException
        public override void WriteByteBufferPart(byte[] buffer, int offset, int length)
        {
            while (length > 0)
            {
                flushIfFull();

                int sublen = _buffer.Length - _offset;

                if (length < sublen)
                {
                    sublen = length;
                }

                _buffer[_offset++] = Hessian2Constants.BC_BINARY_CHUNK;
                _buffer[_offset++] = (byte)(sublen >> 8);
                _buffer[_offset++] = (byte)sublen;

                Array.Copy(buffer, offset, _buffer, _offset, sublen);

                _offset += sublen;
                length -= sublen;
                offset += sublen;
            }
        }

        ///  
        ///   <summary> * Writes a byte buffer to the stream.
        ///   *
        ///   * <code><pre>
        ///   * b b16 b18 bytes
        ///   * </pre></code> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteByteBufferEnd(byte [] buffer, int offset, int length) throws IOException
        public override void WriteByteBufferEnd(byte[] buffer, int offset, int length)
        {
            WriteBytes(buffer, offset, length);
        }

        ///  
        ///   <summary> * Returns an output stream to write binary data. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public OutputStream getBytesOutputStream() throws IOException
        public virtual Stream GetBytesOutputStream()
        {
            return new BytesOutputStream(this);
        }

        ///  
        ///   <summary> * Writes a full output stream. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void WriteByteStream(InputStream is) throws IOException
        public override void WriteByteStream(Stream @is)
        {
            while (true)
            {
                int len = SIZE - _offset - 3;

                if (len < 16)
                {
                    FlushBuffer();
                    len = SIZE - _offset - 3;
                }

                len = @is.Read(_buffer, _offset + 3, len);

                if (len <= 0)
                {
                    _buffer[_offset++] = Hessian2Constants.BC_BINARY_DIRECT;
                    return;
                }

                _buffer[_offset + 0] = Hessian2Constants.BC_BINARY_CHUNK;
                _buffer[_offset + 1] = (byte)(len >> 8);
                _buffer[_offset + 2] = (byte)(len);

                _offset += len + 3;
            }
        }

        ///  
        ///   <summary> * Writes a reference.
        ///   *
        ///   * <code><pre>
        ///   * x51 &lt;int>
        ///   * </pre></code>
        ///   * </summary>
        ///   * <param name="value"> the integer value to write. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected void writeRef(int value) throws IOException
        public override void WriteRef(int value)
        {
            if (SIZE < _offset + 16)
            {
                FlushBuffer();
            }

            _buffer[_offset++] = Hessian2Constants.BC_REF;

            WriteInt(value);
        }

        ///  
        ///   <summary> * If the object has already been written, just write its ref.
        ///   * </summary>
        ///   * <returns> true if we're writing a ref. </returns>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public boolean addRef(Object object) throws IOException
        public override bool AddRef(object @object)
        {
            int newRef = _refs.size();

            int @ref = _refs.put(@object, newRef, false);

            if (@ref != newRef)
            {
                WriteRef(@ref);

                return true;
            }
            return false;
        }

        public override int GetRef(object obj)
        {
            return _refs.get(obj);
        }

        ///  
        ///   <summary> * Removes a reference. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public boolean removeRef(Object obj) throws IOException
        public override bool RemoveRef(object obj)
        {
            if (_refs != null)
            {
                _refs.remove(obj);

                return true;
            }
            return false;
        }

        ///  
        ///   <summary> * Replaces a reference from one object to another. </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public boolean replaceRef(Object oldRef, Object newRef) throws IOException
        public override bool ReplaceRef(object oldRef, object newRef)
        {
            int value = _refs.get(oldRef);

            if (value >= 0)
            {
                _refs.put(newRef, value, true);

                _refs.remove(oldRef);

                return true;
            }
            return false;
        }

        ///  
        ///   <summary> * Starts the streaming message
        ///   *
        ///   * <p>A streaming message starts with 'P'</p>
        ///   *
        ///   * <pre>
        ///   * P x02 x00
        ///   * </pre> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void writeStreamingObject(Object obj) throws IOException
        public virtual void writeStreamingObject(object obj)
        {
            StartPacket();

            WriteObject(obj);

            EndPacket();
        }

        ///  
        ///   <summary> * Starts a streaming packet
        ///   *
        ///   * <p>A streaming contains a set of chunks, ending with a zero chunk.
        ///   * Each chunk is a length followed by data where the length is
        ///   * encoded by (b1xxxxxxxx)* b0xxxxxxxx</p> </summary>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void startPacket() throws IOException
        public virtual void StartPacket()
        {
            if (_refs != null)
            {
                _refs.clear();
            }

            FlushBuffer();

            _isPacket = true;
            _offset = 3;
            _buffer[0] = 0x55;
            _buffer[1] = 0x55;
            _buffer[2] = 0x55;
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void endPacket() throws IOException
        public virtual void EndPacket()
        {
            int offset = _offset;

            Stream os = _os;

            if (os == null)
            {
                _offset = 0;
                return;
            }

            int len = offset - 3;

            _buffer[0] = 0x80;
            _buffer[1] = (byte)(0x80 + ((len >> 7) & 0x7f));
            _buffer[2] = (byte)(len & 0x7f);

            // end chunk
            _buffer[offset++] = 0x80;
            _buffer[offset++] = 0x00;

            _isPacket = false;
            _offset = 0;


            if (len == 0)
            {
                os.Write(_buffer, 1, 2);
            }
            else if (len < 0x80)
            {
                os.Write(_buffer, 1, offset - 1);
            }
            else
            {
                os.Write(_buffer, 0, offset);
            }
        }

        ///  
        ///   <summary> * Prints a string to the stream, encoded as UTF-8 with preceeding length
        ///   * </summary>
        ///   * <param name="v"> the string to print. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void printLenString(String v) throws IOException
        public virtual void printLenString(string v)
        {
            if (SIZE < _offset + 16)
            {
                FlushBuffer();
            }

            if (v == null)
            {
                _buffer[_offset++] = 0;
                _buffer[_offset++] = 0;
            }
            else
            {
                int len = v.Length;
                _buffer[_offset++] = (byte)(len >> 8);
                _buffer[_offset++] = (byte)(len);

                printString(v, 0, len);
            }
        }

        ///  
        ///   <summary> * Prints a string to the stream, encoded as UTF-8
        ///   * </summary>
        ///   * <param name="v"> the string to print. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void printString(String v) throws IOException
        public virtual void printString(string v)
        {
            printString(v, 0, v.Length);
        }

        ///  
        ///   <summary> * Prints a string to the stream, encoded as UTF-8
        ///   * </summary>
        ///   * <param name="v"> the string to print. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void printString(String v, int strOffset, int length) throws IOException
        public virtual void PrintString(string v, int strOffset, int length)
        {
            int offset = _offset;
            byte[] buffer = _buffer;

            for (int i = 0; i < length; i++)
            {
                if (SIZE <= offset + 16)
                {
                    _offset = offset;
                    FlushBuffer();
                    offset = _offset;
                }

                char ch = v[i + strOffset];

                if (ch < 0x80)
                {
                    buffer[offset++] = (byte)(ch);
                }
                else if (ch < 0x800)
                {
                    buffer[offset++] = (byte)(0xc0 + ((ch >> 6) & 0x1f));
                    buffer[offset++] = (byte)(0x80 + (ch & 0x3f));
                }
                else
                {
                    buffer[offset++] = (byte)(0xe0 + ((ch >> 12) & 0xf));
                    buffer[offset++] = (byte)(0x80 + ((ch >> 6) & 0x3f));
                    buffer[offset++] = (byte)(0x80 + (ch & 0x3f));
                }
            }

            _offset = offset;
        }

        ///  
        ///   <summary> * Prints a string to the stream, encoded as UTF-8
        ///   * </summary>
        ///   * <param name="v"> the string to print. </param>
        ///   
        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void printString(char [] v, int strOffset, int length) throws IOException
        public virtual void printString(char[] v, int strOffset, int length)
        {
            int offset = _offset;
            byte[] buffer = _buffer;

            for (int i = 0; i < length; i++)
            {
                if (SIZE <= offset + 16)
                {
                    _offset = offset;
                    FlushBuffer();
                    offset = _offset;
                }

                char ch = v[i + strOffset];

                if (ch < 0x80)
                {
                    buffer[offset++] = (byte)(ch);
                }
                else if (ch < 0x800)
                {
                    buffer[offset++] = (byte)(0xc0 + ((ch >> 6) & 0x1f));
                    buffer[offset++] = (byte)(0x80 + (ch & 0x3f));
                }
                else
                {
                    buffer[offset++] = (byte)(0xe0 + ((ch >> 12) & 0xf));
                    buffer[offset++] = (byte)(0x80 + ((ch >> 6) & 0x3f));
                    buffer[offset++] = (byte)(0x80 + (ch & 0x3f));
                }
            }

            _offset = offset;
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private final void flushIfFull() throws IOException
        private void flushIfFull()
        {
            int offset = _offset;

            if (SIZE < offset + 32)
            {
                FlushBuffer();
            }
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final void flush() throws IOException
        public sealed override void Flush()
        {
            FlushBuffer();

            if (_os != null)
            {
                _os.Flush();
            }
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public final void FlushBuffer() throws IOException
        public void FlushBuffer()
        {
            int offset = _offset;

            Stream os = _os;

            if (!_isPacket && offset > 0)
            {
                _offset = 0;
                if (os != null)
                {
                    os.Write(_buffer, 0, offset);
                }
            }
            else if (_isPacket && offset > 3)
            {
                int len = offset - 3;
                _buffer[0] = 0x80;
                _buffer[1] = (byte)(0x80 + ((len >> 7) & 0x7f));
                _buffer[2] = (byte)(len & 0x7f);
                _offset = 3;

                if (os != null)
                {
                    os.Write(_buffer, 0, offset);
                }

                _buffer[0] = 0x56;
                _buffer[1] = 0x56;
                _buffer[2] = 0x56;

            }
        }

        //JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void close() throws IOException
        public override void Close()
        {
            // hessian/3a8c
            Flush();

            Stream os = _os;
            _os = null;

            if (os != null)
            {
                if (_isCloseStreamOnClose)
                {
                    os.Close();
                }
            }
        }

        public virtual void free()
        {
            Reset();

            _os = null;
            _isCloseStreamOnClose = false;
        }

        ///  
        ///   <summary> * Resets the references for streaming. </summary>
        ///   
        public override void ResetReferences()
        {
            if (_refs != null)
            {
                _refs.clear();
            }
        }

        ///  
        ///   <summary> * Resets all counters and references </summary>
        ///   
        public virtual void Reset()
        {
            if (_refs != null)
            {
                _refs.clear();
            }

            _classRefs.clear();
            _typeRefs = null;
            _offset = 0;
            _isPacket = false;
        }

        internal class BytesOutputStream : Stream
        {
            private readonly Hessian2Output output;
            private int _startOffset;

            internal BytesOutputStream(Hessian2Output output)
            {
                this.output = output;
                if (SIZE < output._offset + 16)
                {
                    output.FlushBuffer();
                }

                _startOffset = output._offset;
                output._offset += 3; // skip 'b' xNN xNN
            }

            public override void WriteByte(byte ch)
            {
                if (SIZE <= output._offset)
                {
                    int length = (output._offset - _startOffset) - 3;

                    output._buffer[_startOffset] = Hessian2Constants.BC_BINARY_CHUNK;
                    output._buffer[_startOffset + 1] = (byte)(length >> 8);
                    output._buffer[_startOffset + 2] = (byte)(length);

                    output.FlushBuffer();

                    _startOffset = output._offset;
                    output._offset += 3;
                }

                output._buffer[output._offset++] = ch;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int length)
            {
                while (length > 0)
                {
                    int sublen = SIZE - output._offset;

                    if (length < sublen)
                    {
                        sublen = length;
                    }

                    if (sublen > 0)
                    {
                        Array.Copy(buffer, offset, output._buffer, output._offset, sublen);
                        output._offset += sublen;
                    }

                    length -= sublen;
                    offset += sublen;

                    if (SIZE <= output._offset)
                    {
                        int chunkLength = (output._offset - _startOffset) - 3;

                        output._buffer[_startOffset] = Hessian2Constants.BC_BINARY_CHUNK;
                        output._buffer[_startOffset + 1] = (byte)(chunkLength >> 8);
                        output._buffer[_startOffset + 2] = (byte)(chunkLength);

                        output.FlushBuffer();

                        _startOffset = output._offset;
                        output._offset += 3;
                    }
                }
            }

            public override bool CanRead
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanSeek
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanWrite
            {
                get { throw new NotImplementedException(); }
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            protected override void Dispose(bool disposing)
            {
                int startOffset = _startOffset;
                _startOffset = -1;

                if (startOffset < 0)
                {
                    return;
                }

                int length = (output._offset - startOffset) - 3;

                output._buffer[startOffset] = (byte)'B';
                output._buffer[startOffset + 1] = (byte)(length >> 8);
                output._buffer[startOffset + 2] = (byte)(length);

                output.FlushBuffer();
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }
        }
    }

}