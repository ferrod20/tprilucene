/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;

using IndexReader = Lucene.Net.Index.IndexReader;
using FieldCache = Lucene.Net.Search.FieldCache;

namespace Lucene.Net.Search.Function
{
	
	/// <summary> Expert: obtains short field values from the 
	/// {@link Lucene.Net.Search.FieldCache FieldCache}
	/// using <code>getShorts()</code> and makes those values 
	/// available as other numeric types, casting as needed.
	/// 
	/// <p><font color="#FF0000">
	/// WARNING: The status of the <b>search.function</b> package is experimental. 
	/// The APIs introduced here might change in the future and will not be 
	/// supported anymore in such a case.</font>
	/// 
	/// </summary>
	/// <seealso cref="Lucene.Net.Search.Function.FieldCacheSource for requirements">
	/// on the field.
	/// </seealso>
	[Serializable]
	public class ShortFieldSource : FieldCacheSource
	{
		private class AnonymousClassDocValues : DocValues
		{
			public AnonymousClassDocValues(short[] arr, ShortFieldSource enclosingInstance)
			{
				InitBlock(arr, enclosingInstance);
			}
			private void  InitBlock(short[] arr, ShortFieldSource enclosingInstance)
			{
				this.arr = arr;
				this.enclosingInstance = enclosingInstance;
			}
			private short[] arr;
			private ShortFieldSource enclosingInstance;
			public ShortFieldSource Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#floatVal(int) */
			public override float FloatVal(int doc)
			{
				return (float) arr[doc];
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#intVal(int) */
			public override int IntVal(int doc)
			{
				return arr[doc];
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#toString(int) */
			public override System.String ToString(int doc)
			{
				return Enclosing_Instance.Description() + '=' + IntVal(doc);
			}
			/*(non-Javadoc) @see Lucene.Net.Search.Function.DocValues#getInnerArray() */
			public /*internal*/ override System.Object GetInnerArray()
			{
				return arr;
			}
		}
		private Lucene.Net.Search.ShortParser parser;
		
		/// <summary> Create a cached short field source with default string-to-short parser. </summary>
		public ShortFieldSource(System.String field) : this(field, null)
		{
		}
		
		/// <summary> Create a cached short field source with a specific string-to-short parser. </summary>
		public ShortFieldSource(System.String field, Lucene.Net.Search.ShortParser parser) : base(field)
		{
			this.parser = parser;
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Function.ValueSource#description() */
		public override System.String Description()
		{
			return "short(" + base.Description() + ')';
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Function.FieldCacheSource#getCachedValues(Lucene.Net.Search.FieldCache, java.lang.String, Lucene.Net.Index.IndexReader) */
		public override DocValues GetCachedFieldValues(FieldCache cache, System.String field, IndexReader reader)
		{
			short[] arr = (parser == null) ? cache.GetShorts(reader, field) : cache.GetShorts(reader, field, parser);
			return new AnonymousClassDocValues(arr, this);
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Function.FieldCacheSource#cachedFieldSourceEquals(Lucene.Net.Search.Function.FieldCacheSource) */
		public override bool CachedFieldSourceEquals(FieldCacheSource o)
		{
			if (o.GetType() != typeof(ShortFieldSource))
			{
				return false;
			}
			ShortFieldSource other = (ShortFieldSource) o;
			return this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType();
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Function.FieldCacheSource#cachedFieldSourceHashCode() */
		public override int CachedFieldSourceHashCode()
		{
			return parser == null ? typeof(System.Int16).GetHashCode() : parser.GetType().GetHashCode();
		}
	}
}