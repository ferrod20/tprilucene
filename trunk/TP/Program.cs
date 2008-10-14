using System;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace TP
{
    internal class Program
    {
        internal static readonly string rutaTp1 = @"D:\Fer\Facultad\RI\TP1";
        internal static readonly string archCorpus = Path.Combine(rutaTp1, "corpus");
        internal static readonly string archQuerys = Path.Combine(rutaTp1, "querys");
        internal static readonly string dirIndices = Path.Combine(rutaTp1, "Indices");

        [STAThread]
        public static void Main(String[] args)
        {
            //Indexar();
            CalcularPrecision();
            //Buscar();
        }

        private static void CalcularPrecision()
        {
            var qp = new MultiFieldQueryParser(new []{"T","W"}, new StandardAnalyzer());
            StreamReader reader = new StreamReader(archQuerys);
            Query query;
            string querystring = "";
            string linea = reader.ReadLine();

            while(linea!=null)
            {                
                if( linea.StartsWith("<title> "))
                    querystring = "T:(" + linea.Substring(8) + ") OR ";
                if (linea.StartsWith("<desc> "))
                {
                    querystring += "W:(" + linea.Substring(7) + ")";
                    query = qp.Parse(querystring);
                    var buscador = new IndexSearcher(dirIndices);
                    Hits hits = buscador.Search(query);
                    int hasta = Math.Min(hits.Length(), 10);
                    for (int i = 0; i < hasta; i++)
                    {
                        Document doc = hits.Doc(i);
                        Console.Out.WriteLine(i + doc.GetField("I").StringValue() + " (" + hits.Score(i) + ") [" +
                                              doc.GetField("T").StringValue() + "]");
                    }
                }
                linea = reader.ReadLine();
            }            
        }

        private static void Buscar()
        {
            //Analizadores sintacticos:----------------------------------------
            //StopAnalyzer Elimina stop words
            //WhitespaceAnalyzer divide el texto por espacios en blanco
            //SimpleAnalyzer divide el texto donde no hay letras y convierte a minuscula
            //StandardAnalyzer Elimina stop words y convierte a minuscula

            //Hacer querys:----------------------------------------------------            
            string querystring = "Are there adverse effects on lipids when progesterone is given with estrogen replacement therapy";//"infusion";
            var qp = new QueryParser("W", new StandardAnalyzer());
            Query query = qp.Parse(querystring);

            //Buscar-----------------------------------------------------------
            var buscador = new IndexSearcher(dirIndices);
            Hits hits = buscador.Search(query);
            int hasta = Math.Min(hits.Length(), 10);
            for (int i = 0; i < hasta; i++)
            {
                Document doc = hits.Doc(i);
                Console.Out.WriteLine(i + doc.GetField("I").StringValue() + " (" + hits.Score(i) + ") [" +
                                      doc.GetField("T").StringValue() + "]");
            }
        }

        private static void Indexar()
        {
            IndexWriter writer = new IndexWriter(dirIndices, new StandardAnalyzer(), true);
            writer.SetMaxBufferedDocs(150);
            StreamReader reader = new StreamReader(archCorpus);
            string linea = reader.ReadLine();

            while (linea != null)
                writer.AddDocument(Leer(reader, ref linea));                    

            writer.Optimize();
            writer.Close();
            reader.Close();            
        }

        private static Document Leer(StreamReader reader, ref string linea)
        {
            #region Doc ejemplo
            //.I 1
//.U
//87049087
//.S
//Am J Emerg Med 8703; 4(6):491-5
//.M
//Allied Health Personnel/*; Electric Countershock/*; Emergencies; Emergency Medical Technicians/*; Human; Prognosis; Recurrence; Support, U.S. Gov't, P.H.S.; Time Factors; Transportation of Patients; Ventricular Fibrillation/*TH.
//.T
//Refibrillation managed by EMT-Ds: incidence and outcome without paramedic back-up.
//.P
//JOURNAL ARTICLE.
//.W
//Some patients converted from ventricular fibrillation to organized rhythms by defibrillation-trained ambulance technicians (EMT-Ds) will refibrillate before hospital arrival. The authors analyzed 271 cases of ventricular fibrillation managed by EMT-Ds working without paramedic back-up. Of 111 patients initially converted to organized rhythms, 19 (17%) refibrillated, 11 (58%) of whom were reconverted to perfusing rhythms, including nine of 11 (82%) who had spontaneous pulses prior to refibrillation. Among patients initially converted to organized rhythms, hospital admission rates were lower for patients who refibrillated than for patients who did not (53% versus 76%, P = NS), although discharge rates were virtually identical (37% and 35%, respectively). Scene-to-hospital transport times were not predictively associated with either the frequency of refibrillation or patient outcome. Defibrillation-trained EMTs can effectively manage refibrillation with additional shocks and are not at a significant disadvantage when paramedic back-up is not available.
//.A
            //Stults KR; Brown DD.
            #endregion

            Document doc = new Document();
           // doc.Add(new Field("I", linea, Field.Store.YES, Field.Index.NO));
            while (linea != null)
            {
                switch (linea)
                {
                    case ".U":
                        doc.Add(new Field("I", reader.ReadLine(), Field.Store.YES, Field.Index.NO));
                        break;
                    case ".T":
                        doc.Add(new Field("T", reader.ReadLine(), Field.Store.YES, Field.Index.NO));
                        break;
                    case ".W":
                        doc.Add(new Field("W", reader.ReadLine(), Field.Store.NO, Field.Index.TOKENIZED));
                        break;
                    case ".M":
                        doc.Add(new Field("M", reader.ReadLine(), Field.Store.NO, Field.Index.TOKENIZED));
                        break;
                }
                linea = reader.ReadLine();
                if (linea != null && linea[0] == '.' && linea[1] == 'I')
                    break;
            }
            return doc;
        }
    }

    public class PrecisionRecallRPrecision
    {
        public float Precision;
        public float RPrecision;
        public float Recall;
    }
}