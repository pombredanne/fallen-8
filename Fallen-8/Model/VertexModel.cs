﻿// 
// VertexModel.cs
//  
// Author:
//       Henning Rauch <Henning@RauchEntwicklung.biz>
// 
// Copyright (c) 2011 Henning Rauch
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
using System.Linq;
using Fallen8.API.Error;

namespace Fallen8.API.Model
{
    /// <summary>
    /// Vertex model.
    /// </summary>
    public sealed class VertexModel : AGraphElement
    {
        #region Data
        
        /// <summary>
        /// The out edges.
        /// </summary>
        private List<OutEdgeContainer> _outEdges;
        
        /// <summary>
        /// The in edges.
        /// </summary>
        private List<IncEdgeContainer> _inEdges;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="VertexModel"/> class.
        /// </summary>
        /// <param name='id'>
        /// Identifier.
        /// </param>
        /// <param name='creationDate'>
        /// Creation date.
        /// </param>
        /// <param name='properties'>
        /// Properties.
        /// </param>
        public VertexModel(Int32 id, DateTime creationDate, List<PropertyContainer> properties)
            : base(id, creationDate, properties)
        {
        }
        
        #endregion
        
        #region internal methods

        /// <summary>
        /// Adds the out edge.
        /// </summary>
        /// <param name='edgePropertyId'>
        /// Edge property identifier.
        /// </param>
        /// <param name='outEdge'>
        /// Out edge.
        /// </param>
        /// <exception cref='CollisionException'>
        /// Is thrown when the collision exception.
        /// </exception>
        internal void AddOutEdge(Int32 edgePropertyId, EdgeModel outEdge)
        {
            if (WriteResource())
            {
                if (_outEdges == null)
                {
                    _outEdges = new List<OutEdgeContainer>();
                }
    
                Boolean foundSth = false;
                
                foreach(var aEdgeProperty in _outEdges)
                {
                    if (aEdgeProperty.EdgePropertyId == edgePropertyId) 
                    {
                        aEdgeProperty.EdgeProperty.Add(outEdge);
                        
                        foundSth = true;
                        
                        break;
                    }
                }
                
                if (!foundSth) 
                {
                    _outEdges.Add(new OutEdgeContainer { EdgePropertyId = edgePropertyId, EdgeProperty = new List<EdgeModel> {outEdge}});
                }
                
                FinishWriteResource();

                return;
            }


            throw new CollisionException();
        }

        /// <summary>
        /// Adds the out edges.
        /// </summary>
        /// <param name='outEdges'>
        /// Out edges.
        /// </param>
        /// <exception cref='CollisionException'>
        /// Is thrown when the collision exception.
        /// </exception>
        internal void SetOutEdges(List<OutEdgeContainer> outEdges)
        {
            if (WriteResource())
            {
                _outEdges = outEdges;
                FinishWriteResource();

                return;
            }

            throw new CollisionException ();
        }
        
        /// <summary>
        /// Adds the incoming edge.
        /// </summary>
        /// <param name='edgePropertyId'>
        /// Edge property identifier.
        /// </param>
        /// <param name='incomingEdge'>
        /// Incoming edge.
        /// </param>
        /// <exception cref='CollisionException'>
        /// Is thrown when the collision exception.
        /// </exception>
        internal void AddIncomingEdge(Int32 edgePropertyId, EdgeModel incomingEdge)
        {
            if (WriteResource())
            {
                if (_inEdges == null)
                {
                    _inEdges = new List<IncEdgeContainer>();
                }
    
                Boolean foundSth = false;
                
                foreach(var aIncomingEdge in _inEdges)
                {
                    if (aIncomingEdge.EdgePropertyId == edgePropertyId) 
                    {
                        aIncomingEdge.IncomingEdges.Add(incomingEdge);
                        foundSth = true;
                        break;
                    }
                }
                
                if (!foundSth) 
                {
                    _inEdges.Add(new IncEdgeContainer { EdgePropertyId = edgePropertyId, IncomingEdges = new List<EdgeModel> {incomingEdge}});    
                }
                
                FinishWriteResource();

                return;
            }

            throw new CollisionException();
        }
  
        /// <summary>
        /// Gets the incoming edges.
        /// </summary>
        /// <returns>
        /// The incoming edges.
        /// </returns>
        internal List<IncEdgeContainer> GetIncomingEdges()
        {
            List<IncEdgeContainer> result = null;
            
            if (ReadResource())
            {
                if (_inEdges != null)
                {
                    result = new List<IncEdgeContainer>(_inEdges);
                }
                    
                FinishReadResource();

                return result;
            }

            throw new CollisionException();
        }
        
        /// <summary>
        /// Gets the outgoing edges.
        /// </summary>
        /// <returns>
        /// The outgoing edges.
        /// </returns>
        internal List<OutEdgeContainer> GetOutgoingEdges()
        {
            List<OutEdgeContainer> result = null;
            
            if (ReadResource())
            {
                if (_outEdges != null)
                {
                    result = new List<OutEdgeContainer>(_outEdges);
                }
                    
                FinishReadResource();

                return result;
            }

            throw new CollisionException();
        }
        
        #endregion

        #region IVertexModel implementation

        /// <summary>
        /// Gets all neighbors.
        /// </summary>
        /// <returns>
        /// The neighbors.
        /// </returns>
        public List<VertexModel> GetAllNeighbors()
        {
            if (ReadResource())
            {
                var neighbors = new List<VertexModel>();

                if (_outEdges != null)
                {
                    foreach (var aOutEdge in _outEdges)
                    {
                        neighbors.AddRange(aOutEdge.EdgeProperty.Select(_ => _.TargetVertex));
                    }
                }

                if (_inEdges != null && _inEdges.Count > 0)
                {
                    foreach (var aInEdge in _inEdges)
                    {
                        neighbors.AddRange(aInEdge.IncomingEdges.Select(_ => _.SourceVertex));
                    }

                }
                FinishReadResource();

                return neighbors;
            }

            throw new CollisionException();
        }

        /// <summary>
        /// Gets the incoming edge identifiers.
        /// </summary>
        /// <returns>
        /// The incoming edge identifiers.
        /// </returns>
        public List<Int32> GetIncomingEdgeIds()
        {
            if (ReadResource())
            {
                var inEdges = new List<Int32>();

                if (_inEdges != null)
                {
                    inEdges.AddRange(_inEdges.Select(_ => _.EdgePropertyId));
                }
                FinishReadResource();

                return inEdges;
            }

            throw new CollisionException ();
        }

        /// <summary>
        /// Gets the outgoing edge identifiers.
        /// </summary>
        /// <returns>
        /// The outgoing edge identifiers.
        /// </returns>
        public List<Int32> GetOutgoingEdgeIds()
        {
            if (ReadResource())
            {
                var outEdges = new List<Int32>();

                if (_outEdges != null)
                {
                    outEdges.AddRange(_outEdges.Select(_ => _.EdgePropertyId));
                }
                FinishReadResource();

                return outEdges;
            }

            throw new CollisionException();
        }

        /// <summary>
        /// Tries to get an out edge.
        /// </summary>
        /// <returns>
        /// <c>true</c> if something was found; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='result'>
        /// Result.
        /// </param>
        /// <param name='edgePropertyId'>
        /// Edge property identifier.
        /// </param>
        public Boolean TryGetOutEdge(out List<EdgeModel> result, Int32 edgePropertyId)
        {
            if (ReadResource())
            {
                var foundSth = false;
                result = null; 
                
                if (_outEdges != null)
                {
                    foreach (var aOutEdge in _outEdges) 
                    {
                        if (aOutEdge.EdgePropertyId == edgePropertyId) 
                        {
                            result = aOutEdge.EdgeProperty;
                            foundSth = true;
                            break;
                        } 
                    }
                }
                
                FinishReadResource();

                return foundSth;
            }

            throw new CollisionException ();
        }

        /// <summary>
        /// Tries to get in edges.
        /// </summary>
        /// <returns>
        /// <c>true</c> if something was found; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='result'>
        /// Result.
        /// </param>
        /// <param name='edgePropertyId'>
        /// Edge property identifier.
        /// </param>
        public Boolean TryGetInEdges(out List<EdgeModel> result, Int32 edgePropertyId)
        {
            if (ReadResource())
            {
                result = null;
                Boolean foundSth = false;
                
                if (_inEdges != null)
                {
                    foreach (var aIncomingEdgeProperty in _inEdges) 
                    {
                        if (aIncomingEdgeProperty.EdgePropertyId == edgePropertyId) 
                        {
                            result = new List<EdgeModel>(aIncomingEdgeProperty.IncomingEdges);
                            foundSth = true;
                            break;
                        }
                    }
                }
                
                FinishReadResource();

                return foundSth;
            }

            throw new CollisionException ();
        }

        #endregion

        #region Equals Overrides

        public override int GetHashCode()
        {
            return Id;
        }

        #endregion
    }
}
