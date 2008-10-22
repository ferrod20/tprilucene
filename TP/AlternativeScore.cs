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
            float res = 0;
            if( freq >0)
                res = (float)(1 + Math.Log(Math.Max(0, freq)));
            return res;
            
        }
        public override float Idf(int docFreq, int numDocs)
        {
            float idf;
            if (numDocs - docFreq <= 0 || docFreq <= 0)
                idf = 0;
            else
                idf = (float)Math.Log(numDocs - docFreq / (double)docFreq);
            return idf;
        }
    }
}
