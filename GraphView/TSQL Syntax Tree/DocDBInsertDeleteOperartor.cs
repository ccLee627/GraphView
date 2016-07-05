﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GraphView
{

    internal class InsertEdgeOperator : GraphViewOperator
    {
        public TraversalProcessor SelectInput;
        public string edge;
        public string source, sink;
        public GraphViewConnection dbConnection;
        private bool UploadFinish;


        public InsertEdgeOperator(GraphViewConnection dbConnection, TraversalProcessor SelectInput, string edge, string source, string sink)
        {
            this.dbConnection = dbConnection;
            this.SelectInput = SelectInput;
            this.edge = edge;
            this.source = source;
            this.sink = sink;
        }

        #region Unfinish GroupBy-method
        /*public override Record Next()
        {
            Dictionary<string, List<string>> groupBySource = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
            Dictionary<string, List<string>> groupBySink = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();

            SelectInput.Open();
            while (SelectInput.Status())
            {
                Record rec = (Record)SelectInput.Next();
                string source = rec.RetriveData(null, 0);
                string sink = rec.RetriveData(null, 1);

                if (!groupBySource.ContainsKey(source))
                {
                    groupBySource[source] = new System.Collections.Generic.List<string>();
                }
                groupBySource[source].Add(sink);

                if (!groupBySink.ContainsKey(sink))
                {
                    groupBySink[sink] = new System.Collections.Generic.List<string>();
                }
                groupBySink[sink].Add(source);
            }
            SelectInput.Close();

            foreach (string source in groupBySource.Keys)
            {
                // Insert edges into the source doc
            }

            foreach (string sink in groupBySink.Keys)
            {
                // Insert reverse edges into the sink doc
            }

            return null;
        }*/
        #endregion

        public override Record Next()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            while (SelectInput.Status())
            {
                //get source and sink's id from SelectQueryBlock's TraversalProcessor 
                Record rec = SelectInput.Next();
                List<string> header = SelectInput.RetriveHeader();
                string sourceid = rec.RetriveData(header, source);
                string sinkid = rec.RetriveData(header, sink);

                //Create one if a document not exist.
                if (!map.ContainsKey(sourceid))
                {
                    var documents =
                        dbConnection.DocDBclient.CreateDocumentQuery(
                            "dbs/" + dbConnection.DocDB_DatabaseId + "/colls/" +
                            dbConnection.DocDB_CollectionId,
                            "SELECT * " +
                            string.Format("FROM doc WHERE doc.id = \"{0}\"", sourceid));
                    foreach (var doc in documents)
                        map[sourceid] = JsonConvert.SerializeObject(doc);
                }
                if (!map.ContainsKey(sinkid))
                {
                    var documents =
                        dbConnection.DocDBclient.CreateDocumentQuery(
                            "dbs/" + dbConnection.DocDB_DatabaseId + "/colls/" +
                            dbConnection.DocDB_CollectionId,
                            "SELECT * " +
                            string.Format("FROM doc WHERE doc.id = \"{0}\"", sinkid));
                    foreach (var doc in documents)
                        map[sinkid] = JsonConvert.SerializeObject(doc);
                }

                GraphViewDocDBCommand.INSERT_EDGE(map, edge, sourceid, sinkid);
            }

            UploadFinish = false;
            ReplaceDocument(map);

            while (!UploadFinish)
                System.Threading.Thread.Sleep(100);

            return null;
        }

        public async Task ReplaceDocument(Dictionary<string, string> map)
        {
            foreach (var cnt in map)
                await GraphViewDocDBCommand.ReplaceDocument(dbConnection, cnt.Key, cnt.Value);
            UploadFinish = true;
        }
    }

    internal class DeleteEdgeOperator : GraphViewOperator
    {
        public TraversalProcessor SelectInput;
        public string source, sink;
        public GraphViewConnection dbConnection;
        private bool UploadFinish;
        public string EdgeID_str;
        public string EdgeReverseID_str;
        public DeleteEdgeOperator(GraphViewConnection dbConnection, TraversalProcessor SelectInput,  string source, string sink, string EdgeID_str, string EdgeReverseID_str)
        {
            this.dbConnection = dbConnection;
            this.SelectInput = SelectInput;
            this.source = source;
            this.sink = sink;
            this.EdgeID_str = EdgeID_str;
            this.EdgeReverseID_str = EdgeReverseID_str;
        }

        public override Record Next()
        {
            Dictionary<string, string> map = new Dictionary<string, string>();

            while (SelectInput.Status())
            {
                //get source and sink's id from SelectQueryBlock's TraversalProcessor 
                Record rec = SelectInput.Next();
                List<string> header = SelectInput.RetriveHeader();
                string sourceid = rec.RetriveData(header, source);
                string sinkid = rec.RetriveData(header, sink);
                string EdgeID = rec.RetriveData(header, EdgeID_str);
                string EdgeReverseID = rec.RetriveData(header, EdgeReverseID_str);

                //Create one if a document not exist.
                if (!map.ContainsKey(sourceid))
                {
                    var documents =
                        dbConnection.DocDBclient.CreateDocumentQuery(
                            "dbs/" + dbConnection.DocDB_DatabaseId + "/colls/" +
                            dbConnection.DocDB_CollectionId,
                            "SELECT * " +
                            string.Format("FROM doc WHERE doc.id = \"{0}\"", sourceid));
                    foreach (var doc in documents)
                        map[sourceid] = JsonConvert.SerializeObject(doc);
                }
                if (!map.ContainsKey(sinkid))
                {
                    var documents =
                        dbConnection.DocDBclient.CreateDocumentQuery(
                            "dbs/" + dbConnection.DocDB_DatabaseId + "/colls/" +
                            dbConnection.DocDB_CollectionId,
                            "SELECT * " +
                            string.Format("FROM doc WHERE doc.id = \"{0}\"", sinkid));
                    foreach (var doc in documents)
                        map[sinkid] = JsonConvert.SerializeObject(doc);
                }

                int ID, reverse_ID;
                int.TryParse(EdgeID, out ID);
                int.TryParse(EdgeReverseID, out reverse_ID);
                map[source] = GraphViewJsonCommand.Delete_edge(map[source], ID);
                map[sink] = GraphViewJsonCommand.Delete_reverse_edge(map[sink], reverse_ID);
            }

            UploadFinish = false;
            ReplaceDocument(map);

            while (!UploadFinish)
                System.Threading.Thread.Sleep(100);

            return null;
        }
        public async Task ReplaceDocument(Dictionary<string, string> map)
        {
            foreach (var cnt in map)
                await GraphViewDocDBCommand.ReplaceDocument(dbConnection, cnt.Key, cnt.Value);
            UploadFinish = true;
        }
    }
}
