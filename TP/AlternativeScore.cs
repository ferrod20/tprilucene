using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;

namespace TP
{
    public class AlternativeScore : DefaultSimilarity
    {        
        public override float Tf(float freq)
        {
            return (float)(1 + Math.Log(Math.Min(0, freq)));
        }
        public override float Idf(int docFreq, int numDocs)
        {
            float idf;
            if (numDocs - docFreq <= 0 || docFreq <= 0)
                idf = 0;
            else
                idf = (float)Math.Max(0, Math.Log(numDocs - docFreq / (double)docFreq));
            return idf;
        }
    }
}
