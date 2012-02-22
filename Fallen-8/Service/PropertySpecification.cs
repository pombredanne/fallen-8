// 
//  PropertySpecification.cs
//  
//  Author:
//       Henning Rauch <Henning@RauchEntwicklung.biz>
//  
//  Copyright (c) 2012 Henning Rauch
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, version 3 of the License.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Runtime.Serialization;

namespace Fallen8.API.Service
{
	/// <summary>
    ///   The property specification
    /// </summary>
    [DataContract]
    public sealed class PropertySpecification
    {
        /// <summary>
        ///   The type name
        /// </summary>
        [DataMember]
        public String TypeName { get; set; }

        /// <summary>
        ///   The property string representation
        /// </summary>
        [DataMember]
        public String Property { get; set; }
    }
}