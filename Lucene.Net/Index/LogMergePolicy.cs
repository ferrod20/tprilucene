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

using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
	
	/// <summary><p>This class implements a {@link MergePolicy} that tries
	/// to merge segments into levels of exponentially
	/// increasing size, where each level has < mergeFactor
	/// segments in it.  Whenever a given levle has mergeFactor
	/// segments or more in it, they will be merged.</p>
	/// 
	/// <p>This class is abstract and requires a subclass to
	/// define the {@link #size} method which specifies how a
	/// segment's size is determined.  {@link LogDocMergePolicy}
	/// is one subclass that measures size by document count in
	/// the segment.  {@link LogByteSizeMergePolicy} is another
	/// subclass that measures size as the total byte size of the
	/// file(s) for the segment.</p>
	/// </summary>
	
	public abstract class LogMergePolicy : MergePolicy
	{
		
		/// <summary>Defines the allowed range of log(size) for each
		/// level.  A level is computed by taking the max segment
		/// log size, minuse LEVEL_LOG_SPAN, and finding all
		/// segments falling within that range. 
		/// </summary>
		public const double LEVEL_LOG_SPAN = 0.75;
		
		/// <summary>Default merge factor, which is how many segments are
		/// merged at a time 
		/// </summary>
		public const int DEFAULT_MERGE_FACTOR = 10;
		
		/// <summary>Default maximum segment size.  A segment of this size</summary>
		/// <seealso cref="setMaxMergeDocs">
		/// </seealso>
		public static readonly int DEFAULT_MAX_MERGE_DOCS = System.Int32.MaxValue;
		
		private int mergeFactor = DEFAULT_MERGE_FACTOR;
		
		internal long minMergeSize;
		internal long maxMergeSize;
		internal int maxMergeDocs = DEFAULT_MAX_MERGE_DOCS;
		
		private bool useCompoundFile = true;
		private bool useCompoundDocStore = true;
		private IndexWriter writer;
		
		private void  Message(System.String message)
		{
			if (writer != null)
				writer.Message("LMP: " + message);
		}
		
		/// <summary><p>Returns the number of segments that are merged at
		/// once and also controls the total number of segments
		/// allowed to accumulate in the index.</p> 
		/// </summary>
		public virtual int GetMergeFactor()
		{
			return mergeFactor;
		}
		
		/// <summary>Determines how often segment indices are merged by
		/// addDocument().  With smaller values, less RAM is used
		/// while indexing, and searches on unoptimized indices are
		/// faster, but indexing speed is slower.  With larger
		/// values, more RAM is used during indexing, and while
		/// searches on unoptimized indices are slower, indexing is
		/// faster.  Thus larger values (> 10) are best for batch
		/// index creation, and smaller values (< 10) for indices
		/// that are interactively maintained. 
		/// </summary>
		public virtual void  SetMergeFactor(int mergeFactor)
		{
			if (mergeFactor < 2)
				throw new System.ArgumentException("mergeFactor cannot be less than 2");
			this.mergeFactor = mergeFactor;
		}
		
		// Javadoc inherited
		public override bool UseCompoundFile(SegmentInfos infos, SegmentInfo info)
		{
			return useCompoundFile;
		}
		
		/// <summary>Sets whether compound file format should be used for
		/// newly flushed and newly merged segments. 
		/// </summary>
		public virtual void  SetUseCompoundFile(bool useCompoundFile)
		{
			this.useCompoundFile = useCompoundFile;
		}
		
		/// <summary>Returns true if newly flushed and newly merge segments</summary>
		/// <seealso cref="">
		/// #setUseCompoundFile 
		/// </seealso>
		public virtual bool GetUseCompoundFile()
		{
			return useCompoundFile;
		}
		
		// Javadoc inherited
		public override bool UseCompoundDocStore(SegmentInfos infos)
		{
			return useCompoundDocStore;
		}
		
		/// <summary>Sets whether compound file format should be used for
		/// newly flushed and newly merged doc store
		/// segment files (term vectors and stored fields). 
		/// </summary>
		public virtual void  SetUseCompoundDocStore(bool useCompoundDocStore)
		{
			this.useCompoundDocStore = useCompoundDocStore;
		}
		
		/// <summary>Returns true if newly flushed and newly merge doc
		/// store segment files (term vectors and stored fields)
		/// </summary>
		/// <seealso cref="">
		/// #setUseCompoundDocStore 
		/// </seealso>
		public virtual bool GetUseCompoundDocStore()
		{
			return useCompoundDocStore;
		}
		
		public override void  Close()
		{
		}
		
		abstract protected internal long Size(SegmentInfo info);
		
		private bool IsOptimized(SegmentInfos infos, IndexWriter writer, int maxNumSegments, System.Collections.Hashtable segmentsToOptimize)
		{
			int numSegments = infos.Count;
			int numToOptimize = 0;
			SegmentInfo optimizeInfo = null;
			for (int i = 0; i < numSegments && numToOptimize <= maxNumSegments; i++)
			{
				SegmentInfo info = infos.Info(i);
				if (segmentsToOptimize.Contains(info))
				{
					numToOptimize++;
					optimizeInfo = info;
				}
			}
			
			return numToOptimize <= maxNumSegments && (numToOptimize != 1 || IsOptimized(writer, optimizeInfo));
		}
		
		/// <summary>Returns true if this single nfo is optimized (has no
		/// pending norms or deletes, is in the same dir as the
		/// writer, and matches the current compound file setting 
		/// </summary>
		private bool IsOptimized(IndexWriter writer, SegmentInfo info)
		{
			return !info.HasDeletions() && !info.HasSeparateNorms() && info.dir == writer.GetDirectory() && info.GetUseCompoundFile() == useCompoundFile;
		}
		
		/// <summary>Returns the merges necessary to optimize the index.
		/// This merge policy defines "optimized" to mean only one
		/// segment in the index, where that segment has no
		/// deletions pending nor separate norms, and it is in
		/// compound file format if the current useCompoundFile
		/// setting is true.  This method returns multiple merges
		/// (mergeFactor at a time) so the {@link MergeScheduler}
		/// in use may make use of concurrency. 
		/// </summary>
		public override MergeSpecification FindMergesForOptimize(SegmentInfos infos, IndexWriter writer, int maxNumSegments, System.Collections.Hashtable segmentsToOptimize)
		{
			MergeSpecification spec;
			
			System.Diagnostics.Debug.Assert(maxNumSegments > 0);
			
			if (!IsOptimized(infos, writer, maxNumSegments, segmentsToOptimize))
			{
				
				// Find the newest (rightmost) segment that needs to
				// be optimized (other segments may have been flushed
				// since optimize started):
				int last = infos.Count;
				while (last > 0)
				{
					SegmentInfo info = infos.Info(--last);
					if (segmentsToOptimize.Contains(info))
					{
						last++;
						break;
					}
				}
				
				if (last > 0)
				{
					
					spec = new MergeSpecification();
					
					// First, enroll all "full" merges (size
					// mergeFactor) to potentially be run concurrently:
					while (last - maxNumSegments + 1 >= mergeFactor)
					{
						spec.Add(new OneMerge(infos.Range(last - mergeFactor, last), useCompoundFile));
						last -= mergeFactor;
					}
					
					// Only if there are no full merges pending do we
					// add a final partial (< mergeFactor segments) merge:
					if (0 == spec.merges.Count)
					{
						if (maxNumSegments == 1)
						{
							
							// Since we must optimize down to 1 segment, the
							// choice is simple:
							if (last > 1 || !IsOptimized(writer, infos.Info(0)))
								spec.Add(new OneMerge(infos.Range(0, last), useCompoundFile));
						}
						else if (last > maxNumSegments)
						{
							
							// Take care to pick a partial merge that is
							// least cost, but does not make the index too
							// lopsided.  If we always just picked the
							// partial tail then we could produce a highly
							// lopsided index over time:
							
							// We must merge this many segments to leave
							// maxNumSegments in the index (from when
							// optimize was first kicked off):
							int finalMergeSize = last - maxNumSegments + 1;
							
							// Consider all possible starting points:
							long bestSize = 0;
							int bestStart = 0;
							
							for (int i = 0; i < last - finalMergeSize + 1; i++)
							{
								long sumSize = 0;
								for (int j = 0; j < finalMergeSize; j++)
									sumSize += Size(infos.Info(j + i));
								if (i == 0 || (sumSize < 2 * Size(infos.Info(i - 1)) && sumSize < bestSize))
								{
									bestStart = i;
									bestSize = sumSize;
								}
							}
							
							spec.Add(new OneMerge(infos.Range(bestStart, bestStart + finalMergeSize), useCompoundFile));
						}
					}
				}
				else
					spec = null;
			}
			else
				spec = null;
			
			return spec;
		}
		
		/// <summary>Checks if any merges are now necessary and returns a
		/// {@link MergePolicy.MergeSpecification} if so.  A merge
		/// is necessary when there are more than {@link
		/// #setMergeFactor} segments at a given level.  When
		/// multiple levels have too many segments, this method
		/// will return multiple merges, allowing the {@link
		/// MergeScheduler} to use concurrency. 
		/// </summary>
		public override MergeSpecification FindMerges(SegmentInfos infos, IndexWriter writer)
		{
			
			int numSegments = infos.Count;
			this.writer = writer;
			Message("findMerges: " + numSegments + " segments");
			
			// Compute levels, which is just log (base mergeFactor)
			// of the size of each segment
			float[] levels = new float[numSegments];
			float norm = (float) System.Math.Log(mergeFactor);
			
			Directory directory = writer.GetDirectory();
			
			for (int i = 0; i < numSegments; i++)
			{
				SegmentInfo info = infos.Info(i);
				long size = Size(info);
				
				// Refuse to import a segment that's too large
				if (info.docCount > maxMergeDocs && info.dir != directory)
					throw new System.ArgumentException("Segment is too large (" + info.docCount + " docs vs max docs " + maxMergeDocs + ")");
				
				if (size >= maxMergeSize && info.dir != directory)
					throw new System.ArgumentException("Segment is too large (" + size + " vs max size " + maxMergeSize + ")");
				
				// Floor tiny segments
				if (size < 1)
					size = 1;
				levels[i] = (float) System.Math.Log(size) / norm;
			}
			
			float levelFloor;
			if (minMergeSize <= 0)
				levelFloor = (float) 0.0;
			else
			{
				levelFloor = (float) (System.Math.Log(minMergeSize) / norm);
			}
			
			// Now, we quantize the log values into levels.  The
			// first level is any segment whose log size is within
			// LEVEL_LOG_SPAN of the max size, or, who has such as
			// segment "to the right".  Then, we find the max of all
			// other segments and use that to define the next level
			// segment, etc.
			
			MergeSpecification spec = null;
			
			int start = 0;
			while (start < numSegments)
			{
				
				// Find max level of all segments not already
				// quantized.
				float maxLevel = levels[start];
				for (int i = 1 + start; i < numSegments; i++)
				{
					float level = levels[i];
					if (level > maxLevel)
						maxLevel = level;
				}
				
				// Now search backwards for the rightmost segment that
				// falls into this level:
				float levelBottom;
				if (maxLevel < levelFloor)
				// All remaining segments fall into the min level
					levelBottom = - 1.0F;
				else
				{
					levelBottom = (float) (maxLevel - LEVEL_LOG_SPAN);
					
					// Force a boundary at the level floor
					if (levelBottom < levelFloor && maxLevel >= levelFloor)
						levelBottom = levelFloor;
				}
				
				int upto = numSegments - 1;
				while (upto >= start)
				{
					if (levels[upto] >= levelBottom)
					{
						break;
					}
					upto--;
				}
				Message("  level " + levelBottom + " to " + maxLevel + ": " + (1 + upto - start) + " segments");
				
				// Finally, record all merges that are viable at this level:
				int end = start + mergeFactor;
				while (end <= 1 + upto)
				{
					bool anyTooLarge = false;
					for (int i = start; i < end; i++)
					{
						SegmentInfo info = infos.Info(i);
						anyTooLarge |= (Size(info) >= maxMergeSize || info.docCount >= maxMergeDocs);
					}
					
					if (!anyTooLarge)
					{
						if (spec == null)
							spec = new MergeSpecification();
						Message("    " + start + " to " + end + ": add this merge");
						spec.Add(new OneMerge(infos.Range(start, end), useCompoundFile));
					}
					else
						Message("    " + start + " to " + end + ": contains segment over maxMergeSize or maxMergeDocs; skipping");
					
					start = end;
					end = start + mergeFactor;
				}
				
				start = 1 + upto;
			}
			
			return spec;
		}
		
		/// <summary><p>Determines the largest segment (measured by
		/// document count) that may be merged with other segments.
		/// Small values (e.g., less than 10,000) are best for
		/// interactive indexing, as this limits the length of
		/// pauses while indexing to a few seconds.  Larger values
		/// are best for batched indexing and speedier
		/// searches.</p>
		/// 
		/// <p>The default value is {@link Integer#MAX_VALUE}.</p>
		/// 
		/// <p>The default merge policy ({@link
		/// LogByteSizeMergePolicy}) also allows you to set this
		/// limit by net size (in MB) of the segment, using {@link
		/// LogByteSizeMergePolicy#setMaxMergeMB}.</p>
		/// </summary>
		public virtual void  SetMaxMergeDocs(int maxMergeDocs)
		{
			this.maxMergeDocs = maxMergeDocs;
		}
		
		/// <summary>Returns the largest segment (measured by document
		/// count) that may be merged with other segments.
		/// </summary>
		/// <seealso cref="setMaxMergeDocs">
		/// </seealso>
		public virtual int GetMaxMergeDocs()
		{
			return maxMergeDocs;
		}
	}
}