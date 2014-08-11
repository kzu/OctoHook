#region BSD License
/* 
Copyright (c) 2011, NETFx
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, 
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list 
  of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this 
  list of conditions and the following disclaimer in the documentation and/or other 
  materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be 
  used to endorse or promote products derived from this software without specific 
  prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH 
DAMAGE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// AddRange extension method for ICollection&lt;T&gt;.
/// </summary>
/// <nuget id="netfx-System.Collections.Generic.CollectionAddRange"/>
internal static partial class CollectionAddRangeExtension
{
	/// <summary>
	/// Adds the elements to the end of the <see cref="ICollection{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the items that the collection contains. Inferred from other parameters.</typeparam>
	/// <param name="source">The collection to add the elements to.</param>
	/// <param name="items">The items to add to the collection.</param>
	public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
	{
		Guard.NotNull(() => source, source);
		Guard.NotNull(() => items, items);

		foreach (var item in items)
		{
			source.Add(item);
		}
	}

	/// <summary>
	/// Adds the elements to the end of the <see cref="ICollection{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of the items that the collection contains. Inferred from other parameters.</typeparam>
	/// <param name="source">The collection to add the elements to.</param>
	/// <param name="items">The items to add to the collection.</param>
	public static void AddRange<T>(this ICollection<T> source, params T[] items)
	{
		AddRange(source, ((IEnumerable<T>)items));
	}
}