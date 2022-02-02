﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Archicad.Communication.Commands
{
	internal sealed class GetElementsType : ICommand<Dictionary<Type, IEnumerable<string>>>
	{
		#region --- Classes ---

		[JsonObject (MemberSerialization.OptIn)]
		public sealed class Parameters
		{
			#region --- Fields ---

			[JsonProperty ("elementIds")]
			private IEnumerable<string> ElementIds { get; }

			#endregion


			#region --- Ctor \ Dtor ---

			public Parameters (IEnumerable<string> elementIds)
			{
				ElementIds = elementIds;
			}

			#endregion
		}


		[JsonObject (MemberSerialization.OptIn)]
		private sealed class Result
		{
			#region --- Fields ---

			[JsonProperty ("elementTypes")]
			public IEnumerable<TypeDescription> ElementTypes { get; private set; }

			#endregion
		}


		[JsonObject(MemberSerialization.OptIn)]
		private sealed class TypeDescription
		{
			[JsonProperty("elementId")]
			public string ElementId { get; private set; }

			[JsonProperty("elementType")]
			public string ElementType { get; private set; }

		}

		#endregion


		#region --- Fields ---

		private IEnumerable<string> ElementIds { get; }

		#endregion


		#region --- Ctor \ Dtor ---

		public GetElementsType (IEnumerable<string> elementIds)
		{
			ElementIds = elementIds;
		}

		#endregion


		#region --- Functions ---

		public async Task<Dictionary<Type, IEnumerable<string>>> Execute ()
		{
			Result result = await HttpCommandExecutor.Execute<Parameters, Result> ("GetElementTypes", new Parameters (ElementIds));
			return result.ElementTypes.GroupBy (row => ElementTypeProvider.GetTypeByName (row.ElementType)).ToDictionary (group => group.Key, group => group.Select (x => x.ElementId));
		}

		#endregion
	}
}