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

namespace Lucene.Net.Search
{
	
	/// <summary> Abstract base class for sorting hits returned by a Query.
	/// 
	/// <p>This class should only be used if the other SortField
	/// types (SCORE, DOC, STRING, INT, FLOAT) do not provide an
	/// adequate sorting.  It maintains an internal cache of values which
	/// could be quite large.  The cache is an array of Comparable,
	/// one for each document in the index.  There is a distinct
	/// Comparable for each unique term in the field - if
	/// some documents have the same term in the field, the cache
	/// array will have entries which reference the same Comparable.
	/// 
	/// <p>Created: Apr 21, 2004 5:08:38 PM
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: SortComparator.java 564236 2007-08-09 15:21:19Z gsingers $
	/// </version>
	/// <since>   1.4
	/// </since>
	[Serializable]
	public abstract class SortComparator : SortComparatorSource
	{
		private class AnonymousClassScoreDocComparator : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator(System.IComparable[] cachedValues, SortComparator enclosingInstance)
			{
				InitBlock(cachedValues, enclosingInstance);
			}
			private void  InitBlock(System.IComparable[] cachedValues, SortComparator enclosingInstance)
			{
				this.cachedValues = cachedValues;
				this.enclosingInstance = enclosingInstance;
			}
			private System.IComparable[] cachedValues;
			private SortComparator enclosingInstance;
			public SortComparator Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			public virtual int Compare(ScoreDoc i, ScoreDoc j)
			{
				return cachedValues[i.doc].CompareTo(cachedValues[j.doc]);
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return cachedValues[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.CUSTOM;
			}
		}
		
		// inherit javadocs
		public virtual ScoreDocComparator NewComparator(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			System.IComparable[] cachedValues = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetCustom(reader, field, this);
			
			return new AnonymousClassScoreDocComparator(cachedValues, this);
		}
		
		/// <summary> Returns an object which, when sorted according to natural order,
		/// will order the Term values in the correct order.
		/// <p>For example, if the Terms contained integer values, this method
		/// would return <code>new Integer(termtext)</code>.  Note that this
		/// might not always be the most efficient implementation - for this
		/// particular example, a better implementation might be to make a
		/// ScoreDocLookupComparator that uses an internal lookup table of int.
		/// </summary>
		/// <param name="termtext">The textual value of the term.
		/// </param>
		/// <returns> An object representing <code>termtext</code> that sorts according to the natural order of <code>termtext</code>.
		/// </returns>
		/// <seealso cref="Comparable">
		/// </seealso>
		/// <seealso cref="ScoreDocComparator">
		/// </seealso>
		public abstract System.IComparable GetComparable(System.String termtext);
	}
}