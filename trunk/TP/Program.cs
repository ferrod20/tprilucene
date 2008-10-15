using System;
using System.Collections.Generic;
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
        #region Variables de clase
        internal static readonly string rutaTp1 = @"D:\Fer\Facultad\RI\TP1";
        internal static readonly string archCorpus = Path.Combine(rutaTp1, "corpus");
        internal static readonly string archQuerys = Path.Combine(rutaTp1, "querys");
        internal static readonly string archResults = Path.Combine(rutaTp1, "results");
        internal static readonly string dirIndices = Path.Combine(rutaTp1, "Indices");        
        private static IDictionary<string, IList<string>> diccionario;
        #endregion

        #region Métodos
        private static void Buscar()
        {
            //Analizadores sintacticos:----------------------------------------
            //StopAnalyzer Elimina stop words
            //WhitespaceAnalyzer divide el texto por espacios en blanco
            //SimpleAnalyzer divide el texto donde no hay letras y convierte a minuscula
            //StandardAnalyzer Elimina stop words y convierte a minuscula

            //Hacer querys:----------------------------------------------------            
            string querystring = "chomba saco sucio lego sucio comida camisa"; //"infusion";
            var qp = new MultiFieldQueryParser(new[] {"W", "T"}, new StandardAnalyzer());
            Query query = qp.Parse(querystring);

            //Buscar-----------------------------------------------------------
            var buscador = new IndexSearcher(dirIndices);
            Hits hits = buscador.Search(query);
            int hasta = Math.Min(hits.Length(), 10);
            for (int i = 0; i < hasta; i++)
            {
                Document doc = hits.Doc(i);
                Console.Out.WriteLine(i + "-" + doc.GetField("I").StringValue() + " (" + hits.Score(i) + ") [" + doc.GetField("T").StringValue() + "]");
            }
        }
        private static void CalcularPrecision()
        {
            IList<PrecisionRecallRPrecision> resultados = new List<PrecisionRecallRPrecision>();
            StreamReader reader = new StreamReader(archQuerys);
            QueryParser parser = new MultiFieldQueryParser(new[] { "T", "W" }, new StandardAnalyzer());
            var indexSearcher = new IndexSearcher(dirIndices);
            string query = "", nombreDelQuery = "",linea = reader.ReadLine();                        

            while (linea != null)
            {
                if (linea.StartsWith("<num> Number: "))
                    nombreDelQuery = linea.Substring(14);
                 if (linea.StartsWith("<title> "))
                    query = linea.Substring(8);
                if (linea.StartsWith("<desc> Description:"))
                {
                    query += " " + reader.ReadLine();
                    resultados.Add(Buscar(nombreDelQuery, query, parser, indexSearcher));
                }
                linea = reader.ReadLine();
            }

            ImprimirResultados(resultados);
        }
        private static void ImprimirResultados(IList<PrecisionRecallRPrecision> resultados)
        {
            int cantResultados = resultados.Count;
            float totalPrecision = 0,totalRecall = 0,totalRPrecision = 0;
            foreach(PrecisionRecallRPrecision resultado in  resultados)
            {
                totalPrecision += resultado.Precision;
                totalRecall += resultado.Recall;
                totalRPrecision += resultado.RPrecision;
            }
            
            Console.WriteLine("Promedio precision: " + totalPrecision / cantResultados);
            Console.WriteLine("Promedio recall: " + totalRecall / cantResultados);
            Console.WriteLine("Promedio R-Precision: " + totalRPrecision / cantResultados);
        }
        private static PrecisionRecallRPrecision Buscar(string nombreDelQuery, string querystring, QueryParser qp, IndexSearcher buscador)
        {
            IList<string> items;
            Query query;
            Hits hits;
            Document doc;
            float itemsDevueltos,itemsRelevantesDevueltos,itemsRelevantes;

            query = qp.Parse(querystring);
            hits = buscador.Search(query);
            
            itemsDevueltos = hits.Length();
            itemsRelevantesDevueltos = 0;
            items = diccionario[nombreDelQuery];
            itemsRelevantes = items.Count;

            for (int i = 0; i < itemsDevueltos; i++)
            {
                doc = hits.Doc(i);
                if (items.Contains(doc.GetField("I").StringValue()))
                    itemsRelevantesDevueltos++;
            }

            var valor = new PrecisionRecallRPrecision();
            valor.Precision = itemsRelevantesDevueltos/itemsDevueltos;
            valor.Recall = itemsRelevantesDevueltos/itemsRelevantes;

            return valor;
        }

        private static void Indexar()
        {
            var writer = new IndexWriter(dirIndices, new StandardAnalyzer(), true);
            writer.SetMaxBufferedDocs(120);
            var reader = new StreamReader(archCorpus);
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

            var doc = new Document();
            // doc.Add(new Field("I", linea, Field.Store.YES, Field.Index.NO));
            while (linea != null)
            {
                switch (linea)
                {
                    case ".U":
                        doc.Add(new Field("I", reader.ReadLine(), Field.Store.YES, Field.Index.NO));
                        break;
                    case ".T":
                        doc.Add(new Field("T", reader.ReadLine(), Field.Store.YES, Field.Index.TOKENIZED));
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
        [STAThread]
        public static void Main(String[] args)
        {
            diccionario = new Dictionary<string, IList<string>>();
            ObtenerResultadosDelQuery();
            Indexar();
            CalcularPrecision();
            //Buscar();
        }

        private static void ObtenerResultadosDelQuery()
        {
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

    public class PrecisionRecallRPrecision
    {
        #region Variables de instancia
        public float Precision;
        public float Recall;
        public float RPrecision;
        #endregion
    }
}