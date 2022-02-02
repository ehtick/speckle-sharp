using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archicad.Communication;
using Speckle.Core.Models;

namespace Archicad.Converters
{
  public sealed class Ceiling : IConverter
  {
    public Type Type => typeof(Objects.BuiltElements.Archicad.Ceiling);

    public async Task<List<string>> ConvertToArchicad(IEnumerable<Base> elements, CancellationToken token)
    {
      var ceilings = elements.OfType<Objects.BuiltElements.Archicad.Ceiling>();
      var result =
        await AsyncCommandProcessor.Execute(
          new Communication.Commands.CreateCeiling(ceilings), token);

      return result.ToList();
    }

    public async Task<List<Base>> ConvertToSpeckle(IEnumerable<Model.ElementModelData> elements,
      CancellationToken token)
    {
      var data = await AsyncCommandProcessor.Execute(
        new Communication.Commands.GetCeilingData(elements.Select(e => e.elementId)), token);

      var ceilings = new List<Base>();
      foreach (var ceiling in data)
      {
        ceiling.displayValue = Operations.ModelConverter.MeshToSpeckle(elements
          .First(e => e.elementId == ceiling.elementId)
          .model);
        ceilings.Add(ceiling);
      }

      return ceilings;
    }
  }
}