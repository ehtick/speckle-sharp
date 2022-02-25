﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateCeiling : ICommand<IEnumerable<string>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("slabs")]
      private IEnumerable<Ceiling> Datas { get; }

      public Parameters(IEnumerable<Ceiling> datas)
      {
        Datas = datas;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationIds")]
      public IEnumerable<string> ApplicationIds { get; private set; }
    }

    private IEnumerable<Ceiling> Datas { get; }

    public CreateCeiling(IEnumerable<Ceiling> datas)
    {
      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateSlab", new Parameters(Datas));
      return result.ApplicationIds;
    }
  }
}