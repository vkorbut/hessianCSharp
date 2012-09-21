using System;
using System.Collections;
using hessiancsharp.io.hessian2;
using java.util;
using ArrayList=System.Collections.ArrayList;

/*
 * Copyright (c) 2001-2004 Caucho Technology, Inc.  All rights reserved.
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

namespace hessian.io.hessian2
{


///
/// <summary> * Deserializing a JDK 1.2 Collection. </summary>
/// 
	public class CollectionDeserializer : AbstractListDeserializer
	{
	  private Type _type;

	  public CollectionDeserializer(Type type)
	  {
		_type = type;
	  }

	  public override Type GetOwnType()
	  {
			return _type;
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object readList(AbstractHessianInput in, int length) throws IOException
	  public override object ReadList(AbstractHessianInput @in, int length)
	  {
		IList list = createList();

		@in.AddRef(list);

		while (! @in.IsEnd())
		{
		  list.Add(@in.ReadObject());
		}

		@in.ReadEnd();

		return list;
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object readLengthList(AbstractHessianInput in, int length) throws IOException
	  public override object ReadLengthList(AbstractHessianInput @in, int length)
	  {
		IList list = createList();

		@in.AddRef(list);

		for (; length > 0; length--)
		{
		  list.Add(@in.ReadObject());
		}

		return list;
	  }

//JAVA TO VB & C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private Collection createList() throws IOException
	  private IList createList()
	  {
		IList list = null;

		if (_type == null)
		{
		  list = new ArrayList();
		}
		else if (! _type.IsInterface)
		{
		  try
		  {
		      list = (IList) Activator.CreateInstance(_type);
		  }
		  catch (Exception e)
		  {
		  }
		}

		if (list != null)
		{
		}
		else if (_type.IsSubclassOf(typeof(SortedSet)))
		{
		  list = (IList) new TreeSet();
		}
		else if (_type.IsSubclassOf(typeof(Set)))
		{
		  list = (IList) new HashSet();
		}
		else if (_type.IsSubclassOf(typeof(IList)))
		{
		  list = new ArrayList();
		}
		else if (_type.IsSubclassOf(typeof(ICollection)))
		{
		  list = new ArrayList();
		}
		else
		{
		  try
		  {
			list = (IList) Activator.CreateInstance(_type);
		  }
		  catch (Exception e)
		  {
			throw new HessianException(e);
		  }
		}

		return list;
	  }
	}



}