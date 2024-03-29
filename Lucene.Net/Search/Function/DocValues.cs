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

using Explanation = Lucene.Net.Search.Explanation;

namespace Lucene.Net.Search.Function
{
	
	/// <summary> Expert: represents field values as different types.
	/// Normally created via a 
	/// {@link Lucene.Net.Search.Function.ValueSource ValueSuorce} 
	/// for a particular field and reader.
	/// 
	/// <p><font color="#FF0000">
	/// WARNING: The status of the <b>search.function</b> package is experimental. 
	/// The APIs introduced here might change in the future and will not be 
	/// supported anymore in such a case.</font>
	/// 
	/// 
	/// </summary>
	public abstract class DocValues
	{
		/*
		* DocValues is distinct from ValueSource because
		* there needs to be an object created at query evaluation time that
		* is not referenced by the query itself because:
		* - Query objects should be MT safe
		* - For caching, Query objects are often used as keys... you don't
		*   want the Query carrying around big objects
		*/
		
		/// <summary> Return doc value as a float. 
		/// <P>Mandatory: every DocValues implementation must implement at least this method. 
		/// </summary>
		/// <param name="doc">document whose float value is requested. 
		/// </param>
		public abstract float FloatVal(int doc);
		
		/// <summary> Return doc value as an int. 
		/// <P>Optional: DocValues implementation can (but don't have to) override this method. 
		/// </summary>
		/// <param name="doc">document whose int value is requested.
		/// </param>
		public virtual int IntVal(int doc)
		{
			return (int) FloatVal(doc);
		}
		
		/// <summary> Return doc value as a long. 
		/// <P>Optional: DocValues implementation can (but don't have to) override this method. 
		/// </summary>
		/// <param name="doc">document whose long value is requested.
		/// </param>
		public virtual long LongVal(int doc)
		{
			return (long) FloatVal(doc);
		}
		
		/// <summary> Return doc value as a double. 
		/// <P>Optional: DocValues implementation can (but don't have to) override this method. 
		/// </summary>
		/// <param name="doc">document whose double value is requested.
		/// </param>
		public virtual double DoubleVal(int doc)
		{
			return (double) FloatVal(doc);
		}
		
		/// <summary> Return doc value as a string. 
		/// <P>Optional: DocValues implementation can (but don't have to) override this method. 
		/// </summary>
		/// <param name="doc">document whose string value is requested.
		/// </param>
		public virtual System.String StrVal(int doc)
		{
			return FloatVal(doc).ToString();
		}
		
		/// <summary> Return a string representation of a doc value, as reuired for Explanations.</summary>
		public abstract System.String ToString(int doc);
		
		/// <summary> Explain the scoring value for the input doc.</summary>
		public virtual Explanation Explain(int doc)
		{
			return new Explanation(FloatVal(doc), ToString(doc));
		}
		
		/// <summary> Expert: for test purposes only, return the inner array of values, or null if not applicable.
		/// <p>
		/// Allows tests to verify that loaded values are:
		/// <ol>
		/// <li>indeed cached/reused.</li>
		/// <li>stored in the expected size/type (byte/short/int/float).</li>
		/// </ol>
		/// Note: implementations of DocValues must override this method for 
		/// these test elements to be tested, Otherwise the test would not fail, just 
		/// print a warning.
		/// </summary>
		public /*internal*/ virtual System.Object GetInnerArray()
		{
			throw new System.NotSupportedException("this optional method is for test purposes only");
		}
		
		// --- some simple statistics on values
		private float minVal;
		private float maxVal;
		private float avgVal;
		private bool computed = false;
		// compute optional values
		private void  Compute()
		{
			if (computed)
			{
				return ;
			}
			minVal = System.Single.MaxValue;
			maxVal = 0;
			float sum = 0;
			int n = 0;
			while (true)
			{
				float val;
				try
				{
					val = FloatVal(n);
				}
				catch (System.IndexOutOfRangeException e)
				{
					break;
				}
				sum += val;
				minVal = System.Math.Min(minVal, val);
				maxVal = System.Math.Max(maxVal, val);
			}
			avgVal = sum / n;
			computed = true;
		}
		/// <summary> Optional op.
		/// Returns the minimum of all values.
		/// </summary>
		public virtual float GetMinValue()
		{
			Compute();
			return minVal;
		}
		
		/// <summary> Optional op.
		/// Returns the maximum of all values. 
		/// </summary>
		public virtual float GetMaxValue()
		{
			Compute();
			return maxVal;
		}
		
		/// <summary> Returns the average of all values. </summary>
		public virtual float GetAverageValue()
		{
			Compute();
			return avgVal;
		}
	}
}