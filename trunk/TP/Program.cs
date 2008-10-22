using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace TP
{
    internal class Program
    {
        #region Variables de clase
        internal static Analyzer analyzer;
        internal static readonly string rutaData = @"D:\Fer\Facultad\RI\TP1\TP\Data";//@"C:\Fer\TpLucene\Data";
        internal static readonly string archCorpus = Path.Combine(rutaData, "corpus");
        internal static readonly string archQuerys = Path.Combine(rutaData, "querys");
        internal static string archResultadoDevuelto = Path.Combine(rutaData, "result.txt");
        internal static readonly string archResults = Path.Combine(rutaData, "results");
        internal static string dirIndices = Path.Combine(rutaData, "Indices");        
        private static IDictionary<string, IList<string>> diccionario;
        #endregion

        #region Métodos

        private static IList<Medicion> Medir()
        {
            IList<Medicion> resultados = new List<Medicion>();
            StreamReader reader = new StreamReader(archQuerys);
            QueryParser parser = new MultiFieldQueryParser(new[] {"T", "W"}, analyzer);
            Searcher indexSearcher = new IndexSearcher(dirIndices);
            string query = "", nombreDelQuery = "", linea = reader.ReadLine();

            while (linea != null)
            {
                if (linea.StartsWith("<num> Number: "))
                    nombreDelQuery = linea.Substring(14);
                if (linea.StartsWith("<title> "))
                    query = linea.Substring(8);
                if (linea.StartsWith("<desc> Description:"))
                {
                    query += " " + reader.ReadLine();
                    resultados.Add(MedirConsulta(nombreDelQuery, query, parser, indexSearcher));
                }
                linea = reader.ReadLine();
            }
            return resultados;            
        }
        private static Hits EjecutarConsulta(QueryParser qp, string querystring, Searcher buscador)
        {
            Query query;
            Hits hits;
            query = qp.Parse(querystring);
            hits = buscador.Search(query);
            return hits;
        }
        private static void GrabarResultados(IList<Medicion> resultados)
        {
            var sw = new StreamWriter(archResultadoDevuelto);

            int cantResultados = resultados.Count;
            float totalPrecision = 0, totalRecall = 0, totalRPrecision = 0, totalKPrecision = 0, totalFMeasure = 0;

            foreach (Medicion resultado in  resultados)
            {
                totalPrecision += resultado.Precision;
                totalRecall += resultado.Recall;
                totalRPrecision += resultado.RPrecision;
                totalKPrecision += resultado.KPrecision;
                totalFMeasure += resultado.FMeasure;

                sw.WriteLine(resultado.NombreDelquery);
                sw.WriteLine("Precision: " + resultado.Precision);
                sw.WriteLine("Recall: " + resultado.Recall);
                sw.WriteLine("R-Precision: " + resultado.RPrecision);
                sw.WriteLine("K-Precision: " + resultado.KPrecision);
                sw.WriteLine("F-Measure: " + resultado.FMeasure);
                sw.WriteLine();
            }

            sw.WriteLine();
            sw.WriteLine("Promedio");
            sw.WriteLine("Promedio precision: " + totalPrecision/cantResultados);
            sw.WriteLine("Promedio recall: " + totalRecall/cantResultados);
            sw.WriteLine("Promedio R-Precision: " + totalRPrecision/cantResultados);
            sw.WriteLine("Promedio K-Precision: " + totalKPrecision/cantResultados);
            sw.WriteLine("Promedio F-Measure: " + totalFMeasure/cantResultados);
            sw.Close();
        }

        private static void Indexar()
        {
            var writer = new IndexWriter(dirIndices, analyzer, true);
            writer.SetMaxBufferedDocs(110);
            var reader = new StreamReader(archCorpus);
            string linea = reader.ReadLine();

            while (linea != null)
                writer.AddDocument(ObtenerDocumento(reader, ref linea));

            writer.Optimize();
            writer.Close();
            reader.Close();
        }

        private static Document ObtenerDocumento(StreamReader reader, ref string linea)
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

            //var f = new Field("U", reader.ReadLine(), Field.Store.YES, Field.Index.NO);
            //f.SetBoost();//peso
            var doc = new Document();
            while (linea != null)
            {
                switch (linea)
                {
                    case ".U":
                        doc.Add(new Field("U", reader.ReadLine(), Field.Store.YES, Field.Index.NO));
                        break;
                    case ".T":
                        doc.Add(new Field("T", reader.ReadLine(), Field.Store.NO, Field.Index.TOKENIZED));
                        break;
                    case ".W":
                        doc.Add(new Field("W", reader.ReadLine(), Field.Store.NO, Field.Index.TOKENIZED));
                        break;
                 /*   case ".M":
                        doc.Add(new Field("M", reader.ReadLine(), Field.Store.NO, Field.Index.TOKENIZED));
                        break;*/
                }
                linea = reader.ReadLine();
                if (linea != null && linea[0] == '.' && linea[1] == 'I')
                    break;
            }
            return doc;
        }
        [STAThread]
        public static void Main(String[] args)
        {
            analyzer = new KeywordAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultKeywordAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesKeywordAnalyzer");        
            IndexarYMedir();

            analyzer = new SimpleAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultSimpleAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesSimpleAnalyzer");        
            IndexarYMedir();

            analyzer = new StopAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultStopAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStopAnalyzer");        
            IndexarYMedir();

            analyzer = new WhitespaceAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultWhitespaceAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesWhitespaceAnalyzer");        
            IndexarYMedir();

            analyzer = new StandardAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultStandardAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStandardAnalyzer");        
            IndexarYMedir();            
        }
        private static void IndexarYMedir()
        {
            IList<Medicion> resultados;
            
            Medicion.Alfa = 1;
            Medicion.K = 50;
            
            ProcesarArchivoResults();
            Indexar();
            resultados = Medir();
            GrabarResultados(resultados);
        }
        private static Medicion MedirConsulta(string nombreDelQuery, string querystring, QueryParser qp, Searcher buscador)
        {
            Hits hits = EjecutarConsulta(qp, querystring, buscador);
            return ObtenerMedicionDeLaConsulta(nombreDelQuery, hits);
        }
        private static Medicion ObtenerMedicionDeLaConsulta(string nombreDelQuery, Hits hits)
        {
            float itemsRelevantesDevueltosK = 0, itemsRelevantesDevueltosR = 0, itemsRelevantesDevueltos = 0;
            IList<string> items;
            float itemsDevueltos, itemsRelevantes;
            ;
            Document doc;

            items = diccionario[nombreDelQuery];
            itemsDevueltos = hits.Length();
            itemsRelevantes = items.Count;

            for (int i = 0, r = 0, k = 0; i < itemsDevueltos; i++,k++,r++)
            {
                doc = hits.Doc(i);
                if (items.Contains(doc.GetField("U").StringValue()))
                {
                    itemsRelevantesDevueltos++;
                    if (k < Medicion.K)
                        itemsRelevantesDevueltosK++;

                    if (r < itemsRelevantes)
                        itemsRelevantesDevueltosR++;
                }
            }
            return new Medicion(nombreDelQuery, itemsRelevantesDevueltosK, itemsRelevantesDevueltosR, itemsRelevantesDevueltos, itemsRelevantes, itemsDevueltos);
        }

        private static void ProcesarArchivoResults()
        {
            diccionario = new Dictionary<string, IList<string>>();
            var reader = new StreamReader(archResults);
            string linea = reader.ReadLine();
            string[] pedazos;

            while (linea != null)
            {
                pedazos = linea.Split('\t');
                if (!diccionario.ContainsKey(pedazos[0]))
                    diccionario.Add(pedazos[0], new List<string>());

                diccionario[pedazos[0]].Add(pedazos[1]);
                linea = reader.ReadLine();
            }
        }
        #endregion
    }

    public class Medicion
    {
        #region Variables de clase
        public static float Alfa;
        public static float K;
        #endregion

        #region Variables de instancia
        public float DevueltosTotal;
        public string NombreDelquery;
        public float RelevantesDevueltos;
        public float RelevantesDevueltosK;
        public float RelevantesDevueltosR;
        public float RelevantesTotal;
        #endregion

        #region Constructores
        public Medicion(string nombreDelquery, float relevantesDevueltosK, float relevantesDevueltosR, float relevantesDevueltos, float relevantesTotal, float devueltosTotal)
        {
            NombreDelquery = nombreDelquery;
            RelevantesDevueltosK = relevantesDevueltosK;
            RelevantesDevueltosR = relevantesDevueltosR;
            RelevantesDevueltos = relevantesDevueltos;
            RelevantesTotal = relevantesTotal;
            DevueltosTotal = devueltosTotal;
        }
        #endregion

        #region Propiedades
        public float Precision
        {
            get
            {
                return RelevantesDevueltos/DevueltosTotal;
            }
        }

        public float Recall
        {
            get
            {
                return RelevantesDevueltos/RelevantesTotal;
            }
        }

        public float FMeasure
        {
            get
            {
                return (1 + Alfa)*Recall*Precision/(Recall + (Alfa*Precision));
            }
        }

        public float RPrecision
        {
            get
            {
                return RelevantesDevueltosR/RelevantesTotal;
                ;
            }
        }

        public float KPrecision
        {
            get
            {
                return RelevantesDevueltosK/K;
            }
        }
        #endregion
    }
}