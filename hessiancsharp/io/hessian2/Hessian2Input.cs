using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using java.io;
using java.lang;
using Byte=System.Byte;
using ByteArrayOutputStream=java.io.ByteArrayOutputStream;
using EOFException=java.io.EOFException;
using Exception=System.Exception;
using InputStream=java.io.InputStream;
using IOException=System.IO.IOException;

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
 * 4. The names "Hessian", "Resin", and "Caucho" must not be used to
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
/// <summary> * Input stream for Hessian requests.
/// *
/// * <p>HessianInput is unbuffered, so any client needs to provide
/// * its own buffering.
/// *
/// * <pre>
/// * InputStream is = ...; // from http connection
/// * HessianInput in = new HessianInput(is);
/// * String value;
/// *
/// * in.startReply();         // Read reply header
/// * value = in.ReadString(); // Read string value
/// * in.completeReply();      // Read reply footer
/// * </pre> </summary>
/// 
	public class Hessian2Input : AbstractHessianInput
	{
	  //private static readonly Logger log = Logger.getLogger(typeof(Hessian2Input).Name);

	  //private static Field _detailMessageField;

	  private const int SIZE = 256;
	  private const int GAP = 16;

  // standard, unmodified factory for deserializing objects
	  protected internal CSerializerFactory _defaultSerializerFactory;
  // factory for deserializing objects in the input stream
	  protected internal CSerializerFactory _serializerFactory;

	  private static bool _isCloseStreamOnClose;

	  protected internal ArrayList _refs = new ArrayList();
	  private readonly List<ObjectDefinition> _classDefs = new List<ObjectDefinition>();
	  protected internal ArrayList _types = new ArrayList();

  // the underlying input stream
	  private Stream _is;
	  private readonly byte[] _buffer = new byte[SIZE];

  // a peek character
	  private int _offset;
	  private int _length;

  // the method for a call
	  private string _method;
	  private Exception _replyFault;

	  private StringBuilder _sbuf = new StringBuilder();

  // true if this is the last chunk
	  private bool _isLastChunk;
  // the chunk length
	  private int _chunkLength;

///  
///   <summary> * Creates a new Hessian input stream, initialized with an
///   * underlying input stream.
///   * </summary>
///   * <param name="is"> the underlying input stream. </param>
///   
	  public Hessian2Input(Stream @is)
	  {
		_is = @is;
	  }

///  
///   <summary> * Sets the serializer factory. </summary>
///   
	  public override CSerializerFactory SerializerFactory
	  {
		  set
		  {
			_serializerFactory = value;
		  }
		  get
		  {
		// the default serializer factory cannot be modified by external
		// callers
			if (_serializerFactory == _defaultSerializerFactory)
			{
			  _serializerFactory = new CSerializerFactory();
			}
    
			return _serializerFactory;
		  }
	  }

///  
///   <summary> * Gets the serializer factory. </summary>
///   

///  
///   <summary> * Gets the serializer factory. </summary>
///   
	  protected internal CSerializerFactory findSerializerFactory()
	  {
		CSerializerFactory factory = _serializerFactory;

		if (factory == null)
		{
		    factory = CSerializerFactory.newInstance();
		    _defaultSerializerFactory = factory;
		    _serializerFactory = factory;
		}

		return factory;
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
///   <summary> * Returns the calls method </summary>
///   
	  public override string Method
	  {
		  get
		  {
			return _method;
		  }
	  }

///  
///   <summary> * Returns any reply fault. </summary>
///   
	  public virtual Exception ReplyFault
	  {
		  get
		  {
			return _replyFault;
		  }
	  }

///  
///   <summary> * Starts reading the call
///   *
///   * <pre>
///   * c major minor
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readCall() throws IOException
	  public override int ReadCall()
	  {
		int tag = Read();

		if (tag != 'C')
		{
		  throw error("expected hessian call ('C') at " + codeName(tag));
		}

		return 0;
	  }

///  
///   <summary> * Starts reading the envelope
///   *
///   * <pre>
///   * E major minor
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readEnvelope() throws IOException
	  public virtual int readEnvelope()
	  {
		int tag = Read();
		int version = 0;

		if (tag == 'H')
		{
		  int major = Read();
		  int minor = Read();

		  version = (major << 16) + minor;

		  tag = Read();
		}

		if (tag != 'E')
		{
		  throw error("expected hessian Envelope ('E') at " + codeName(tag));
		}

		return version;
	  }

///  
///   <summary> * Completes reading the envelope
///   *
///   * <p>A successful completion will have a single value:
///   *
///   * <pre>
///   * Z
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void completeEnvelope() throws IOException
	  public virtual void completeEnvelope()
	  {
		int tag = Read();

		if (tag != 'Z')
		{
		  error("expected end of envelope at " + codeName(tag));
		}
	  }

///  
///   <summary> * Starts reading the call
///   *
///   * <p>A successful completion will have a single value:
///   *
///   * <pre>
///   * string
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public String readMethod() throws IOException
	  public override string ReadMethod()
	  {
		_method = ReadString();

		return _method;
	  }

///  
///   <summary> * Returns the number of method arguments
///   *
///   * <pre>
///   * int
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readMethodArgLength() throws IOException
	  public override int ReadMethodArgLength()
	  {
		return ReadInt();
	  }

///  
///   <summary> * Starts reading the call, including the headers.
///   *
///   * <p>The call expects the following protocol data
///   *
///   * <pre>
///   * c major minor
///   * m b16 b8 method
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void startCall() throws IOException
	  public override void StartCall()
	  {
		ReadCall();
		ReadMethod();
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object [] readArguments() throws IOException
	  public virtual object [] ReadArguments()
	  {
		int len = ReadInt();

		object[] args = new object[len];

		for (int i = 0; i < len; i++)
		{
		  args[i] = ReadObject();
		}

		return args;
	  }

///  
///   <summary> * Completes reading the call
///   *
///   * <p>A successful completion will have a single value:
///   *
///   * <pre>
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void completeCall() throws IOException
	  public override void CompleteCall()
	  {
	  }

///  
///   <summary> * Reads a reply as an object.
///   * If the reply has a fault, throws the exception. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object readReply(Class expectedClass) throws Throwable
	  public override object ReadReply(Type expectedClass)
	  {
		int tag = Read();

		if (tag == 'R')
		{
		  return ReadObject(expectedClass);
		}
    if (tag == 'F')
    {
        Hashtable map = (Hashtable) ReadObject(typeof(Hashtable));

        throw prepareFault(map);
    }
    StringBuilder sb = new StringBuilder();
    sb.Append((char) tag);

    try
    {
        int ch;

        while ((ch = Read()) >= 0)
        {
            sb.Append((char) ch);
        }
    }
    catch (IOException e)
    {
        //log.log(Level.FINE, e.ToString(), e);
    }

    throw error("expected hessian reply at " + codeName(tag) + "\n" + sb);
	  }

///  
///   <summary> * Starts reading the reply
///   *
///   * <p>A successful completion will have a single value:
///   *
///   * <pre>
///   * r
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void startReply() throws Throwable
	  public override void StartReply()
	  {
	// XXX: for variable length (?)

		ReadReply(typeof(object));
	  }

///  
///   <summary> * Prepares the fault. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private Throwable prepareFault(HashMap fault) throws IOException
	  private Exception prepareFault(Hashtable fault)
	  {
		object detail = fault["detail"];
		string message = (string) fault["message"];

		if (detail is Exception)
		{
		  _replyFault = (Exception) detail;

		  /*if (message != null && _detailMessageField != null)
		  {
		try
		{
		  _detailMessageField.set(_replyFault, message);
		}
		catch (Exception e)
		{
		}
		  }*/

		  return _replyFault;
		}

    string code = (string) fault["code"];

    _replyFault = new HessianServiceException(message, code, detail);

    return _replyFault;
	  }

///  
///   <summary> * Completes reading the call
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
///   <summary> * Completes reading the call
///   *
///   * <p>A successful completion will have a single value:
///   *
///   * <pre>
///   * z
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void completeValueReply() throws IOException
	  public virtual void completeValueReply()
	  {
		int tag = Read();

		if (tag != 'Z')
		{
		  error("expected end of reply at " + codeName(tag));
		}
	  }

///  
///   <summary> * Reads a header, returning null if there are no headers.
///   *
///   * <pre>
///   * H b16 b8 value
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public String readHeader() throws IOException
	  public override string ReadHeader()
	  {
		return null;
	  }

///  
///   <summary> * Starts reading a packet
///   *
///   * <pre>
///   * p major minor
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int startMessage() throws IOException
	  public virtual int StartMessage()
	  {
		int tag = Read();

		if (tag == 'p')
		{
		}
		else if (tag == 'P')
		{
		}
		else
		{
		  throw error("expected Hessian message ('p') at " + codeName(tag));
		}

		int major = Read();
		int minor = Read();

		return (major << 16) + minor;
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
		int tag = Read();

		if (tag != 'Z')
		{
		  error("expected end of message at " + codeName(tag));
		}
	  }

///  
///   <summary> * Reads a null
///   *
///   * <pre>
///   * N
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void readNull() throws IOException
	  public override void ReadNull()
	  {
		int tag = Read();

		switch (tag)
		{
		case 'N':
			return;

		default:
		  throw expect("null", tag);
		}
	  }

///  
///   <summary> * Reads a boolean
///   *
///   * <pre>
///   * T
///   * F
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public boolean readBoolean() throws IOException
	  public override bool ReadBoolean()
	  {
		int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		switch (tag)
		{
		case 'T':
			return true;
		case 'F':
			return false;

	  // direct integer
		case 0x80:
			case 0x81:
				case 0x82:
					case 0x83:
		case 0x84:
			case 0x85:
				case 0x86:
					case 0x87:
		case 0x88:
			case 0x89:
				case 0x8a:
					case 0x8b:
		case 0x8c:
			case 0x8d:
				case 0x8e:
					case 0x8f:

		case 0x90:
			case 0x91:
				case 0x92:
					case 0x93:
		case 0x94:
			case 0x95:
				case 0x96:
					case 0x97:
		case 0x98:
			case 0x99:
				case 0x9a:
					case 0x9b:
		case 0x9c:
			case 0x9d:
				case 0x9e:
					case 0x9f:

		case 0xa0:
			case 0xa1:
				case 0xa2:
					case 0xa3:
		case 0xa4:
			case 0xa5:
				case 0xa6:
					case 0xa7:
		case 0xa8:
			case 0xa9:
				case 0xaa:
					case 0xab:
		case 0xac:
			case 0xad:
				case 0xae:
					case 0xaf:

		case 0xb0:
			case 0xb1:
				case 0xb2:
					case 0xb3:
		case 0xb4:
			case 0xb5:
				case 0xb6:
					case 0xb7:
		case 0xb8:
			case 0xb9:
				case 0xba:
					case 0xbb:
		case 0xbc:
			case 0xbd:
				case 0xbe:
					case 0xbf:
		  return tag != Hessian2Constants.BC_INT_ZERO;

	  // INT_BYTE = 0
		case 0xc8:
		  return Read() != 0;

	  // INT_BYTE != 0
		case 0xc0:
			case 0xc1:
				case 0xc2:
					case 0xc3:
		case 0xc4:
			case 0xc5:
				case 0xc6:
					case 0xc7:
		case 0xc9:
			case 0xca:
				case 0xcb:
		case 0xcc:
			case 0xcd:
				case 0xce:
					case 0xcf:
		  Read();
		  return true;

	  // INT_SHORT = 0
		case 0xd4:
		  return (256 * Read() + Read()) != 0;

	  // INT_SHORT != 0
		case 0xd0:
			case 0xd1:
				case 0xd2:
					case 0xd3:
		case 0xd5:
			case 0xd6:
				case 0xd7:
		  Read();
		  Read();
		  return true;

		case 'I':
			return parseInt() != 0;

		case 0xd8:
			case 0xd9:
				case 0xda:
					case 0xdb:
		case 0xdc:
			case 0xdd:
				case 0xde:
					case 0xdf:

		case 0xe0:
			case 0xe1:
				case 0xe2:
					case 0xe3:
		case 0xe4:
			case 0xe5:
				case 0xe6:
					case 0xe7:
		case 0xe8:
			case 0xe9:
				case 0xea:
					case 0xeb:
		case 0xec:
			case 0xed:
				case 0xee:
					case 0xef:
		  return tag != Hessian2Constants.BC_LONG_ZERO;

	  // LONG_BYTE = 0
		case 0xf8:
		  return Read() != 0;

	  // LONG_BYTE != 0
		case 0xf0:
			case 0xf1:
				case 0xf2:
					case 0xf3:
		case 0xf4:
			case 0xf5:
				case 0xf6:
					case 0xf7:
		case 0xf9:
			case 0xfa:
				case 0xfb:
		case 0xfc:
			case 0xfd:
				case 0xfe:
					case 0xff:
		  Read();
		  return true;

	  // INT_SHORT = 0
		case 0x3c:
		  return (256 * Read() + Read()) != 0;

	  // INT_SHORT != 0
		case 0x38:
			case 0x39:
				case 0x3a:
					case 0x3b:
		case 0x3d:
			case 0x3e:
				case 0x3f:
		  Read();
		  Read();
		  return true;

		case Hessian2Constants.BC_LONG_INT:
		  return (0x1000000L * Read() + 0x10000L * Read() + 0x100 * Read() + Read()) != 0;

		case 'L':
		  return parseLong() != 0;

		case Hessian2Constants.BC_DOUBLE_ZERO:
		  return false;

		case Hessian2Constants.BC_DOUBLE_ONE:
		  return true;

		case Hessian2Constants.BC_DOUBLE_BYTE:
		  return Read() != 0;

		case Hessian2Constants.BC_DOUBLE_SHORT:
		  return (0x100 * Read() + Read()) != 0;

		case Hessian2Constants.BC_DOUBLE_MILL:
		  {
		int mills = parseInt();

		return mills != 0;
		  }

		case 'D':
		  return parseDouble() != 0.0;

		case 'N':
		  return false;

		default:
		  throw expect("boolean", tag);
		}
	  }

///  
///   <summary> * Reads a short
///   *
///   * <pre>
///   * I b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public short readShort() throws IOException
	  public virtual short ReadShort()
	  {
		return (short) ReadInt();
	  }

///  
///   <summary> * Reads an integer
///   *
///   * <pre>
///   * I b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int ReadInt() throws IOException
	  public sealed override int ReadInt()
	  {
	//int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();
		int tag = Read();

		switch (tag)
		{
		case 'N':
		  return 0;

		case 'F':
		  return 0;

		case 'T':
		  return 1;

	  // direct integer
		case 0x80:
			case 0x81:
				case 0x82:
					case 0x83:
		case 0x84:
			case 0x85:
				case 0x86:
					case 0x87:
		case 0x88:
			case 0x89:
				case 0x8a:
					case 0x8b:
		case 0x8c:
			case 0x8d:
				case 0x8e:
					case 0x8f:

		case 0x90:
			case 0x91:
				case 0x92:
					case 0x93:
		case 0x94:
			case 0x95:
				case 0x96:
					case 0x97:
		case 0x98:
			case 0x99:
				case 0x9a:
					case 0x9b:
		case 0x9c:
			case 0x9d:
				case 0x9e:
					case 0x9f:

		case 0xa0:
			case 0xa1:
				case 0xa2:
					case 0xa3:
		case 0xa4:
			case 0xa5:
				case 0xa6:
					case 0xa7:
		case 0xa8:
			case 0xa9:
				case 0xaa:
					case 0xab:
		case 0xac:
			case 0xad:
				case 0xae:
					case 0xaf:

		case 0xb0:
			case 0xb1:
				case 0xb2:
					case 0xb3:
		case 0xb4:
			case 0xb5:
				case 0xb6:
					case 0xb7:
		case 0xb8:
			case 0xb9:
				case 0xba:
					case 0xbb:
		case 0xbc:
			case 0xbd:
				case 0xbe:
					case 0xbf:
		  return tag - Hessian2Constants.BC_INT_ZERO;

	  /* byte int */
		case 0xc0:
			case 0xc1:
				case 0xc2:
					case 0xc3:
		case 0xc4:
			case 0xc5:
				case 0xc6:
					case 0xc7:
		case 0xc8:
			case 0xc9:
				case 0xca:
					case 0xcb:
		case 0xcc:
			case 0xcd:
				case 0xce:
					case 0xcf:
		  return ((tag - Hessian2Constants.BC_INT_BYTE_ZERO) << 8) + Read();

	  /* short int */
		case 0xd0:
			case 0xd1:
				case 0xd2:
					case 0xd3:
		case 0xd4:
			case 0xd5:
				case 0xd6:
					case 0xd7:
		  return ((tag - Hessian2Constants.BC_INT_SHORT_ZERO) << 16) + 256 * Read() + Read();

		case 'I':
		case Hessian2Constants.BC_LONG_INT:
		  return ((Read() << 24) + (Read() << 16) + (Read() << 8) + Read());

	  // direct long
		case 0xd8:
			case 0xd9:
				case 0xda:
					case 0xdb:
		case 0xdc:
			case 0xdd:
				case 0xde:
					case 0xdf:

		case 0xe0:
			case 0xe1:
				case 0xe2:
					case 0xe3:
		case 0xe4:
			case 0xe5:
				case 0xe6:
					case 0xe7:
		case 0xe8:
			case 0xe9:
				case 0xea:
					case 0xeb:
		case 0xec:
			case 0xed:
				case 0xee:
					case 0xef:
		  return tag - Hessian2Constants.BC_LONG_ZERO;

	  /* byte long */
		case 0xf0:
			case 0xf1:
				case 0xf2:
					case 0xf3:
		case 0xf4:
			case 0xf5:
				case 0xf6:
					case 0xf7:
		case 0xf8:
			case 0xf9:
				case 0xfa:
					case 0xfb:
		case 0xfc:
			case 0xfd:
				case 0xfe:
					case 0xff:
		  return ((tag - Hessian2Constants.BC_LONG_BYTE_ZERO) << 8) + Read();

	  /* short long */
		case 0x38:
			case 0x39:
				case 0x3a:
					case 0x3b:
		case 0x3c:
			case 0x3d:
				case 0x3e:
					case 0x3f:
		  return ((tag - Hessian2Constants.BC_LONG_SHORT_ZERO) << 16) + 256 * Read() + Read();

		case 'L':
		  return (int) parseLong();

		case Hessian2Constants.BC_DOUBLE_ZERO:
		  return 0;

		case Hessian2Constants.BC_DOUBLE_ONE:
		  return 1;

	  //case LONG_BYTE:
		case Hessian2Constants.BC_DOUBLE_BYTE:
		  return (sbyte)(_offset < _length ? _buffer[_offset++] : Read());

	  //case INT_SHORT:
	  //case LONG_SHORT:
		case Hessian2Constants.BC_DOUBLE_SHORT:
		  return (short)(256 * Read() + Read());

		case Hessian2Constants.BC_DOUBLE_MILL:
		  {
		int mills = parseInt();

		return (int)(0.001 * mills);
		  }

		case 'D':
		  return (int) parseDouble();

		default:
		  throw expect("integer", tag);
		}
	  }

///  
///   <summary> * Reads a long
///   *
///   * <pre>
///   * L b64 b56 b48 b40 b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public long readLong() throws IOException
	  public override long ReadLong()
	  {
		int tag = Read();

		switch (tag)
		{
		case 'N':
		  return 0;

		case 'F':
		  return 0;

		case 'T':
		  return 1;

	  // direct integer
		case 0x80:
			case 0x81:
				case 0x82:
					case 0x83:
		case 0x84:
			case 0x85:
				case 0x86:
					case 0x87:
		case 0x88:
			case 0x89:
				case 0x8a:
					case 0x8b:
		case 0x8c:
			case 0x8d:
				case 0x8e:
					case 0x8f:

		case 0x90:
			case 0x91:
				case 0x92:
					case 0x93:
		case 0x94:
			case 0x95:
				case 0x96:
					case 0x97:
		case 0x98:
			case 0x99:
				case 0x9a:
					case 0x9b:
		case 0x9c:
			case 0x9d:
				case 0x9e:
					case 0x9f:

		case 0xa0:
			case 0xa1:
				case 0xa2:
					case 0xa3:
		case 0xa4:
			case 0xa5:
				case 0xa6:
					case 0xa7:
		case 0xa8:
			case 0xa9:
				case 0xaa:
					case 0xab:
		case 0xac:
			case 0xad:
				case 0xae:
					case 0xaf:

		case 0xb0:
			case 0xb1:
				case 0xb2:
					case 0xb3:
		case 0xb4:
			case 0xb5:
				case 0xb6:
					case 0xb7:
		case 0xb8:
			case 0xb9:
				case 0xba:
					case 0xbb:
		case 0xbc:
			case 0xbd:
				case 0xbe:
					case 0xbf:
		  return tag - Hessian2Constants.BC_INT_ZERO;

	  /* byte int */
		case 0xc0:
			case 0xc1:
				case 0xc2:
					case 0xc3:
		case 0xc4:
			case 0xc5:
				case 0xc6:
					case 0xc7:
		case 0xc8:
			case 0xc9:
				case 0xca:
					case 0xcb:
		case 0xcc:
			case 0xcd:
				case 0xce:
					case 0xcf:
		  return ((tag - Hessian2Constants.BC_INT_BYTE_ZERO) << 8) + Read();

	  /* short int */
		case 0xd0:
			case 0xd1:
				case 0xd2:
					case 0xd3:
		case 0xd4:
			case 0xd5:
				case 0xd6:
					case 0xd7:
		  return ((tag - Hessian2Constants.BC_INT_SHORT_ZERO) << 16) + 256 * Read() + Read();

	  //case LONG_BYTE:
		case Hessian2Constants.BC_DOUBLE_BYTE:
		  return (sbyte)(_offset < _length ? _buffer[_offset++] : Read());

	  //case INT_SHORT:
	  //case LONG_SHORT:
		case Hessian2Constants.BC_DOUBLE_SHORT:
		  return (short)(256 * Read() + Read());

		case 'I':
		case Hessian2Constants.BC_LONG_INT:
		  return parseInt();

	  // direct long
		case 0xd8:
			case 0xd9:
				case 0xda:
					case 0xdb:
		case 0xdc:
			case 0xdd:
				case 0xde:
					case 0xdf:

		case 0xe0:
			case 0xe1:
				case 0xe2:
					case 0xe3:
		case 0xe4:
			case 0xe5:
				case 0xe6:
					case 0xe7:
		case 0xe8:
			case 0xe9:
				case 0xea:
					case 0xeb:
		case 0xec:
			case 0xed:
				case 0xee:
					case 0xef:
		  return tag - Hessian2Constants.BC_LONG_ZERO;

	  /* byte long */
		case 0xf0:
			case 0xf1:
				case 0xf2:
					case 0xf3:
		case 0xf4:
			case 0xf5:
				case 0xf6:
					case 0xf7:
		case 0xf8:
			case 0xf9:
				case 0xfa:
					case 0xfb:
		case 0xfc:
			case 0xfd:
				case 0xfe:
					case 0xff:
		  return ((tag - Hessian2Constants.BC_LONG_BYTE_ZERO) << 8) + Read();

	  /* short long */
		case 0x38:
			case 0x39:
				case 0x3a:
					case 0x3b:
		case 0x3c:
			case 0x3d:
				case 0x3e:
					case 0x3f:
		  return ((tag - Hessian2Constants.BC_LONG_SHORT_ZERO) << 16) + 256 * Read() + Read();

		case 'L':
		  return parseLong();

		case Hessian2Constants.BC_DOUBLE_ZERO:
		  return 0;

		case Hessian2Constants.BC_DOUBLE_ONE:
		  return 1;

		case Hessian2Constants.BC_DOUBLE_MILL:
		  {
		int mills = parseInt();

		return (long)(0.001 * mills);
		  }

		case 'D':
		  return (long) parseDouble();

		default:
		  throw expect("long", tag);
		}
	  }

///  
///   <summary> * Reads a float
///   *
///   * <pre>
///   * D b64 b56 b48 b40 b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public float readFloat() throws IOException
	  public virtual float readFloat()
	  {
		return (float) ReadDouble();
	  }

///  
///   <summary> * Reads a double
///   *
///   * <pre>
///   * D b64 b56 b48 b40 b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public double ReadDouble() throws IOException
	  public override double ReadDouble()
	  {
		int tag = Read();

		switch (tag)
		{
		case 'N':
		  return 0;

		case 'F':
		  return 0;

		case 'T':
		  return 1;

	  // direct integer
		case 0x80:
			case 0x81:
				case 0x82:
					case 0x83:
		case 0x84:
			case 0x85:
				case 0x86:
					case 0x87:
		case 0x88:
			case 0x89:
				case 0x8a:
					case 0x8b:
		case 0x8c:
			case 0x8d:
				case 0x8e:
					case 0x8f:

		case 0x90:
			case 0x91:
				case 0x92:
					case 0x93:
		case 0x94:
			case 0x95:
				case 0x96:
					case 0x97:
		case 0x98:
			case 0x99:
				case 0x9a:
					case 0x9b:
		case 0x9c:
			case 0x9d:
				case 0x9e:
					case 0x9f:

		case 0xa0:
			case 0xa1:
				case 0xa2:
					case 0xa3:
		case 0xa4:
			case 0xa5:
				case 0xa6:
					case 0xa7:
		case 0xa8:
			case 0xa9:
				case 0xaa:
					case 0xab:
		case 0xac:
			case 0xad:
				case 0xae:
					case 0xaf:

		case 0xb0:
			case 0xb1:
				case 0xb2:
					case 0xb3:
		case 0xb4:
			case 0xb5:
				case 0xb6:
					case 0xb7:
		case 0xb8:
			case 0xb9:
				case 0xba:
					case 0xbb:
		case 0xbc:
			case 0xbd:
				case 0xbe:
					case 0xbf:
		  return tag - 0x90;

	  /* byte int */
		case 0xc0:
			case 0xc1:
				case 0xc2:
					case 0xc3:
		case 0xc4:
			case 0xc5:
				case 0xc6:
					case 0xc7:
		case 0xc8:
			case 0xc9:
				case 0xca:
					case 0xcb:
		case 0xcc:
			case 0xcd:
				case 0xce:
					case 0xcf:
		  return ((tag - Hessian2Constants.BC_INT_BYTE_ZERO) << 8) + Read();

	  /* short int */
		case 0xd0:
			case 0xd1:
				case 0xd2:
					case 0xd3:
		case 0xd4:
			case 0xd5:
				case 0xd6:
					case 0xd7:
		  return ((tag - Hessian2Constants.BC_INT_SHORT_ZERO) << 16) + 256 * Read() + Read();

		case 'I':
		case Hessian2Constants.BC_LONG_INT:
		  return parseInt();

	  // direct long
		case 0xd8:
			case 0xd9:
				case 0xda:
					case 0xdb:
		case 0xdc:
			case 0xdd:
				case 0xde:
					case 0xdf:

		case 0xe0:
			case 0xe1:
				case 0xe2:
					case 0xe3:
		case 0xe4:
			case 0xe5:
				case 0xe6:
					case 0xe7:
		case 0xe8:
			case 0xe9:
				case 0xea:
					case 0xeb:
		case 0xec:
			case 0xed:
				case 0xee:
					case 0xef:
		  return tag - Hessian2Constants.BC_LONG_ZERO;

	  /* byte long */
		case 0xf0:
			case 0xf1:
				case 0xf2:
					case 0xf3:
		case 0xf4:
			case 0xf5:
				case 0xf6:
					case 0xf7:
		case 0xf8:
			case 0xf9:
				case 0xfa:
					case 0xfb:
		case 0xfc:
			case 0xfd:
				case 0xfe:
					case 0xff:
		  return ((tag - Hessian2Constants.BC_LONG_BYTE_ZERO) << 8) + Read();

	  /* short long */
		case 0x38:
			case 0x39:
				case 0x3a:
					case 0x3b:
		case 0x3c:
			case 0x3d:
				case 0x3e:
					case 0x3f:
		  return ((tag - Hessian2Constants.BC_LONG_SHORT_ZERO) << 16) + 256 * Read() + Read();

		case 'L':
		  return (double) parseLong();

		case Hessian2Constants.BC_DOUBLE_ZERO:
		  return 0;

		case Hessian2Constants.BC_DOUBLE_ONE:
		  return 1;

		case Hessian2Constants.BC_DOUBLE_BYTE:
		  return (sbyte)(_offset < _length ? _buffer[_offset++] : Read());

		case Hessian2Constants.BC_DOUBLE_SHORT:
		  return (short)(256 * Read() + Read());

		case Hessian2Constants.BC_DOUBLE_MILL:
		  {
		int mills = parseInt();

		return 0.001 * mills;
		  }

		case 'D':
		  return parseDouble();

		default:
		  throw expect("double", tag);
		}
	  }

///  
///   <summary> * Reads a date.
///   *
///   * <pre>
///   * T b64 b56 b48 b40 b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public long ReadUTCDate() throws IOException
	  public override long ReadUTCDate()
	  {
		int tag = Read();

		if (tag == Hessian2Constants.BC_DATE)
		{
		  return parseLong();
		}
    if (tag == Hessian2Constants.BC_DATE_MINUTE)
    {
        return parseInt() * 60000L;
    }
    throw expect("date", tag);
	  }

///  
///   <summary> * Reads a byte from the stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadChar() throws IOException
	  public virtual int ReadChar()
	  {
		if (_chunkLength > 0)
		{
		  _chunkLength--;
		  if (_chunkLength == 0 && _isLastChunk)
		  {
			_chunkLength = END_OF_DATA;
		  }

		  int ch = parseUTF8Char();
		  return ch;
		}
    if (_chunkLength == END_OF_DATA)
    {
        _chunkLength = 0;
        return -1;
    }

    int tag = Read();

		switch (tag)
		{
		case 'N':
		  return -1;

		case 'S':
		case Hessian2Constants.BC_STRING_CHUNK:
		  _isLastChunk = tag == 'S';
		  _chunkLength = (Read() << 8) + Read();

		  _chunkLength--;
		  int value = parseUTF8Char();

	  // special code so successive Read byte won't
	  // be Read as a single object.
		  if (_chunkLength == 0 && _isLastChunk)
		  {
			_chunkLength = END_OF_DATA;
		  }

		  return value;

		default:
		  throw expect("char", tag);
		}
	  }

///  
///   <summary> * Reads a byte array from the stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadString(char [] buffer, int offset, int length) throws IOException
	  public virtual int ReadString(char[] buffer, int offset, int length)
	  {
		int ReadLength = 0;

		if (_chunkLength == END_OF_DATA)
		{
		  _chunkLength = 0;
		  return -1;
		}
    if (_chunkLength == 0)
    {
        int tag = Read();

        switch (tag)
        {
            case 'N':
                return -1;

            case 'S':
            case Hessian2Constants.BC_STRING_CHUNK:
                _isLastChunk = tag == 'S';
                _chunkLength = (Read() << 8) + Read();
                break;

            case 0x00:
            case 0x01:
            case 0x02:
            case 0x03:
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07:
            case 0x08:
            case 0x09:
            case 0x0a:
            case 0x0b:
            case 0x0c:
            case 0x0d:
            case 0x0e:
            case 0x0f:

            case 0x10:
            case 0x11:
            case 0x12:
            case 0x13:
            case 0x14:
            case 0x15:
            case 0x16:
            case 0x17:
            case 0x18:
            case 0x19:
            case 0x1a:
            case 0x1b:
            case 0x1c:
            case 0x1d:
            case 0x1e:
            case 0x1f:
                _isLastChunk = true;
                _chunkLength = tag - 0x00;
                break;

            case 0x30:
            case 0x31:
            case 0x32:
            case 0x33:
                _isLastChunk = true;
                _chunkLength = (tag - 0x30) * 256 + Read();
                break;

            default:
                throw expect("string", tag);
        }
    }

    while (length > 0)
		{
		  if (_chunkLength > 0)
		  {
			buffer[offset++] = (char) parseUTF8Char();
			_chunkLength--;
			length--;
			ReadLength++;
		  }
		  else if (_isLastChunk)
		  {
			if (ReadLength == 0)
			{
			  return -1;
			}
		      _chunkLength = END_OF_DATA;
		      return ReadLength;
		  }
		  else
		  {
			int tag = Read();

			switch (tag)
			{
			case 'S':
			case Hessian2Constants.BC_STRING_CHUNK:
			  _isLastChunk = tag == 'S';
			  _chunkLength = (Read() << 8) + Read();
			  break;

		case 0x00:
			case 0x01:
				case 0x02:
					case 0x03:
		case 0x04:
			case 0x05:
				case 0x06:
					case 0x07:
		case 0x08:
			case 0x09:
				case 0x0a:
					case 0x0b:
		case 0x0c:
			case 0x0d:
				case 0x0e:
					case 0x0f:

		case 0x10:
			case 0x11:
				case 0x12:
					case 0x13:
		case 0x14:
			case 0x15:
				case 0x16:
					case 0x17:
		case 0x18:
			case 0x19:
				case 0x1a:
					case 0x1b:
		case 0x1c:
			case 0x1d:
				case 0x1e:
					case 0x1f:
		  _isLastChunk = true;
		  _chunkLength = tag - 0x00;
		  break;

		case 0x30:
			case 0x31:
				case 0x32:
					case 0x33:
		  _isLastChunk = true;
		  _chunkLength = (tag - 0x30) * 256 + Read();
		  break;

		default:
			  throw expect("string", tag);
			}
		  }
		}

		if (ReadLength == 0)
		{
		  return -1;
		}
    if (_chunkLength > 0 || ! _isLastChunk)
    {
        return ReadLength;
    }
    _chunkLength = END_OF_DATA;
    return ReadLength;
	  }

///  
///   <summary> * Reads a string
///   *
///   * <pre>
///   * S b16 b8 string value
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public String ReadString() throws IOException
	  public override string ReadString()
	  {
		int tag = Read();

		switch (tag)
		{
		case 'N':
		  return null;
		case 'T':
		  return "true";
		case 'F':
		  return "false";

	  // direct integer
		case 0x80:
			case 0x81:
				case 0x82:
					case 0x83:
		case 0x84:
			case 0x85:
				case 0x86:
					case 0x87:
		case 0x88:
			case 0x89:
				case 0x8a:
					case 0x8b:
		case 0x8c:
			case 0x8d:
				case 0x8e:
					case 0x8f:

		case 0x90:
			case 0x91:
				case 0x92:
					case 0x93:
		case 0x94:
			case 0x95:
				case 0x96:
					case 0x97:
		case 0x98:
			case 0x99:
				case 0x9a:
					case 0x9b:
		case 0x9c:
			case 0x9d:
				case 0x9e:
					case 0x9f:

		case 0xa0:
			case 0xa1:
				case 0xa2:
					case 0xa3:
		case 0xa4:
			case 0xa5:
				case 0xa6:
					case 0xa7:
		case 0xa8:
			case 0xa9:
				case 0xaa:
					case 0xab:
		case 0xac:
			case 0xad:
				case 0xae:
					case 0xaf:

		case 0xb0:
			case 0xb1:
				case 0xb2:
					case 0xb3:
		case 0xb4:
			case 0xb5:
				case 0xb6:
					case 0xb7:
		case 0xb8:
			case 0xb9:
				case 0xba:
					case 0xbb:
		case 0xbc:
			case 0xbd:
				case 0xbe:
					case 0xbf:
		  return Convert.ToString((tag - 0x90));

	  /* byte int */
		case 0xc0:
			case 0xc1:
				case 0xc2:
					case 0xc3:
		case 0xc4:
			case 0xc5:
				case 0xc6:
					case 0xc7:
		case 0xc8:
			case 0xc9:
				case 0xca:
					case 0xcb:
		case 0xcc:
			case 0xcd:
				case 0xce:
					case 0xcf:
		  return Convert.ToString(((tag - Hessian2Constants.BC_INT_BYTE_ZERO) << 8) + Read());

	  /* short int */
		case 0xd0:
			case 0xd1:
				case 0xd2:
					case 0xd3:
		case 0xd4:
			case 0xd5:
				case 0xd6:
					case 0xd7:
		  return Convert.ToString(((tag - Hessian2Constants.BC_INT_SHORT_ZERO) << 16) + 256 * Read() + Read());

		case 'I':
		case Hessian2Constants.BC_LONG_INT:
		  return Convert.ToString(parseInt());

	  // direct long
		case 0xd8:
			case 0xd9:
				case 0xda:
					case 0xdb:
		case 0xdc:
			case 0xdd:
				case 0xde:
					case 0xdf:

		case 0xe0:
			case 0xe1:
				case 0xe2:
					case 0xe3:
		case 0xe4:
			case 0xe5:
				case 0xe6:
					case 0xe7:
		case 0xe8:
			case 0xe9:
				case 0xea:
					case 0xeb:
		case 0xec:
			case 0xed:
				case 0xee:
					case 0xef:
		  return Convert.ToString(tag - Hessian2Constants.BC_LONG_ZERO);

	  /* byte long */
		case 0xf0:
			case 0xf1:
				case 0xf2:
					case 0xf3:
		case 0xf4:
			case 0xf5:
				case 0xf6:
					case 0xf7:
		case 0xf8:
			case 0xf9:
				case 0xfa:
					case 0xfb:
		case 0xfc:
			case 0xfd:
				case 0xfe:
					case 0xff:
		  return Convert.ToString(((tag - Hessian2Constants.BC_LONG_BYTE_ZERO) << 8) + Read());

	  /* short long */
		case 0x38:
			case 0x39:
				case 0x3a:
					case 0x3b:
		case 0x3c:
			case 0x3d:
				case 0x3e:
					case 0x3f:
		  return Convert.ToString(((tag - Hessian2Constants.BC_LONG_SHORT_ZERO) << 16) + 256 * Read() + Read());

		case 'L':
		  return Convert.ToString(parseLong());

		case Hessian2Constants.BC_DOUBLE_ZERO:
		  return "0.0";

		case Hessian2Constants.BC_DOUBLE_ONE:
		  return "1.0";

		case Hessian2Constants.BC_DOUBLE_BYTE:
		  return Convert.ToString((sbyte)(_offset < _length ? _buffer[_offset++] : Read()));

		case Hessian2Constants.BC_DOUBLE_SHORT:
		  return Convert.ToString(((short)(256 * Read() + Read())));

		case Hessian2Constants.BC_DOUBLE_MILL:
		  {
		int mills = parseInt();

		return Convert.ToString(0.001 * mills);
		  }

		case 'D':
		  return Convert.ToString(parseDouble());

		case 'S':
		case Hessian2Constants.BC_STRING_CHUNK:
		  _isLastChunk = tag == 'S';
		  _chunkLength = (Read() << 8) + Read();

		  _sbuf.Length = 0;
		  int ch;

		  while ((ch = parseChar()) >= 0)
		  {
			_sbuf.Append((char) ch);
		  }

		  return _sbuf.ToString();

	  // 0-byte string
		case 0x00:
			case 0x01:
				case 0x02:
					case 0x03:
		case 0x04:
			case 0x05:
				case 0x06:
					case 0x07:
		case 0x08:
			case 0x09:
				case 0x0a:
					case 0x0b:
		case 0x0c:
			case 0x0d:
				case 0x0e:
					case 0x0f:

		case 0x10:
			case 0x11:
				case 0x12:
					case 0x13:
		case 0x14:
			case 0x15:
				case 0x16:
					case 0x17:
		case 0x18:
			case 0x19:
				case 0x1a:
					case 0x1b:
		case 0x1c:
			case 0x1d:
				case 0x1e:
					case 0x1f:
		  _isLastChunk = true;
		  _chunkLength = tag - 0x00;

		  _sbuf.Length = 0;

		  while ((ch = parseChar()) >= 0)
		  {
			_sbuf.Append((char) ch);
		  }

		  return _sbuf.ToString();

		case 0x30:
			case 0x31:
				case 0x32:
					case 0x33:
		  _isLastChunk = true;
		  _chunkLength = (tag - 0x30) * 256 + Read();

		  _sbuf.Length = 0;

		  while ((ch = parseChar()) >= 0)
		  {
			_sbuf.Append((char) ch);
		  }

		  return _sbuf.ToString();

		default:
		  throw expect("string", tag);
		}
	  }

///  
///   <summary> * Reads a byte array
///   *
///   * <pre>
///   * B b16 b8 data value
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public byte [] ReadBytes() throws IOException
	  public override byte [] ReadBytes()
	  {
          int tag = Read();

			switch (tag) 
			{
				case PROT_NULL:
					return null;
				case Hessian2Constants.BC_BINARY:
		        case Hessian2Constants.BC_BINARY_CHUNK:
			        {
			            _isLastChunk = tag == Hessian2Constants.BC_BINARY;
			            _chunkLength = (Read() << 8) + Read();
			            MemoryStream memoryStream = new MemoryStream();
			            int data;
			            while ((data = parseByte()) >= 0)
			                memoryStream.WriteByte((Byte) data);
			            return memoryStream.ToArray();
			        }
			    case 0x20: case 0x21: case 0x22: case 0x23:
		case 0x24: case 0x25: case 0x26: case 0x27:
		case 0x28: case 0x29: case 0x2a: case 0x2b:
		case 0x2c: case 0x2d: case 0x2e: case 0x2f:
                    {
		_isLastChunk = true;
		_chunkLength = tag - 0x20;

		byte[] buffer = new byte[_chunkLength];

		int offset = 0;
		while (offset < _chunkLength)
		{
		  int sublen = Read(buffer, 0, _chunkLength - offset);

		  if (sublen <= 0)
		  {
			break;
		  }

		  offset += sublen;
		}

		return buffer;
		  }
        case 0x34:case 0x35:case 0x36:case 0x37:
		  {
		_isLastChunk = true;
		_chunkLength = (tag - 0x34) * 256 + Read();

		byte[] buffer = new byte[_chunkLength];

		int offset = 0;
		while (offset < _chunkLength)
		{
		  int sublen = Read(buffer, 0, _chunkLength - offset);

		  if (sublen <= 0)
		  {
			break;
		  }

		  offset += sublen;
		}

		return buffer;
		  }
				default:
					 throw expect("bytes", tag);
			}
	  }

///  
///   <summary> * Reads a byte from the stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadByte() throws IOException
	  public virtual int ReadByte()
	  {
		if (_chunkLength > 0)
		{
		  _chunkLength--;
		  if (_chunkLength == 0 && _isLastChunk)
		  {
			_chunkLength = END_OF_DATA;
		  }

		  return Read();
		}
    if (_chunkLength == END_OF_DATA)
    {
        _chunkLength = 0;
        return -1;
    }

    int tag = Read();

		switch (tag)
		{
		case 'N':
		  return -1;

		case 'B':
		case Hessian2Constants.BC_BINARY_CHUNK:
		  {
		_isLastChunk = tag == 'B';
		_chunkLength = (Read() << 8) + Read();

		int value = parseByte();

	// special code so successive Read byte won't
	// be Read as a single object.
		if (_chunkLength == 0 && _isLastChunk)
		{
		  _chunkLength = END_OF_DATA;
		}

		return value;
		  }

		case 0x20:
			case 0x21:
				case 0x22:
					case 0x23:
		case 0x24:
			case 0x25:
				case 0x26:
					case 0x27:
		case 0x28:
			case 0x29:
				case 0x2a:
					case 0x2b:
		case 0x2c:
			case 0x2d:
				case 0x2e:
					case 0x2f:
		  {
		_isLastChunk = true;
		_chunkLength = tag - 0x20;

		int value = parseByte();

	// special code so successive Read byte won't
	// be Read as a single object.
		if (_chunkLength == 0)
		{
		  _chunkLength = END_OF_DATA;
		}

		return value;
		  }

		case 0x34:
			case 0x35:
				case 0x36:
					case 0x37:
		  {
		_isLastChunk = true;
		_chunkLength = (tag - 0x34) * 256 + Read();

		int value = parseByte();

	// special code so successive Read byte won't
	// be Read as a single object.
		if (_chunkLength == 0)
		{
		  _chunkLength = END_OF_DATA;
		}

		return value;
		  }

		default:
		  throw expect("binary", tag);
		}
	  }

///  
///   <summary> * Reads a byte array from the stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadBytes(byte [] buffer, int offset, int length) throws IOException
	  public virtual int ReadBytes(sbyte[] buffer, int offset, int length)
	  {
		int ReadLength = 0;

		if (_chunkLength == END_OF_DATA)
		{
		  _chunkLength = 0;
		  return -1;
		}
    if (_chunkLength == 0)
    {
        int tag = Read();

        switch (tag)
        {
            case 'N':
                return -1;

            case 'B':
            case Hessian2Constants.BC_BINARY_CHUNK:
                _isLastChunk = tag == 'B';
                _chunkLength = (Read() << 8) + Read();
                break;

            case 0x20:
            case 0x21:
            case 0x22:
            case 0x23:
            case 0x24:
            case 0x25:
            case 0x26:
            case 0x27:
            case 0x28:
            case 0x29:
            case 0x2a:
            case 0x2b:
            case 0x2c:
            case 0x2d:
            case 0x2e:
            case 0x2f:
                {
                    _isLastChunk = true;
                    _chunkLength = tag - 0x20;
                    break;
                }

            case 0x34:
            case 0x35:
            case 0x36:
            case 0x37:
                {
                    _isLastChunk = true;
                    _chunkLength = (tag - 0x34) * 256 + Read();
                    break;
                }

            default:
                throw expect("binary", tag);
        }
    }

    while (length > 0)
		{
		  if (_chunkLength > 0)
		  {
			buffer[offset++] = (sbyte) Read();
			_chunkLength--;
			length--;
			ReadLength++;
		  }
		  else if (_isLastChunk)
		  {
			if (ReadLength == 0)
			{
			  return -1;
			}
		      _chunkLength = END_OF_DATA;
		      return ReadLength;
		  }
		  else
		  {
			int tag = Read();

			switch (tag)
			{
			case 'B':
			case Hessian2Constants.BC_BINARY_CHUNK:
			  _isLastChunk = tag == 'B';
			  _chunkLength = (Read() << 8) + Read();
			  break;

			default:
			  throw expect("binary", tag);
			}
		  }
		}

		if (ReadLength == 0)
		{
		  return -1;
		}
    if (_chunkLength > 0 || ! _isLastChunk)
    {
        return ReadLength;
    }
    _chunkLength = END_OF_DATA;
    return ReadLength;
	  }

///  
///   <summary> * Reads a fault. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private HashMap ReadFault() throws IOException
	  private Hashtable ReadFault()
	  {
		Hashtable map = new Hashtable();

		int code = Read();
		for (; code > 0 && code != 'Z'; code = Read())
		{
		  _offset--;

		  object key = ReadObject();
		  object value = ReadObject();

		  if (key != null && value != null)
		  {
			map.Add(key, value);
		  }
		}

		if (code != 'Z')
		{
		  throw expect("fault", code);
		}

		return map;
	  }

///  
///   <summary> * Reads an object from the input stream with an expected type. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object ReadObject(Class cl) throws IOException
	  public override object ReadObject(Type cl)
	  {
		if (cl == null || cl == typeof(object))
		{
		  return ReadObject();
		}

		int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		switch (tag)
		{
		case 'N':
		  return null;

		case 'H':
		  {
		AbstractDeserializer reader = findSerializerFactory().GetDeserializer(cl);

		return reader.ReadMap(this);
		  }

		case 'M':
		  {
		string type = ReadType();

	// hessian/3bb3
		if ("".Equals(type))
		{
		    AbstractDeserializer reader = findSerializerFactory().GetDeserializer(cl);

		    return reader.ReadMap(this);
		}
		else
		{
		    AbstractDeserializer reader = findSerializerFactory().GetObjectDeserializer(type, cl);

		    return reader.ReadMap(this);
		}
		  }
		case 'C':
		  {
		ReadObjectDefinition(cl);

		return ReadObject(cl);
		  }

		case 0x60:case 0x61:case 0x62:case 0x63:
		case 0x64:case 0x65:case 0x66:case 0x67:
		case 0x68:case 0x69:case 0x6a:case 0x6b:
		case 0x6c:case 0x6d:case 0x6e:case 0x6f:
		  {
		int @ref = tag - 0x60;
		int size = _classDefs.Count;

		if (@ref < 0 || size <= @ref)
		{
		  throw new CHessianException("'" + @ref + "' is an unknown class definition");
		}

		ObjectDefinition def = _classDefs[@ref];

		return ReadObjectInstance(cl, def);
		  }

		case 'O':
		  {
		int @ref = ReadInt();
		int size = _classDefs.Count;

		if (@ref < 0 || size <= @ref)
		{
		  throw new HessianException("'" + @ref + "' is an unknown class definition");
		}

		ObjectDefinition def = _classDefs[@ref];

		return ReadObjectInstance(cl, def);
		  }

		case Hessian2Constants.BC_LIST_VARIABLE:
		  {
		string type = ReadType();

		      AbstractDeserializer reader = findSerializerFactory().GetListDeserializer(type, cl);

		object v = reader.ReadList(this, -1);

		return v;
		  }

		case Hessian2Constants.BC_LIST_FIXED:
		  {
		string type = ReadType();
		int length = ReadInt();

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(type, cl);

		object v = Reader.ReadLengthList(this, length);

		return v;
		  }

		case 0x70:case 0x71:case 0x72:case 0x73:
		case 0x74:case 0x75:case 0x76:case 0x77:
		  {
		int length = tag - 0x70;

		string type = ReadType();

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(type, cl);

		object v = Reader.ReadLengthList(this, length);

		return v;
		  }

		case Hessian2Constants.BC_LIST_VARIABLE_UNTYPED:
		  {
		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(null, cl);

		object v = Reader.ReadList(this, -1);

		return v;
		  }

		case Hessian2Constants.BC_LIST_FIXED_UNTYPED:
		  {
		int length = ReadInt();

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(null, cl);

		object v = Reader.ReadLengthList(this, length);

		return v;
		  }

		case 0x78:case 0x79:case 0x7a:case 0x7b:
		case 0x7c:case 0x7d:case 0x7e:case 0x7f:
		  {
		int length = tag - 0x78;

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(null, cl);

		object v = Reader.ReadLengthList(this, length);

		return v;
		  }

		case Hessian2Constants.BC_REF:
		  {
		int @ref = ReadInt();

		return _refs[@ref];
		  }
		}

		if (tag >= 0)
		{
		  _offset--;
		}

	// hessian/3b2i vs hessian/3406
	// return ReadObject();
		object value = findSerializerFactory().GetDeserializer(cl).ReadObject(this);
		return value;
	  }

///  
///   <summary> * Reads an arbitrary object from the input stream when the type
///   * is unknown. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object ReadObject() throws IOException
	  public override object ReadObject()
	  {
		int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		switch (tag)
		{
		case 'N':
		  return null;

		case 'T':
		  return Convert.ToBoolean(true);

		case 'F':
		  return Convert.ToBoolean(false);

	  // direct integer
		case 0x80:case 0x81:case 0x82:case 0x83:
		case 0x84:case 0x85:case 0x86:case 0x87:
		case 0x88:case 0x89:case 0x8a:case 0x8b:
		case 0x8c:case 0x8d:case 0x8e:case 0x8f:

		case 0x90:case 0x91:case 0x92:case 0x93:
		case 0x94:case 0x95:case 0x96:case 0x97:
		case 0x98:case 0x99:case 0x9a:case 0x9b:
		case 0x9c:case 0x9d:case 0x9e:case 0x9f:

		case 0xa0:case 0xa1:case 0xa2:case 0xa3:
		case 0xa4:case 0xa5:case 0xa6:case 0xa7:
		case 0xa8:case 0xa9:case 0xaa:case 0xab:
		case 0xac:case 0xad:case 0xae:case 0xaf:

		case 0xb0:case 0xb1:case 0xb2:case 0xb3:
		case 0xb4:case 0xb5:case 0xb6:case 0xb7:
		case 0xb8:case 0xb9:case 0xba:case 0xbb:
		case 0xbc:case 0xbd:case 0xbe:case 0xbf:
		  return Convert.ToInt32(tag - Hessian2Constants.BC_INT_ZERO);

	  /* byte int */
		case 0xc0:case 0xc1:case 0xc2:case 0xc3:
		case 0xc4:case 0xc5:case 0xc6:case 0xc7:
		case 0xc8:case 0xc9:case 0xca:case 0xcb:
		case 0xcc:case 0xcd:case 0xce:case 0xcf:
		  return Convert.ToInt32(((tag - Hessian2Constants.BC_INT_BYTE_ZERO) << 8) + Read());

	  /* short int */
		case 0xd0:case 0xd1:case 0xd2:case 0xd3:
		case 0xd4:case 0xd5:case 0xd6:case 0xd7:
		  return Convert.ToInt32(((tag - Hessian2Constants.BC_INT_SHORT_ZERO) << 16) + 256 * Read() + Read());

		case 'I':
		  return Convert.ToInt32(parseInt());

	  // direct long
		case 0xd8:case 0xd9:case 0xda:case 0xdb:
		case 0xdc:case 0xdd:case 0xde:case 0xdf:

		case 0xe0:case 0xe1:case 0xe2:case 0xe3:
		case 0xe4:case 0xe5:case 0xe6:case 0xe7:
		case 0xe8:case 0xe9:case 0xea:case 0xeb:
		case 0xec:case 0xed:case 0xee:case 0xef:
		  return Convert.ToInt64(tag - Hessian2Constants.BC_LONG_ZERO);

	  /* byte long */
		case 0xf0:case 0xf1:case 0xf2:case 0xf3:
		case 0xf4:case 0xf5:case 0xf6:case 0xf7:
		case 0xf8:case 0xf9:case 0xfa:case 0xfb:
		case 0xfc:case 0xfd:case 0xfe:case 0xff:
		  return Convert.ToInt64(((tag - Hessian2Constants.BC_LONG_BYTE_ZERO) << 8) + Read());

	  /* short long */
		case 0x38:case 0x39:case 0x3a:case 0x3b:
		case 0x3c:case 0x3d:case 0x3e:case 0x3f:
		  return Convert.ToInt64(((tag - Hessian2Constants.BC_LONG_SHORT_ZERO) << 16) + 256 * Read() + Read());

		case Hessian2Constants.BC_LONG_INT:
		  return Convert.ToInt64(parseInt());

		case 'L':
		  return Convert.ToInt64(parseLong());

		case Hessian2Constants.BC_DOUBLE_ZERO:
		  return Convert.ToDouble(0);

		case Hessian2Constants.BC_DOUBLE_ONE:
		  return Convert.ToDouble(1);

		case Hessian2Constants.BC_DOUBLE_BYTE:
		  return Convert.ToDouble((sbyte) Read());

		case Hessian2Constants.BC_DOUBLE_SHORT:
		  return Convert.ToDouble((short)(256 * Read() + Read()));

		case Hessian2Constants.BC_DOUBLE_MILL:
		  {
		int mills = parseInt();

		return Convert.ToDouble(0.001 * mills);
		  }

		case 'D':
		  return Convert.ToDouble(parseDouble());

		case Hessian2Constants.BC_DATE:
		  return new DateTime(parseLong());

		case Hessian2Constants.BC_DATE_MINUTE:
		  return new DateTime(parseInt() * 60000L);

		case Hessian2Constants.BC_STRING_CHUNK:
		case 'S':
		  {
		_isLastChunk = tag == 'S';
		_chunkLength = (Read() << 8) + Read();

		int data;
		_sbuf.Length = 0;

		while ((data = parseChar()) >= 0)
		{
		  _sbuf.Append((char) data);
		}

		return _sbuf.ToString();
		  }

		case 0x00:case 0x01:case 0x02:case 0x03:
		case 0x04:case 0x05:case 0x06:case 0x07:
		case 0x08:case 0x09:case 0x0a:case 0x0b:
		case 0x0c:case 0x0d:case 0x0e:case 0x0f:

		case 0x10:case 0x11:case 0x12:case 0x13:
		case 0x14:case 0x15:case 0x16:case 0x17:
		case 0x18:case 0x19:case 0x1a:case 0x1b:
		case 0x1c:case 0x1d:case 0x1e:case 0x1f:
		  {
		_isLastChunk = true;
		_chunkLength = tag - 0x00;

		int data;
		_sbuf.Length = 0;

		while ((data = parseChar()) >= 0)
		{
		  _sbuf.Append((char) data);
		}

		return _sbuf.ToString();
		  }

		case 0x30:case 0x31:case 0x32:case 0x33:
		  {
		_isLastChunk = true;
		_chunkLength = (tag - 0x30) * 256 + Read();

		_sbuf.Length = 0;

		int ch;
		while ((ch = parseChar()) >= 0)
		{
		  _sbuf.Append((char) ch);
		}

		return _sbuf.ToString();
		  }

		case Hessian2Constants.BC_BINARY_CHUNK:
		case 'B':
		  {
		_isLastChunk = tag == 'B';
		_chunkLength = (Read() << 8) + Read();

		int data;
		ByteArrayOutputStream bos = new ByteArrayOutputStream();

		while ((data = parseByte()) >= 0)
		{
		  bos.write(data);
		}

		return bos.toByteArray();
		  }

		case 0x20:case 0x21:case 0x22:case 0x23:
		case 0x24:case 0x25:case 0x26:case 0x27:
		case 0x28:case 0x29:case 0x2a:case 0x2b:
		case 0x2c:case 0x2d:case 0x2e:case 0x2f:
		  {
		_isLastChunk = true;
		int len = tag - 0x20;
		_chunkLength = 0;

		sbyte[] data = new sbyte[len];

		for (int i = 0; i < len; i++)
		{
		  data[i] = (sbyte) Read();
		}

		return data;
		  }

		case 0x34:case 0x35:case 0x36:case 0x37:
		  {
		_isLastChunk = true;
		int len = (tag - 0x34) * 256 + Read();
		_chunkLength = 0;

		sbyte[] buffer = new sbyte[len];

		for (int i = 0; i < len; i++)
		{
		  buffer[i] = (sbyte) Read();
		}

		return buffer;
		  }

		case Hessian2Constants.BC_LIST_VARIABLE:
		  {
	// variable length list
		string type = ReadType();

		return findSerializerFactory().ReadList(this, -1, type);
		  }

		case Hessian2Constants.BC_LIST_VARIABLE_UNTYPED:
		  {
		return findSerializerFactory().ReadList(this, -1, null);
		  }

		case Hessian2Constants.BC_LIST_FIXED:
		  {
	// fixed length lists
		string type = ReadType();
		int length = ReadInt();

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(type, null);

		return Reader.ReadLengthList(this, length);
		  }

		case Hessian2Constants.BC_LIST_FIXED_UNTYPED:
		  {
	// fixed length lists
		int length = ReadInt();

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(null, null);

		return Reader.ReadLengthList(this, length);
		  }

	  // compact fixed list
		case 0x70:case 0x71:case 0x72:case 0x73:
		case 0x74:case 0x75:case 0x76:case 0x77:
		  {
	// fixed length lists
		string type = ReadType();
		int length = tag - 0x70;

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(type, null);

		return Reader.ReadLengthList(this, length);
		  }

	  // compact fixed untyped list
		case 0x78:case 0x79:case 0x7a:case 0x7b:
		case 0x7c:case 0x7d:case 0x7e:case 0x7f:
		  {
	// fixed length lists
		int length = tag - 0x78;

		      AbstractDeserializer Reader = findSerializerFactory().GetListDeserializer(null, null);

		return Reader.ReadLengthList(this, length);
		  }

		case 'H':
		  {
		return findSerializerFactory().ReadMap(this, null);
		  }

		case 'M':
		  {
		string type = ReadType();

		return findSerializerFactory().ReadMap(this, type);
		  }

		case 'C':
		  {
		ReadObjectDefinition(null);

		return ReadObject();
		  }

		case 0x60:case 0x61:case 0x62:case 0x63:
		case 0x64:case 0x65:case 0x66:case 0x67:
		case 0x68:case 0x69:case 0x6a:case 0x6b:
		case 0x6c:case 0x6d:case 0x6e:case 0x6f:
		  {
		int @ref = tag - 0x60;

		if (_classDefs.Count <= @ref)
		{
		  throw error("No classes defined at reference '" + tag.ToString("X") + "'");
		}

		ObjectDefinition def = _classDefs[@ref];

		return ReadObjectInstance(null, def);
		  }

		case 'O':
		  {
		int @ref = ReadInt();

		if (_classDefs.Count <= @ref)
		{
		  throw error("Illegal object reference #" + @ref);
		}

		ObjectDefinition def = _classDefs[@ref];

		return ReadObjectInstance(null, def);
		  }

		case Hessian2Constants.BC_REF:
		  {
		int @ref = ReadInt();

		return _refs[@ref];
		  }

		default:
		  if (tag < 0)
		  {
		throw new EOFException("ReadObject: unexpected end of file");
		  }
		        throw error("ReadObject: unknown code " + codeName(tag));
		}
	  }

///  
///   <summary> * Reads an object definition:
///   *
///   * <pre>
///   * O string <int> (string)* <value>*
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void ReadObjectDefinition(Class<?> cl) throws IOException
	  private void ReadObjectDefinition(Type cl)
	  {
		string type = ReadString();
		int len = ReadInt();

		CSerializerFactory factory = findSerializerFactory();

		AbstractDeserializer Reader = factory.GetObjectDeserializer(type, null);

		object[] fields = Reader.CreateFields(len);
		string[] fieldNames = new string[len];

		for (int i = 0; i < len; i++)
		{
		  string name = ReadString();

		  fields[i] = Reader.CreateField(name);
		  fieldNames[i] = name;
		}

		ObjectDefinition def = new ObjectDefinition(type, Reader, fields, fieldNames);

		_classDefs.Add(def);
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private Object ReadObjectInstance(Class<?> cl, ObjectDefinition def) throws IOException
	  private object ReadObjectInstance(Type cl, ObjectDefinition def)
	  {
		string type = def.Type;
		AbstractDeserializer Reader = def.Reader;
		object[] fields = def.Fields;

		CSerializerFactory factory = findSerializerFactory();

		if (cl != Reader.GetOwnType() && cl != null)
		{
		  Reader = factory.GetObjectDeserializer(type, cl);

		  return Reader.ReadObject(this, def.FieldNames);
		}
	      return Reader.ReadObject(this, fields);
	  }

///  
///   <summary> * Reads a remote object. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object ReadRemote() throws IOException
	  public override object ReadRemote()
	  {
		string type = ReadType();
		string url = ReadString();

		return resolveRemote(type, url);
	  }

   ///  
///   <summary> * Reads a reference. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object ReadRef() throws IOException
	  public override object ReadRef()
	  {
		return _refs[parseInt()];
	  }

///  
///   <summary> * Reads the start of a list. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadListStart() throws IOException
	  public override int ReadListStart()
	  {
		return Read();
	  }

///  
///   <summary> * Reads the start of a list. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadMapStart() throws IOException
	  public override int ReadMapStart()
	  {
		return Read();
	  }

///  
///   <summary> * Returns true if this is the end of a list or a map. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public boolean isEnd() throws IOException
	  public override bool IsEnd()
	  {
			int code;
    
			if (_offset < _length)
			{
			  code = (_buffer[_offset] & 0xff);
			}
			else
			{
			  code = Read();
    
			  if (code >= 0)
			  {
			_offset--;
			  }
			}
    
			return (code < 0 || code == 'Z');
	  }

///  
///   <summary> * Reads the end byte. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void ReadEnd() throws IOException
	  public override void ReadEnd()
	  {
		int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		if (code == 'Z')
		{
		  return;
		}
    if (code < 0)
    {
        throw error("unexpected end of file");
    }
    throw error("unknown code:" + codeName(code));
	  }

///  
///   <summary> * Reads the end byte. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void ReadMapEnd() throws IOException
	  public override void ReadMapEnd()
	  {
		int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		if (code != 'Z')
		{
		  throw error("expected end of map ('Z') at '" + codeName(code) + "'");
		}
	  }

///  
///   <summary> * Reads the end byte. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void ReadListEnd() throws IOException
	  public override void ReadListEnd()
	  {
		int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		if (code != 'Z')
		{
		  throw error("expected end of list ('Z') at '" + codeName(code) + "'");
		}
	  }

///  
///   <summary> * Adds a list/map reference. </summary>
///   
	  public override int AddRef(object @ref)
	  {
		if (_refs == null)
		{
		  _refs = new ArrayList();
		}

		_refs.Add(@ref);

		return _refs.Count - 1;
	  }

///  
///   <summary> * Adds a list/map reference. </summary>
///   
	  public override void SetRef(int i, object @ref)
	  {
		_refs[i] = @ref;
	  }

///  
///   <summary> * Resets the references for streaming. </summary>
///   
	  public override void ResetReferences()
	  {
		_refs.Clear();
	  }

	  public virtual void Reset()
	  {
		ResetReferences();

		_classDefs.Clear();
		_types.Clear();
	  }

	  public virtual void ResetBuffer()
	  {
		int offset = _offset;
		_offset = 0;

		int length = _length;
		_length = 0;

		if (length > 0 && offset != length)
		{
		  throw new IllegalStateException("offset=" + offset + " length=" + length);
		}
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object ReadStreamingObject() throws IOException
	  public virtual object ReadStreamingObject()
	  {
		if (_refs != null)
		{
		  _refs.Clear();
		}

		return ReadObject();
	  }

///  
///   <summary> * Resolves a remote object. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object resolveRemote(String type, String url) throws IOException
	  public virtual object resolveRemote(string type, string url)
	  {
		/*HessianRemoteResolver resolver = RemoteResolver;

		if (resolver != null)
		{
		  return resolver.lookup(type, url);
		}
		else
		{
		  return new HessianRemote(type, url);
		}*/
         throw new NotImplementedException("resolveRemote");
	  }

///  
///   <summary> * Parses a type from the stream.
///   *
///   * <pre>
///   * type ::= string
///   * type ::= int
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public String ReadType() throws IOException
	  public override string ReadType()
	  {
		int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();
		_offset--;

		switch (code)
		{
		case 0x00:
			case 0x01:
				case 0x02:
					case 0x03:
		case 0x04:
			case 0x05:
				case 0x06:
					case 0x07:
		case 0x08:
			case 0x09:
				case 0x0a:
					case 0x0b:
		case 0x0c:
			case 0x0d:
				case 0x0e:
					case 0x0f:

		case 0x10:
			case 0x11:
				case 0x12:
					case 0x13:
		case 0x14:
			case 0x15:
				case 0x16:
					case 0x17:
		case 0x18:
			case 0x19:
				case 0x1a:
					case 0x1b:
		case 0x1c:
			case 0x1d:
				case 0x1e:
					case 0x1f:

		case 0x30:
			case 0x31:
				case 0x32:
					case 0x33:
		case Hessian2Constants.BC_STRING_CHUNK:
			case 'S':
		  {
		string type = ReadString();

		if (_types == null)
		{
		  _types = new ArrayList();
		}

		_types.Add(type);

		return type;
		  }

		default:
		  {
		int @ref = ReadInt();

		if (_types.Count <= @ref)
		{
		  throw new IndexOutOfRangeException("type ref #" + @ref + " is greater than the number of valid types (" + _types.Count + ")");
		}

		return (string) _types[@ref];
		  }
		}
	  }

///  
///   <summary> * Parses the length for an array
///   *
///   * <pre>
///   * l b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int ReadLength() throws IOException
	  public override int ReadLength()
	  {
		throw new UnsupportedOperationException();
	  }

///  
///   <summary> * Parses a 32-bit integer value from the stream.
///   *
///   * <pre>
///   * b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int parseInt() throws IOException
	  private int parseInt()
	  {
		int offset = _offset;

		if (offset + 3 < _length)
		{
		  byte[] buffer = _buffer;

		  int b32 = buffer[offset + 0] & 0xff;
		  int b24 = buffer[offset + 1] & 0xff;
		  int b16 = buffer[offset + 2] & 0xff;
		  int b8 = buffer[offset + 3] & 0xff;

		  _offset = offset + 4;

		  return (b32 << 24) + (b24 << 16) + (b16 << 8) + b8;
		}
		else
		{
		  int b32 = Read();
		  int b24 = Read();
		  int b16 = Read();
		  int b8 = Read();

		  return (b32 << 24) + (b24 << 16) + (b16 << 8) + b8;
		}
	  }

///  
///   <summary> * Parses a 64-bit long value from the stream.
///   *
///   * <pre>
///   * b64 b56 b48 b40 b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private long parseLong() throws IOException
	  private long parseLong()
	  {
		long b64 = Read();
		long b56 = Read();
		long b48 = Read();
		long b40 = Read();
		long b32 = Read();
		long b24 = Read();
		long b16 = Read();
		long b8 = Read();

		return ((b64 << 56) + (b56 << 48) + (b48 << 40) + (b40 << 32) + (b32 << 24) + (b24 << 16) + (b16 << 8) + b8);
	  }

///  
///   <summary> * Parses a 64-bit double value from the stream.
///   *
///   * <pre>
///   * b64 b56 b48 b40 b32 b24 b16 b8
///   * </pre> </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private double parseDouble() throws IOException
	  private double parseDouble()
	  {
		/*long bits = parseLong();
          BitConverter.ToDouble()
		return double.longBitsToDouble(bits);*/
          throw new NotImplementedException("Hessian2Input.parseDouble");
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: org.w3c.dom.Node parseXML() throws IOException
	  /*internal virtual org.w3c.dom.Node parseXML()
	  {
		throw new UnsupportedOperationException();
	  }*/

///  
///   <summary> * Reads a character from the underlying stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int parseChar() throws IOException
	  private int parseChar()
	  {
		while (_chunkLength <= 0)
		{
		  if (_isLastChunk)
		  {
			return -1;
		  }

		  int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		  switch (code)
		  {
		  case Hessian2Constants.BC_STRING_CHUNK:
			_isLastChunk = false;

			_chunkLength = (Read() << 8) + Read();
			break;

		  case 'S':
			_isLastChunk = true;

			_chunkLength = (Read() << 8) + Read();
			break;

		  case 0x00:
			  case 0x01:
				  case 0x02:
					  case 0x03:
		  case 0x04:
			  case 0x05:
				  case 0x06:
					  case 0x07:
		  case 0x08:
			  case 0x09:
				  case 0x0a:
					  case 0x0b:
		  case 0x0c:
			  case 0x0d:
				  case 0x0e:
					  case 0x0f:

		  case 0x10:
			  case 0x11:
				  case 0x12:
					  case 0x13:
		  case 0x14:
			  case 0x15:
				  case 0x16:
					  case 0x17:
		  case 0x18:
			  case 0x19:
				  case 0x1a:
					  case 0x1b:
		  case 0x1c:
			  case 0x1d:
				  case 0x1e:
					  case 0x1f:
		_isLastChunk = true;
		_chunkLength = code - 0x00;
		break;

		  case 0x30:
			  case 0x31:
				  case 0x32:
					  case 0x33:
		_isLastChunk = true;
		_chunkLength = (code - 0x30) * 256 + Read();
		break;

		  default:
			throw expect("string", code);
		  }

		}

		_chunkLength--;

		return parseUTF8Char();
	  }

///  
///   <summary> * Parses a single UTF8 character. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int parseUTF8Char() throws IOException
	  private int parseUTF8Char()
	  {
		int ch = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

		if (ch < 0x80)
		{
		  return ch;
		}
    switch ((ch & 0xe0))
    {
        case 0xc0:
            {
                int ch1 = Read();
                int v = ((ch & 0x1f) << 6) + (ch1 & 0x3f);

                return v;
            }
        default:
            if ((ch & 0xf0) == 0xe0)
            {
                int ch1 = Read();
                int ch2 = Read();
                int v = ((ch & 0x0f) << 12) + ((ch1 & 0x3f) << 6) + (ch2 & 0x3f);

                return v;
            }
            throw error("bad utf-8 encoding at " + codeName(ch));
    }
	  }

///  
///   <summary> * Reads a byte from the underlying stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int parseByte() throws IOException
	  private int parseByte()
	  {
		while (_chunkLength <= 0)
		{
		  if (_isLastChunk)
		  {
			return -1;
		  }

		  int code = Read();

		  switch (code)
		  {
		  case Hessian2Constants.BC_BINARY_CHUNK:
			_isLastChunk = false;

			_chunkLength = (Read() << 8) + Read();
			break;

		  case 'B':
			_isLastChunk = true;

			_chunkLength = (Read() << 8) + Read();
			break;

		  case 0x20:
			  case 0x21:
				  case 0x22:
					  case 0x23:
		  case 0x24:
			  case 0x25:
				  case 0x26:
					  case 0x27:
		  case 0x28:
			  case 0x29:
				  case 0x2a:
					  case 0x2b:
		  case 0x2c:
			  case 0x2d:
				  case 0x2e:
					  case 0x2f:
			_isLastChunk = true;

			_chunkLength = code - 0x20;
			break;

		  case 0x34:
			  case 0x35:
				  case 0x36:
					  case 0x37:
		_isLastChunk = true;
			_chunkLength = (code - 0x34) * 256 + Read();
			break;

		  default:
			throw expect("byte[]", code);
		  }
		}

		_chunkLength--;

		return Read();
	  }

///  
///   <summary> * Reads bytes based on an input stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public InputStream ReadInputStream() throws IOException
	  public override Stream ReadInputStream()
	  {
		int tag = Read();

		switch (tag)
		{
		case 'N':
		  return null;

		case Hessian2Constants.BC_BINARY:
		case Hessian2Constants.BC_BINARY_CHUNK:
		  _isLastChunk = tag == Hessian2Constants.BC_BINARY;
		  _chunkLength = (Read() << 8) + Read();
		  break;

		case 0x20:
			case 0x21:
				case 0x22:
					case 0x23:
		case 0x24:
			case 0x25:
				case 0x26:
					case 0x27:
		case 0x28:
			case 0x29:
				case 0x2a:
					case 0x2b:
		case 0x2c:
			case 0x2d:
				case 0x2e:
					case 0x2f:
		  _isLastChunk = true;
		  _chunkLength = tag - 0x20;
		  break;

		case 0x34:
			case 0x35:
				case 0x36:
					case 0x37:
		  _isLastChunk = true;
		  _chunkLength = (tag - 0x34) * 256 + Read();
		  break;

		default:
		  throw expect("binary", tag);
		}

		return new Read_InputStream(this);
	  }

///  
///   <summary> * Reads bytes from the underlying stream. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: int Read(byte [] buffer, int offset, int length) throws IOException
	  internal virtual int Read(byte[] buffer, int offset, int length)
	  {
		int ReadLength = 0;

		while (length > 0)
		{
		  while (_chunkLength <= 0)
		  {
			if (_isLastChunk)
			{
			  return ReadLength == 0 ? -1 : ReadLength;
			}

			int code = Read();

			switch (code)
			{
			case Hessian2Constants.BC_BINARY_CHUNK:
			  _isLastChunk = false;

			  _chunkLength = (Read() << 8) + Read();
			  break;

			case Hessian2Constants.BC_BINARY:
			  _isLastChunk = true;

			  _chunkLength = (Read() << 8) + Read();
			  break;

		case 0x20:
			case 0x21:
				case 0x22:
					case 0x23:
		case 0x24:
			case 0x25:
				case 0x26:
					case 0x27:
		case 0x28:
			case 0x29:
				case 0x2a:
					case 0x2b:
		case 0x2c:
			case 0x2d:
				case 0x2e:
					case 0x2f:
		  _isLastChunk = true;
		  _chunkLength = code - 0x20;
		  break;

		case 0x34:
			case 0x35:
				case 0x36:
					case 0x37:
		  _isLastChunk = true;
		  _chunkLength = (code - 0x34) * 256 + Read();
		  break;

			default:
			  throw expect("byte[]", code);
			}
		  }

		  int sublen = _chunkLength;
		  if (length < sublen)
		  {
			sublen = length;
		  }

		  if (_length <= _offset && ! ReadBuffer())
		  {
		return -1;
		  }

		  if (_length - _offset < sublen)
		  {
		sublen = _length - _offset;
		  }

		  Array.Copy(_buffer, _offset, buffer, offset, sublen);

		  _offset += sublen;

		  offset += sublen;
		  ReadLength += sublen;
		  length -= sublen;
		  _chunkLength -= sublen;
		}

		return ReadLength;
	  }

///  
///   <summary> * Normally, shouldn't be called externally, but needed for QA, e.g.
///   * ejb/3b01. </summary>
///   
//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final int Read() throws IOException
	  public int Read()
	  {
		if (_length <= _offset && ! ReadBuffer())
		{
		  return -1;
		}

		return _buffer[_offset++] & 0xff;
	  }

	  protected internal virtual void unRead()
	  {
		if (_offset <= 0)
		{
		  throw new IllegalStateException();
		}

		_offset--;
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private final boolean ReadBuffer() throws IOException
	  private bool ReadBuffer()
	  {
		byte[] buffer = _buffer;
		int offset = _offset;
		int length = _length;

		if (offset < length)
		{
		  Array.Copy(buffer, offset, buffer, 0, length - offset);
		  offset = length - offset;
		}
		else
		{
		  offset = 0;
		}

		int len = _is.Read(buffer, offset, SIZE - offset);

		if (len <= 0)
		{
		  _length = offset;
		  _offset = 0;

		  return offset > 0;
		}

		_length = offset + len;
		_offset = 0;

		return true;
	  }

	  public virtual Reader GetReader()
	  {
	      throw new NotImplementedException();
		  //return null;
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected IOException expect(String expect, int ch) throws IOException
	  protected internal virtual HessianException expect(string expect, int ch)
	  {
		if (ch < 0)
		{
		  return error("expected " + expect + " at end of file");
		}
		else
		{
		  _offset--;

		  try
		  {
		int offset = _offset;
		string context = buildDebugContext(_buffer, 0, _length, offset);

		object obj = ReadObject();

		if (obj != null)
		{
		  return error("expected " + expect + " at 0x" + (ch & 0xff).ToString("X") + " " + obj.GetType().Name + " (" + obj + ")" + "\n  " + context + "");
		}
		      return error("expected " + expect + " at 0x" + (ch & 0xff).ToString("X") + " null");
		  }
		  catch (Exception e)
		  {
		    //log.log(Level.FINE, e.ToString(), e);

		return error("expected " + expect + " at 0x" + (ch & 0xff).ToString("X"));
		  }
		}
	  }

	  private string buildDebugContext(byte[] buffer, int offset, int length, int errorOffset)
	  {
		StringBuilder sb = new StringBuilder();

		sb.Append("[");
		for (int i = 0; i < errorOffset; i++)
		{
		  int ch = buffer[offset + i];
		  addDebugChar(sb, ch);
		}
		sb.Append("] ");
		addDebugChar(sb, buffer[offset + errorOffset]);
		sb.Append(" [");
		for (int i = errorOffset + 1; i < length; i++)
		{
		  int ch = buffer[offset + i];
		  addDebugChar(sb, ch);
		}
		sb.Append("]");

		return sb.ToString();
	  }

	  private void addDebugChar(StringBuilder sb, int ch)
	  {
		if (ch >= 0x20 && ch < 0x7f)
		{
		  sb.Append((char) ch);
		}
		else if (ch == '\n')
		{
		  sb.Append((char) ch);
		}
		else
		{
		  sb.Append(string.Format("\\x{0:x2}", ch & 0xff));
		}
	  }

	  protected internal virtual string codeName(int ch)
	  {
		if (ch < 0)
		{
		  return "end of file";
		}
		else
		{
		  return "0x" + (ch & 0xff).ToString("X") + " (" + (char) + ch + ")";
		}
	  }

	  protected internal virtual HessianException error(string message)
	  {
		if (_method != null)
		{
		  return new HessianException(_method + ": " + message);
		}
		else
		{
		  return new HessianException(message);
		}
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void close() throws IOException
	  public override void Close()
	  {
		Stream @is = _is;
		_is = null;

		if (_isCloseStreamOnClose && @is != null)
		{
		  @is.Dispose();
		}
	  }

	  internal class Read_InputStream : Stream
	  {
	      private readonly Hessian2Input input;

	      public Read_InputStream(Hessian2Input input)
	      {
	          this.input = input;
	      }

	      internal bool _isClosed = false;

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int Read() throws IOException
		public override int ReadByte()
		{
		  if (_isClosed)
		  {
		return -1;
		  }

		  int ch = input.parseByte();
		  if (ch < 0)
		  {
		_isClosed = true;
		  }

		  return ch;
		}

	      public override void Write(byte[] buffer, int offset, int count)
	      {
	          throw new System.NotImplementedException();
	      }

	      public override bool CanRead
	      {
	          get { throw new System.NotImplementedException(); }
	      }

	      public override bool CanSeek
	      {
	          get { throw new System.NotImplementedException(); }
	      }

	      public override bool CanWrite
	      {
	          get { throw new System.NotImplementedException(); }
	      }

	      public override long Length
	      {
	          get { throw new System.NotImplementedException(); }
	      }

	      public override long Position
	      {
	          get { throw new System.NotImplementedException(); }
	          set { throw new System.NotImplementedException(); }
	      }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int Read(byte [] buffer, int offset, int length) throws IOException
	      public override long Seek(long offset, SeekOrigin origin)
	      {
	          throw new System.NotImplementedException();
	      }

	      public override void SetLength(long value)
	      {
	          throw new System.NotImplementedException();
	      }

	      public override int Read(byte[] buffer, int offset, int length)
		{
		  if (_isClosed)
		  {
		return -1;
		  }

		  int len = input.Read(buffer, offset, length);
		  if (len < 0)
		  {
		_isClosed = true;
		  }

		  return len;
		}

	      protected override void Dispose(bool disposing)
	      {
               while (ReadByte() >= 0)
		  {
		  }
	          base.Dispose(disposing);
	      }

	      public override void Flush()
	      {
	          throw new System.NotImplementedException();
	      }
	  }

	  internal sealed class ObjectDefinition
	  {
		private readonly string _type;
		private readonly AbstractDeserializer _Reader;
		private readonly object[] _fields;
		private readonly string[] _fieldNames;

		internal ObjectDefinition(string type, AbstractDeserializer Reader, object[] fields, string[] fieldNames)
		{
		  _type = type;
		  _Reader = Reader;
		  _fields = fields;
		  _fieldNames = fieldNames;
		}

		internal string Type
		{
			get
			{
			  return _type;
			}
		}

		internal AbstractDeserializer Reader
		{
			get
			{
			  return _Reader;
			}
		}

		internal object [] Fields
		{
			get
			{
			  return _fields;
			}
		}

		internal string [] FieldNames
		{
			get
			{
			  return _fieldNames;
			}
		}
	  }

	  static Hessian2Input()
	  {
		/*try
		{
		  _detailMessageField = typeof(Exception).getDeclaredField("detailMessage");
		  _detailMessageField.Accessible = true;
		}
		catch (Exception e)
		{
		}*/
	  }
	}

}