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
	
	/// <summary> A query that wraps a filter and simply returns a constant score equal to the
	/// query boost for every document in the filter.
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: ConstantScoreQuery.java 564236 2007-08-09 15:21:19Z gsingers $
	/// </version>
	[Serializable]
	public class ConstantScoreQuery : Query
	{
		protected internal Filter filter;
		
		public ConstantScoreQuery(Filter filter)
		{
			this.filter = filter;
		}
		
		/// <summary>Returns the encapsulated filter </summary>
		public virtual Filter GetFilter()
		{
			return filter;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			return this;
		}
		
		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
			// OK to not add any terms when used for MultiSearcher,
			// but may not be OK for highlighting
		}
		
		[Serializable]
		protected internal class ConstantWeight : Weight
		{
			private void  InitBlock(ConstantScoreQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ConstantScoreQuery enclosingInstance;
			public ConstantScoreQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Similarity similarity;
			private float queryNorm;
			private float queryWeight;
			
			public ConstantWeight(ConstantScoreQuery enclosingInstance, Searcher searcher)
			{
				InitBlock(enclosingInstance);
				this.similarity = Enclosing_Instance.GetSimilarity(searcher);
			}
			
			public virtual Query GetQuery()
			{
				return Enclosing_Instance;
			}
			
			public virtual float GetValue()
			{
				return queryWeight;
			}
			
			public virtual float SumOfSquaredWeights()
			{
				queryWeight = Enclosing_Instance.GetBoost();
				return queryWeight * queryWeight;
			}
			
			public virtual void  Normalize(float norm)
			{
				this.queryNorm = norm;
				queryWeight *= this.queryNorm;
			}
			
			public virtual Scorer Scorer(IndexReader reader)
			{
				return new ConstantScorer(enclosingInstance, similarity, reader, this);
			}
			
			public virtual Explanation Explain(IndexReader reader, int doc)
			{
				
				ConstantScorer cs = (ConstantScorer) Scorer(reader);
				bool exists = cs.bits.Get(doc);
				
				ComplexExplanation result = new ComplexExplanation();
				
				if (exists)
				{
					result.SetDescription("ConstantScoreQuery(" + Enclosing_Instance.filter + "), product of:");
					result.SetValue(queryWeight);
					System.Boolean tempAux = true;
					result.SetMatch(tempAux);
					result.AddDetail(new Explanation(Enclosing_Instance.GetBoost(), "boost"));
					result.AddDetail(new Explanation(queryNorm, "queryNorm"));
				}
				else
				{
					result.SetDescription("ConstantScoreQuery(" + Enclosing_Instance.filter + ") doesn't match id " + doc);
					result.SetValue(0);
					System.Boolean tempAux2 = false;
					result.SetMatch(tempAux2);
				}
				return result;
			}
		}
		
		protected internal class ConstantScorer : Scorer
		{
			private void  InitBlock(ConstantScoreQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ConstantScoreQuery enclosingInstance;
			public ConstantScoreQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal System.Collections.BitArray bits;
			internal float theScore;
			internal int doc = - 1;
			
			public ConstantScorer(ConstantScoreQuery enclosingInstance, Similarity similarity, IndexReader reader, Weight w):base(similarity)
			{
				InitBlock(enclosingInstance);
				theScore = w.GetValue();
				bits = Enclosing_Instance.filter.Bits(reader);
			}
			
			public override bool Next()
			{
				doc = SupportClass.Number.NextSetBit(bits, doc + 1);
				return doc >= 0;
			}
			
			public override int Doc()
			{
				return doc;
			}
			
			public override float Score()
			{
				return theScore;
			}
			
			public override bool SkipTo(int target)
			{
				doc = SupportClass.Number.NextSetBit(bits, target); // requires JDK 1.4
				return doc >= 0;
			}
			
			public override Explanation Explain(int doc)
			{
				throw new System.NotSupportedException();
			}
		}
		
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new ConstantScoreQuery.ConstantWeight(this, searcher);
		}
		
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString(System.String field)
		{
			return "ConstantScore(" + filter.ToString() + (GetBoost() == 1.0 ? ")" : "^" + GetBoost());
		}
		
		/// <summary>Returns true if <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (this == o)
				return true;
			if (!(o is ConstantScoreQuery))
				return false;
			ConstantScoreQuery other = (ConstantScoreQuery) o;
			return this.GetBoost() == other.GetBoost() && filter.Equals(other.filter);
		}
		
		/// <summary>Returns a hash code value for this object. </summary>
		public override int GetHashCode()
		{
			// Simple add is OK since no existing filter hashcode has a float component.
			return filter.GetHashCode() + BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0);
		}

		override public System.Object Clone()
		{
            // {{Aroush-1.9}} is this all that we need to clone?!
            ConstantScoreQuery clone = (ConstantScoreQuery) base.Clone();
            clone.filter = (Filter) this.filter;
            return clone;
        }
	}
}