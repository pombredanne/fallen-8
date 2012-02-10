// 
// Fallen8PersistencyFactory.cs
//  
// Author:
//       Henning Rauch <Henning@RauchEntwicklung.biz>
// 
// Copyright (c) 2012 Henning Rauch
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using Fallen8.API.Model;
using Fallen8.API.Index;
using System.IO;
using Fallen8.API.Helper;
using Framework.Serialization;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace Fallen8.API.Persistency
{
    /// <summary>
    /// Fallen8 persistency factory.
    /// </summary>
    public static class Fallen8PersistencyFactory
    {
        #region public methods
  
        /// <summary>
        /// Load Fallen-8 from a save point
        /// </summary>
        /// <param name='pathToSavePoint'>
        /// Path to save point.
        /// </param>
        /// <param name='currentIdOfFallen8'>
        /// Current identifier of Fallen-8.
        /// </param>
        /// <param name='graphElementsOfFallen8'>
        /// Graph elements of Fallen-8.
        /// </param>
        /// <param name='indexFactoryOfFallen8'>
        /// Index factory of Fallen-8.
        /// </param>
        public static void Load (string pathToSavePoint, ref int currentIdOfFallen8, ref List<AGraphElement> graphElementsOfFallen8, ref IFallen8IndexFactory indexFactoryOfFallen8)
        {
            //if there is no savepoint file... do nothing
            if (!File.Exists(pathToSavePoint))
            {
                return;
            }
            
            var file = File.Open(pathToSavePoint, FileMode.Open, FileAccess.Read);
            var reader = new SerializationReader(file);
            
            //get the maximum id
            currentIdOfFallen8 = reader.ReadOptimizedInt32();
            
            //initialize the list of graph elements
            graphElementsOfFallen8 = new List<AGraphElement>(reader.ReadOptimizedInt32());
            
            var graphElementStreams = new List<String>();
            for (int i = 0; i < reader.ReadOptimizedInt32(); i++) 
            {
                graphElementStreams.Add(reader.ReadOptimizedString());
            }
            
            LoadGraphElements(graphElementsOfFallen8, graphElementStreams);
            
            var indexStreams = new List<String>();
            for (int i = 0; i < reader.ReadOptimizedInt32(); i++) 
            {
                indexStreams.Add(reader.ReadOptimizedString());
            }
        }
        
        /// <summary>
        /// Save the specified graphElements, indices and pathToSavePoint.
        /// </summary>
        /// <param name='graphElements'>
        /// Graph elements.
        /// </param>
        /// <param name='indices'>
        /// Indices.
        /// </param>
        /// <param name='path'>
        /// Path.
        /// </param>
        public static void Save(Int32 currentId, List<AGraphElement> graphElements, IDictionary<String, IIndex> indices, String path)
        {
            // Create the new, empty data file.
            if (File.Exists(path))
            {
                //the newer save gets an timestamp
                path = path + DateTime.Now.ToBinary().ToString();
            }
            
            var file = File.Create(path, Constants.BufferSize, FileOptions.SequentialScan);
            var writer = new SerializationWriter(file);
            
            //the maximum id
            writer.WriteOptimized(currentId);
            
            #region graph elements
            
            //the number of maximum graph elements
            writer.WriteOptimized(graphElements.Count);
            
            List<String> graphElementStreams = new List<String>();
            var partitions = Partitioner.Create(0, graphElements.Count);
            Parallel.ForEach(
                partitions,
                () => String.Empty,
                (range, loopstate, initialValue) =>
                    {
                        String partitionFileName = path + "_graphElements_" + range.Item1 + "_to_" + range.Item2;
                
                        //create file for range
                        var partitionFile = File.Create(partitionFileName, Constants.BufferSize, FileOptions.SequentialScan);
                        SerializationWriter partitionWriter = new SerializationWriter(partitionFile);
                        
                        for (int i = range.Item1; i < range.Item2; i++) 
                        {
                            var aGraphElement = graphElements[i];
                    
                            //there can be nulls
                            if (aGraphElement == null) 
                            {
                                writer.WriteObject (null);
                                continue;
                            }
                            
                            //code if it is an vertex or an edge
                            if (aGraphElement is VertexModel) 
                            {
                                WriteVertex((VertexModel)aGraphElement, partitionWriter);
                            }
                            else
                            {
                                WriteEdge((EdgeModel)aGraphElement, partitionWriter);
                            }
                        }
                
                        if (partitionWriter != null) 
                        {
                            partitionWriter.Flush();
                            partitionWriter.Close();
                        }
            
                        if (partitionFile != null) 
                        {
                            partitionFile.Flush();
                            partitionFile.Close();
                        }
                        
                        return partitionFileName;

                    },
                delegate(String rangeFileStream)
                    {
                        lock (graphElementStreams)
                        {
                            graphElementStreams.Add(rangeFileStream);
                        }
                    });
            
            writer.WriteOptimized(graphElementStreams.Count);
            foreach (var aFileStreamName in graphElementStreams) 
            {
                writer.WriteOptimized(aFileStreamName);    
            }
            
            #endregion
            
            #region indices
            
            List<String> indexfileStreamNames = new List<String>();
            Parallel.ForEach(
                indices,
                () => String.Empty,
                (indexKV, loopstate, initialValue) =>
                    {
                        String indexFileName = path + "_index_" + indexKV.Key;
                
                        var indexFile = File.Create(indexFileName, Constants.BufferSize, FileOptions.SequentialScan);
                        SerializationWriter indexWriter = new SerializationWriter(indexFile);
                       
                        indexKV.Value.Save(ref indexWriter);
                
                        if (indexWriter != null) 
                        {
                            indexWriter.Flush();
                            indexWriter.Close();
                        }
            
                        if (indexFile != null) 
                        {
                            indexFile.Flush();
                            indexFile.Close();
                        }
                        
                        return indexFileName;

                    },
                delegate(String indexFileName)
                    {
                        lock (indexfileStreamNames)
                        {
                            indexfileStreamNames.Add(indexFileName);
                        }
                    });
            
            writer.WriteOptimized(indexfileStreamNames.Count);
            foreach (var aIndexFileName in indexfileStreamNames) 
            {
                writer.WriteOptimized(aIndexFileName);    
            }
            
            #endregion
            
            if (writer != null) {
                writer.Flush();
                writer.Close();
            }
            
            if (file != null) {
                file.Flush();
                file.Close();
            }
        }
  
        #endregion
        
        #region private helper

        private static void LoadGraphElements (List<AGraphElement> graphElementsOfFallen8, List<String> graphElementStreams)
        {
            //create some futures to load as much as possible in parallel
            const TaskCreationOptions options = TaskCreationOptions.LongRunning;
            var f = new TaskFactory(CancellationToken.None, options, TaskContinuationOptions.None, TaskScheduler.Default);
            Task<List<EdgeSneakPeak>>[] tasks = (Task<List<EdgeSneakPeak>>[])Array.CreateInstance(typeof(Task<List<EdgeSneakPeak>>), graphElementStreams.Count);
            
            for (int i = 0; i < graphElementStreams.Count; i++)
            {
                tasks[i] = f.StartNew(() => LoadAGraphElementBunch(graphElementStreams[i], graphElementsOfFallen8));
            }

            Task.WaitAll(tasks);   
        }

        private static List<EdgeSneakPeak> LoadAGraphElementBunch (string graphElementBunchPath, List<AGraphElement> graphElementsOfFallen8)
        {
            return null;
        }
  
        /// <summary>
        /// Writes A graph element.
        /// </summary>
        /// <param name='graphElement'>
        /// Graph element.
        /// </param>
        /// <param name='writer'>
        /// Writer.
        /// </param>
        private static void WriteAGraphElement (AGraphElement graphElement, SerializationWriter writer)
        {
            writer.WriteOptimized(graphElement.Id);
            writer.WriteOptimized(graphElement.CreationDate);
            writer.WriteOptimized(graphElement.ModificationDate);
            
            List<PropertyContainer> properties = new List<PropertyContainer>(graphElement.GetAllProperties());
            writer.WriteOptimized(properties.Count);
            foreach (var aProperty in properties) 
            {
                writer.WriteOptimized(aProperty.PropertyId);
                writer.WriteObject(aProperty.Value);
            }
        }
        
        /// <summary>
        /// Writes the vertex.
        /// </summary>
        /// <param name='vertex'>
        /// Vertex.
        /// </param>
        /// <param name='writer'>
        /// Writer.
        /// </param>
        private static void WriteVertex (VertexModel vertex, SerializationWriter writer)
        {
            writer.WriteOptimized(1);// 1 for vertex
            WriteAGraphElement(vertex, writer);
            
            #region edges
            
            var outgoingEdges = vertex.GetOutgoingEdges();
            if(outgoingEdges == null)
            {
                writer.WriteOptimized(0);
            }
            else 
            {
                writer.WriteOptimized(outgoingEdges.Count);
                foreach(var aOutEdgeProperty in outgoingEdges)
                {
                    writer.WriteOptimized(aOutEdgeProperty.EdgePropertyId);
                    writer.WriteOptimized(aOutEdgeProperty.EdgeProperty.Count);
                    foreach(var aOutEdge in aOutEdgeProperty.EdgeProperty)
                    {
                        writer.WriteOptimized(aOutEdge.Id);
                    }
                }
            }
            
            var incomingEdges = vertex.GetIncomingEdges();
            if(incomingEdges == null)
            {
                writer.WriteOptimized(0);
            }
            else 
            {
                writer.WriteOptimized(incomingEdges.Count);
                foreach(var aIncEdgeProperty in incomingEdges)
                {
                    writer.WriteOptimized(aIncEdgeProperty.EdgePropertyId);
                    writer.WriteOptimized(aIncEdgeProperty.IncomingEdges.Count);
                    foreach(var aIncEdge in aIncEdgeProperty.IncomingEdges)
                    {
                        writer.WriteOptimized(aIncEdge.Id);
                    }
                }
            }
            
            #endregion
        }
  
        /// <summary>
        /// Writes the edge.
        /// </summary>
        /// <param name='edge'>
        /// Edge.
        /// </param>
        /// <param name='writer'>
        /// Writer.
        /// </param>
        private static void WriteEdge (EdgeModel edge, SerializationWriter writer)
        {
            writer.WriteOptimized(0);//0 for edge
            WriteAGraphElement(edge, writer);
            writer.WriteOptimized(edge.SourceVertex.Id);
            writer.WriteOptimized(edge.TargetVertex.Id);
        }
        
        #endregion
    }
}

