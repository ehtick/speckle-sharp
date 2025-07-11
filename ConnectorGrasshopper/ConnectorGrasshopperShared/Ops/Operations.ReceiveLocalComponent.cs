using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperAsyncComponent;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops;

public class ReceiveLocalComponent : SelectKitAsyncComponentBase<ReceiveLocalComponent>
{
  public bool foundKit;

  public ReceiveLocalComponent()
    : base(
      "Local Receive",
      "LR",
      "Receives data locally, without the need of a Speckle Server. NOTE: updates will not be automatically received.",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.SEND_RECEIVE
    )
  {
    BaseWorker = new ReceiveLocalWorker(this);
  }

  public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

  protected override Bitmap Icon => Resources.LocalReceiver;

  public override Guid ComponentGuid => new("43E22B36-891B-4478-8A4E-2338272EA3B3");

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("localDataId", "id", "ID of the local data sent.", GH_ParamAccess.item);
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter("Data", "D", "Data to send.", GH_ParamAccess.tree);
  }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    Menu_AppendSeparator(menu);
    Menu_AppendItem(menu, "Select the converter you want to use:", null, null, false);
    var kits = KitManager.GetKitsWithConvertersForApp(Utilities.GetVersionedAppName());

    foreach (var kit in kits)
    {
      Menu_AppendItem(
        menu,
        $"{kit.Name} ({kit.Description})",
        (s, e) =>
        {
          SetConverterFromKit(kit.Name);
        },
        true,
        kit.Name == Kit.Name
      );
    }

    base.AppendAdditionalMenuItems(menu);
  }

  public override void AddedToDocument(GH_Document document)
  {
    SetDefaultKitAndConverter();
    base.AddedToDocument(document);
  }

  public void SetConverterFromKit(string kitName)
  {
    if (kitName == Kit.Name)
    {
      return;
    }

    Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
    Converter = Kit.LoadConverter(Utilities.GetVersionedAppName());
    Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
    SpeckleGHSettings.OnMeshSettingsChanged += (sender, args) =>
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);

    Message = $"Using the {Kit.Name} Converter";
    ExpireSolution(true);
  }

  private void SetDefaultKitAndConverter()
  {
    try
    {
      Kit = KitManager.GetDefaultKit();
      Converter = Kit.LoadConverter(Utilities.GetVersionedAppName());
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      SpeckleGHSettings.OnMeshSettingsChanged += (sender, args) =>
        Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      Converter.SetContextDocument(Loader.GetCurrentDocument());
      foundKit = true;
    }
    catch (Exception e) when (!e.IsFatal())
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      foundKit = false;
    }
  }

  protected override void SolveInstance(IGH_DataAccess DA)
  {
    if (!foundKit)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No kit found on this machine.");
      return;
    }
    base.SolveInstance(DA);
    if (DA.Iteration == 0)
    {
      Tracker.TrackNodeRun();
    }
  }
}

public class ReceiveLocalWorker : WorkerInstance<ReceiveLocalComponent>
{
  private GH_Structure<IGH_Goo> data;
  private string localDataId;

  public ReceiveLocalWorker(
    ReceiveLocalComponent parent,
    string id = "baseWorker",
    CancellationToken cancellationToken = default
  )
    : base(parent, id, cancellationToken) { }

  private List<(GH_RuntimeMessageLevel, string)> RuntimeMessages { get; set; } = new();

  public override WorkerInstance<ReceiveLocalComponent> Duplicate(string id, CancellationToken cancellationToken)
  {
    return new ReceiveLocalWorker(Parent, id, cancellationToken);
  }

  public override async Task DoWork(Action<string, double> ReportProgress, Action Done)
  {
    try
    {
      Parent.Message = "Receiving...";
      var Converter = Parent.Converter;

      Base @base = null;

      try
      {
        @base = Operations.Receive(localDataId, disposeTransports: true).Result;
      }
      catch (Exception e) when (!e.IsFatal())
      {
        RuntimeMessages.Add((GH_RuntimeMessageLevel.Warning, "Failed to receive local data."));
        Done();
        return;
      }

      data = Utilities.ConvertToTree(Converter, @base, Parent.AddRuntimeMessage);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // If we reach this, something happened that we weren't expecting...
      SpeckleLog.Logger.Error(ex, "Failed during execution of {componentName}", this.GetType());
      RuntimeMessages.Add((GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + ex.ToFormattedString()));
      Parent.Message = "Error";
    }
    Done();
  }

  public override void SetData(IGH_DataAccess DA)
  {
    if (data != null)
    {
      DA.SetDataTree(0, data);
    }

    foreach (var (level, message) in RuntimeMessages)
    {
      Parent.AddRuntimeMessage(level, message);
    }

    Parent.Message = "Done";
  }

  public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
  {
    DA.GetData(0, ref localDataId);
    data = null;
  }
}
