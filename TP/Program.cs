using System;
using System.Collections.Generic;
using System.Globalization;
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
        internal static readonly string rutaData = @"C:\Fer\TpLucene\Data";//@"D:\Fer\Facultad\RI\TP1\TP\Data";//
        internal static readonly string archCorpus = Path.Combine(rutaData, "corpus");
        internal static readonly string archQuerys = Path.Combine(rutaData, "querys");
        internal static string archResultadoDevuelto = Path.Combine(rutaData, "result.txt");
        internal static readonly string archResults = Path.Combine(rutaData, "results");
        internal static string dirIndices = Path.Combine(rutaData, "Indices");        
        private static IDictionary<string, IList<string>> diccionario;
        private static bool useAlternativeScoreFunction;
        private static string[] stopWords = new []
                {
                    "a", "about", "above", "across", "after", "afterwards", "again", "against", "all", "almost", "alone", "along", "already", "also", "although", "always", "am", "among", "amongst", "amoungst", "amount",
                    "an", "and", "another", "any", "anyhow", "anyone", "anything", "anyway", "anywhere", "are", "around", "as", "at", "back", "be", "became", "because", "become", "becomes", "becoming", "been", "before",
                    "beforehand", "behind", "being", "below", "beside", "besides", "between", "beyond", "bill", "both", "bottom", "but", "by", "call", "can", "cannot", "cant", "co", "computer", "con", "could", "couldnt",
                    "cry", "de", "describe", "detail", "do", "done", "down", "due", "during", "each", "eg", "eight", "either", "eleven", "else", "elsewhere", "empty", "enough", "etc", "even", "ever", "every", "everyone",
                    "everything", "everywhere", "except", "few", "fifteen", "fify", "fill", "find", "fire", "first", "five", "for", "former", "formerly", "forty", "found", "four", "from", "front", "full", "further", "get",
                    "give", "go", "had", "has", "hasnt", "have", "he", "hence", "her", "here", "hereafter", "hereby", "herein", "hereupon", "hers", "herself", "him", "himself", "his", "how", "however", "hundred", "i", "ie",
                    "if", "in", "inc", "indeed", "interest", "into", "is", "it", "its", "itself", "keep", "last", "latter", "latterly", "least", "less", "ltd", "made", "many", "may", "me", "meanwhile", "might", "mill",
                    "mine", "more", "moreover", "most", "mostly", "move", "much", "must", "my", "myself", "name", "namely", "neither", "never", "nevertheless", "next", "nine", "no", "nobody", "none", "noone", "nor", "not",
                    "nothing", "now", "nowhere", "of", "off", "often", "on", "once", "one", "only", "onto", "or", "other", "others", "otherwise", "our", "ours", "ourselves", "out", "over", "own", "part", "per", "perhaps",
                    "please", "put", "rather", "re", "same", "see", "seem", "seemed", "seeming", "seems", "serious", "several", "she", "should", "show", "side", "since", "sincere", "six", "sixty", "so", "some", "somehow",
                    "someone", "something", "sometime", "sometimes", "somewhere", "still", "such", "system", "take", "ten", "than", "that", "the", "their", "them", "themselves", "then", "thence", "there", "thereafter",
                    "thereby", "therefore", "therein", "thereupon", "these", "they", "thick", "thin", "third", "this", "those", "though", "three", "through", "throughout", "thru", "thus", "to", "together", "too", "top",
                    "toward", "towards", "twelve", "twenty", "two", "un", "under", "until", "up", "upon", "us", "very", "via", "was", "we", "well", "were", "what", "whatever", "when", "whence", "whenever", "where",
                    "whereafter", "whereas", "whereby", "wherein", "whereupon", "wherever", "whether", "which", "while", "whither", "who", "whoever", "whole", "whom", "whose", "why", "will", "with", "within", "without",
                    "would", "yet", "you", "your", "yours", "yourself", "yourselves"
                };
        #endregion

        #region Métodos
        
        // Ejecuta la consulta y devuelve una lista con las mediciones; una medición por cada query.
        private static IList<Medicion> Medir()
        {
            IList<Medicion> resultados = new List<Medicion>();
            StreamReader reader = new StreamReader(archQuerys);
            QueryParser parser = new MultiFieldQueryParser(new[] {"T", "W"}, analyzer);
            Searcher indexSearcher = new IndexSearcher(dirIndices);
            if(useAlternativeScoreFunction)
                indexSearcher.SetSimilarity(new AlternativeScore());
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
        //Ejecuta la consulta y devuelve los Hits
        private static Hits EjecutarConsulta(QueryParser qp, string querystring, Searcher buscador)
        {
            Query query;
            Hits hits;
            query = qp.Parse(querystring);
            hits = buscador.Search(query);
            return hits;
        }
        //A partir de una lista de mediciones graba las mismas y su promedio en un archivo
        private static void GrabarResultados(IList<Medicion> resultados)
        {
            var sw = new StreamWriter(archResultadoDevuelto);
            CultureInfo ci = new CultureInfo("es-ar");

            int cantResultados = resultados.Count;
            float totalPrecision = 0, totalRecall = 0, totalRPrecision = 0, totalKPrecision1 = 0, totalKPrecision2 = 0, totalFMeasure = 0f;

            foreach (Medicion resultado in  resultados)
            {
                if (!float.IsNaN(resultado.Precision))
                    totalPrecision += resultado.Precision;
                if (!float.IsNaN(resultado.Recall))
                    totalRecall += resultado.Recall;
                if (!float.IsNaN(resultado.RPrecision))
                    totalRPrecision += resultado.RPrecision;
                if (!float.IsNaN(resultado.KPrecision1))
                    totalKPrecision1 += resultado.KPrecision1;
                if (!float.IsNaN(resultado.KPrecision2))
                    totalKPrecision2 += resultado.KPrecision2;
                if (!float.IsNaN(resultado.FMeasure))
                    totalFMeasure += resultado.FMeasure;

                sw.WriteLine(resultado.NombreDelquery);
                sw.WriteLine("Precision: \f" + resultado.Precision.ToString("R", ci));
                sw.WriteLine("Recall: " + resultado.Recall.ToString("R", ci));
                sw.WriteLine("R-Precision: " + resultado.RPrecision.ToString("R", ci));
                sw.WriteLine("K-Precision1: " + resultado.KPrecision1.ToString("R", ci));
                sw.WriteLine("K-Precision2: " + resultado.KPrecision2.ToString("R", ci));
                sw.WriteLine("F-Measure: " + resultado.FMeasure.ToString("R", ci));
                sw.WriteLine();
            }

            sw.WriteLine();
            sw.WriteLine("Promedio");
            sw.WriteLine("Promedio precision: " + (totalPrecision / cantResultados).ToString("R", ci));
            sw.WriteLine("Promedio recall: " + (totalRecall / cantResultados).ToString("R", ci));
            sw.WriteLine("Promedio R-Precision: " + (totalRPrecision/cantResultados).ToString("R", ci));
            sw.WriteLine("Promedio K-Precision1: " + (totalKPrecision1/cantResultados).ToString("R", ci));
            sw.WriteLine("Promedio K-Precision2: " + (totalKPrecision2 / cantResultados).ToString("R", ci));
            sw.WriteLine("Promedio F-Measure: " + (totalFMeasure / cantResultados).ToString("R", ci));
            sw.Close();
        }
        //Parsea el corpus y realiza la indexación.
        private static void Indexar()
        {            
            var writer = new IndexWriter(dirIndices, analyzer, true);
            if (useAlternativeScoreFunction)
                writer.SetSimilarity(new AlternativeScore());
            writer.SetMaxBufferedDocs(110);
            var reader = new StreamReader(archCorpus);
            string linea = reader.ReadLine();

            while (linea != null)
                writer.AddDocument(ObtenerDocumento(reader, ref linea));

            writer.Optimize();
            writer.Close();
            reader.Close();
        }
        //Arma el documento con los campos U,T y W y los valores de los mismos obtenidos del corpus
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
            useAlternativeScoreFunction = false;
            IndexarBuscarYMedir();

            analyzer = new KeywordAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultKeywordAnalyzerWithAlternativeScoreFunction.txt");
            dirIndices = Path.Combine(rutaData, "IndicesKeywordAnalyzerWithAlternativeScoreFunction");
            useAlternativeScoreFunction = true;
            IndexarBuscarYMedir();

            analyzer = new SimpleAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultSimpleAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesSimpleAnalyzer");
            useAlternativeScoreFunction = false;
            IndexarBuscarYMedir();

            analyzer = new SimpleAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultSimpleAnalyzerWithAlternativeScoreFunction.txt");
            dirIndices = Path.Combine(rutaData, "IndicesSimpleAnalyzerWithAlternativeScoreFunction");
            useAlternativeScoreFunction = true;
            IndexarBuscarYMedir();

            analyzer = new StopAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultStopAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStopAnalyzer");
            useAlternativeScoreFunction = false;
            IndexarBuscarYMedir();

            analyzer = new StopAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultStopAnalyzerWithAlternativeScoreFunction.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStopAnalyzerWithAlternativeScoreFunction");
            useAlternativeScoreFunction = true;
            IndexarBuscarYMedir();

            analyzer = new WhitespaceAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultWhitespaceAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesWhitespaceAnalyzer");
            useAlternativeScoreFunction = false;
            IndexarBuscarYMedir();

            analyzer = new WhitespaceAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultWhitespaceAnalyzerWithAlternativeScoreFunction.txt");
            dirIndices = Path.Combine(rutaData, "IndicesWhitespaceAnalyzerWithAlternativeScoreFunction");
            useAlternativeScoreFunction = true;
            IndexarBuscarYMedir();

            analyzer = new StandardAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultStandardAnalyzer.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStandardAnalyzer");
            useAlternativeScoreFunction = false;
            IndexarBuscarYMedir();

            analyzer = new StandardAnalyzer();
            archResultadoDevuelto = Path.Combine(rutaData, "resultStandardAnalyzerWithAlternativeScoreFunction.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStandardAnalyzerWithAlternativeScoreFunction");
            useAlternativeScoreFunction = true;
            IndexarBuscarYMedir();

            analyzer = new StandardAnalyzer(stopWords);
            archResultadoDevuelto = Path.Combine(rutaData, "resultStandardAnalyzerWithCusomizedStopWords.txt");
            dirIndices = Path.Combine(rutaData, "IndicesStandardAnalyzerWithCusomizedStopWords");
            useAlternativeScoreFunction = false;
            IndexarBuscarYMedir();            
        }
        private static void IndexarBuscarYMedir()
        {
            IList<Medicion> resultados;
            
            Medicion.Alfa = 1;
            Medicion.K1 = 10;
            Medicion.K2 = 50;
            
            ProcesarArchivoResults();
            Indexar();
            resultados = Medir();
            GrabarResultados(resultados);
        }
        //Ejecuta una consulta y obtiene su medicion
        private static Medicion MedirConsulta(string nombreDelQuery, string querystring, QueryParser qp, Searcher buscador)
        {
            Hits hits = EjecutarConsulta(qp, querystring, buscador);
            return ObtenerMedicionDeLaConsulta(nombreDelQuery, hits);
        }
        //A partir de los hits de la busqueda realizada, calcula su medicion: itemsRelevantesDevueltosK1 y K2, itemsRelevantesDevueltosR, itemsRelevantesDevueltos, itemsRelevantes e itemsDevueltos
        private static Medicion ObtenerMedicionDeLaConsulta(string nombreDelQuery, Hits hits)
        {
            float itemsRelevantesDevueltosK1 = 0,itemsRelevantesDevueltosK2 = 0, itemsRelevantesDevueltosR = 0, itemsRelevantesDevueltos = 0;
            IList<string> items;
            float itemsDevueltos, itemsRelevantes;
            ;
            Document doc;

            items = diccionario[nombreDelQuery];
            itemsDevueltos = hits.Length();
            itemsRelevantes = items.Count;

            for (int i = 0; i < itemsDevueltos; i++)
            {
                doc = hits.Doc(i);
                if (items.Contains(doc.GetField("U").StringValue()))
                {
                    itemsRelevantesDevueltos++;
                    if (i < Medicion.K1)
                        itemsRelevantesDevueltosK1++;

                    if (i < Medicion.K2)
                        itemsRelevantesDevueltosK2++;

                    if (i < itemsRelevantes)
                        itemsRelevantesDevueltosR++;
                }
            }
            return new Medicion(nombreDelQuery, itemsRelevantesDevueltosK1, itemsRelevantesDevueltosK2, itemsRelevantesDevueltosR, itemsRelevantesDevueltos, itemsRelevantes, itemsDevueltos);
        }
        //Lee el archivo results y lo guarda en un diccionario: (nombreQuery, <documentosRelevantes>)
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
    //Encargada de calcular Precision, FMeasure, Recall, KPrecision1, KPrecision2 y RPrecision
    public class Medicion
    {
        #region Variables de clase
        public static float Alfa;
        public static float K1, K2;
        #endregion

        #region Variables de instancia
        public float DevueltosTotal;
        public string NombreDelquery;
        public float RelevantesDevueltos;
        public float RelevantesDevueltosK1;
        public float RelevantesDevueltosK2;
        public float RelevantesDevueltosR;
        public float RelevantesTotal;
        #endregion

        #region Constructores
        public Medicion(string nombreDelquery, float relevantesDevueltosK1, float relevantesDevueltosK2, float relevantesDevueltosR, float relevantesDevueltos, float relevantesTotal, float devueltosTotal)
        {
            NombreDelquery = nombreDelquery;
            RelevantesDevueltosK1 = relevantesDevueltosK1;
            RelevantesDevueltosK2 = relevantesDevueltosK2;
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

        public float KPrecision1
        {
            get
            {
                return RelevantesDevueltosK1/K1;
            }
        }
        public float KPrecision2
        {
            get
            {
                return RelevantesDevueltosK2 / K2;
            }
        }
        #endregion
    }
}