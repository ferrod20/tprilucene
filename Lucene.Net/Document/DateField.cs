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
using PrefixQuery = Lucene.Net.Search.PrefixQuery;
using RangeQuery = Lucene.Net.Search.RangeQuery;

namespace Lucene.Net.Documents
{
    // for javadoc

    /// <summary> Provides support for converting dates to strings and vice-versa.
    /// The strings are structured so that lexicographic sorting orders by date,
    /// which makes them suitable for use as field values and search terms.
    /// 
    /// <P>Note that this class saves dates with millisecond granularity,
    /// which is bad for {@link RangeQuery} and {@link PrefixQuery}, as those
    /// queries are expanded to a BooleanQuery with a potentially large number
    /// of terms when searching. Thus you might want to use
    /// {@link DateTools} instead.
    /// 
    /// <P>
    /// Note: dates before 1970 cannot be used, and therefore cannot be
    /// indexed when using this class. See {@link DateTools} for an
    /// alternative without such a limitation.
    /// 
    /// </summary>
    /// <deprecated> If you build a new index, use {@link DateTools} instead. This class is included for use with existing
    /// indices and will be removed in a future release.
    /// </deprecated>
    public class DateField
    {
		
        private DateField()
        {
        }
		
        // make date strings long enough to last a millenium
        private static int DATE_LEN = SupportClass.Number.ToString(
            1000L * 365 * 24 * 60 * 60 * 1000, 16).Length;
		
        public static System.String MIN_DATE_STRING()
        {
            return TimeToString(0);
        }
		
        public static System.String MAX_DATE_STRING()
        {
            char[] buffer = new char[DATE_LEN];
            char c = SupportClass.Character.ForDigit(36 - 1, SupportClass.Character.MAX_RADIX);
            for (int i = 0; i < DATE_LEN; i++)
                buffer[i] = c;
            return new System.String(buffer);
        }
		
        /// <summary> Converts a Date to a string suitable for indexing.</summary>
        /// <throws>  SystemException if the date specified in the </throws>
        /// <summary> method argument is before 1970
        /// </summary>
        public static System.String DateToString(System.DateTime date)
        {
            TimeSpan ts = date.Subtract(new DateTime(1970, 1, 1));
            ts = ts.Subtract(TimeZone.CurrentTimeZone.GetUtcOffset(date));
            return TimeToString(ts.Ticks / TimeSpan.TicksPerMillisecond);
        }

        /// <summary> Converts a millisecond time to a string suitable for indexing.</summary>
        /// <throws>  SystemException if the time specified in the </throws>
        /// <summary> method argument is negative, that is, before 1970
        /// </summary>
        public static System.String TimeToString(long time)
        {
            if (time < 0)
                throw new System.SystemException("time '" + time + "' is too early, must be >= 0");
			
            System.String s = SupportClass.Number.ToString(time, SupportClass.Number.MAX_RADIX);
			
            if (s.Length > DATE_LEN)
                throw new System.SystemException("time '" + time + "' is too late, length of string " + "representation must be <= " + DATE_LEN);
			
            // Pad with leading zeros
            if (s.Length < DATE_LEN)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(s);
                while (sb.Length < DATE_LEN)
                    sb.Insert(0, 0);
                s = sb.ToString();
            }
			
            return s;
        }
		
        /// <summary>Converts a string-encoded date into a millisecond time. </summary>
        public static long StringToTime(System.String s)
        {
            return SupportClass.Number.Parse(s, SupportClass.Number.MAX_RADIX);
        }
		
        /// <summary>Converts a string-encoded date into a Date object. </summary>
        public static System.DateTime StringToDate(System.String s)
        {
            long ticks = StringToTime(s) * TimeSpan.TicksPerMillisecond;
            System.DateTime date = new System.DateTime(1970, 1, 1);
            date = date.AddTicks(ticks);
            date = date.Add(TimeZone.CurrentTimeZone.GetUtcOffset(date));
            return date;
        }
    }
}