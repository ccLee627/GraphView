﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphView.TSQL_Syntax_Tree;

namespace GraphView
{
    /// <summary>
    /// The visitor that classifies table references in a FROM clause
    /// into named table references and others. A named table reference
    /// represents the entire collection of vertices in the graph. 
    /// Other table references correspond to query derived tables, 
    /// variable tables defined earlier in the script, or table-valued 
    /// functions.
    /// </summary>
    internal class TableClassifyVisitor : WSqlFragmentVisitor
    {
        private List<WNamedTableReference> vertexTableList;
        private List<WTableReferenceWithAlias> nonVertexTableList;
        
        public void Invoke(
            WFromClause fromClause, 
            List<WNamedTableReference> vertexTableList, 
            List<WTableReferenceWithAlias> nonVertexTableList)
        {
            this.vertexTableList = vertexTableList;
            this.nonVertexTableList = nonVertexTableList;

            foreach (WTableReference tabRef in fromClause.TableReferences)
            {
                tabRef.Accept(this);
            }
        }

        public override void Visit(WNamedTableReference node)
        {
            vertexTableList.Add(node);
        }

        public override void Visit(WQueryDerivedTable node)
        {
            nonVertexTableList.Add(node);
        }

        public override void Visit(WSchemaObjectFunctionTableReference node)
        {
            nonVertexTableList.Add(node);
        }

        public override void Visit(WVariableTableReference node)
        {
            nonVertexTableList.Add(node);
        }
    }

    /// <summary>
    /// The visitor that traverses the syntax tree and returns the columns 
    /// accessed in current query fragment for each provided table alias. 
    /// This visitor is used to determine what vertex/edge properties are projected 
    /// when a JSON query is sent to the underlying system to retrieve vertices and edges. 
    /// </summary>
    internal class AccessedTableColumnVisitor : WSqlFragmentVisitor
    {
        // A collection of table aliases and their columns
        // accessed in the query block
        Dictionary<string, HashSet<string>> accessedColumns;

        public Dictionary<string, HashSet<string>> Invoke(WSqlFragment sqlFragment, List<string> tableAliases)
        {
            accessedColumns = new Dictionary<string, HashSet<string>>(tableAliases.Count);
            foreach (string tabAlias in tableAliases)
            {
                accessedColumns.Add(tabAlias, new HashSet<string>());
            }

            sqlFragment.Accept(this);
            return accessedColumns;
        }

        public override void Visit(WColumnReferenceExpression node) 
        {
            string columnName = node.ColumnName;
            string tableAlias = node.TableReference;

            if (tableAlias == null)
            {
                throw new QueryCompilationException("Identifier " + columnName + " must be bound to a table alias.");
            }

            if (accessedColumns.ContainsKey(tableAlias))
            {
                accessedColumns[tableAlias].Add(columnName);
            }
        }

        //public override void Visit(WMatchPath node)
        //{
        //    foreach (var sourceEdge in node.PathEdgeList)
        //    {
        //        WSchemaObjectName source = sourceEdge.Item1;
        //        string tableAlias = source.BaseIdentifier.Value;
        //        WEdgeColumnReferenceExpression edge = sourceEdge.Item2;

        //        if (accessedColumns.ContainsKey(tableAlias))
        //        {
        //            switch (edge.EdgeType)
        //            {
        //                case WEdgeType.OutEdge:
        //                    accessedColumns[tableAlias].Add(ColumnGraphType.OutAdjacencyList.ToString());
        //                    break;
        //                case WEdgeType.InEdge:

        //            }
        //        }
        //    }
        //}
    }
}
