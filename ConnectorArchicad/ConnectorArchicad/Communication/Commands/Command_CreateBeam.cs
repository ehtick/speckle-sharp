using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class CreateBeam : ICommand<IEnumerable<ApplicationObject>>
{
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters
  {
    [JsonProperty("beams")]
    private IEnumerable<ArchicadBeam> Datas { get; }

    public Parameters(IEnumerable<ArchicadBeam> datas)
    {
      Datas = datas;
    }
  }

  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("applicationObjects")]
    public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }
  }

  private IEnumerable<ArchicadBeam> Datas { get; }

  public CreateBeam(IEnumerable<ArchicadBeam> datas)
  {
    foreach (var data in datas)
    {
      data.displayValue = null;
      data.baseLine = null;
    }

    Datas = datas;
  }

  public async Task<IEnumerable<ApplicationObject>> Execute()
  {
    var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateBeam", new Parameters(Datas));
    return result == null ? null : result.ApplicationObjects;
  }
}
