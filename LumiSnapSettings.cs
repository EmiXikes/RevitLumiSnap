using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LumiSnap.VIEW;
using LumiSnap.VM;
using System.Collections.Generic;
using System.Windows;

namespace EpicLumiSnap
{
    [Transaction(TransactionMode.Manual)]
    public class LumiSnapSettings : HelperOps, IExternalCommand
    {
        UIApplication uiapp;
        UIDocument uidoc;
        Autodesk.Revit.ApplicationServices.Application app;
        Document doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument;
            app = uiapp.Application;
            doc = uidoc.Document;

            Transaction trans = new Transaction(doc);
            trans.Start("LumiSnap Settings");

            LumiSnapSettingsStorage MySettingStorage = new LumiSnapSettingsStorage();
            LumiSnapSettingsData MySettings = MySettingStorage.ReadSettings(doc);

            if (MySettings == null)
            {
                // Default Values
                MySettings = new LumiSnapSettingsData()
                {
                    DistanceFwd = 1500,
                    DistanceRev = 500,
                    ViewName = "EpicC"
                };
            }


            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<ElementId> linkedDocIdSet =
              collector
              .OfCategory(BuiltInCategory.OST_RvtLinks)
              .OfClass(typeof(RevitLinkType))
              .ToElementIds();


            List<Document> linkedDocs = new List<Document>();
            List<RevitLinkType> linkedDocTypes = new List<RevitLinkType>();

            foreach (ElementId linkedFileId in linkedDocIdSet)
            {
                RevitLinkType link = doc.GetElement(linkedFileId) as RevitLinkType;
                linkedDocTypes.Add(link);

            }

            foreach (Document LinkedDoc in uiapp.Application.Documents)
            {
                if (LinkedDoc.IsLinked)
                {
                    linkedDocs.Add(LinkedDoc);
                }
            }




            // UI
            Window uiWin = new Window();
            uiWin.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            uiWin.Width = 250; uiWin.Height = 300;
            uiWin.ResizeMode = ResizeMode.NoResize;
            uiWin.Title = "LumiSnap Settings";

            LumiSnapSettingsVM uiData = new LumiSnapSettingsVM()
            {
                DistanceRev = MySettings.DistanceRev,
                DistanceFwd = MySettings.DistanceFwd,
                CollisionViewName = MySettings.ViewName,
                CollisionLinks = linkedDocTypes,

            };

            int ind = 0;
            foreach (var link in linkedDocTypes)
            {
                if (MySettings.LinkId == link.Id)
                {
                    ind = linkedDocTypes.IndexOf(link);
                    break;
                }
            }
            uiData.SelectedIndex = ind;

            uiData.OnRequestClose += (s, e) => uiWin.Close();

            uiWin.Content = new LumiSnapSettingsUI();
            uiWin.DataContext = uiData;
            uiWin.ShowDialog();

            if (uiData.RevitTransactionResult == Result.Cancelled)
            {
                trans.Dispose();
                return Result.Cancelled;
            }

            MySettings.DistanceRev = uiData.DistanceRev;
            MySettings.DistanceFwd = uiData.DistanceFwd;
            MySettings.ViewName = uiData.CollisionViewName;
            if (uiData.CollisionLinks != null)
            {
                MySettings.LinkId = uiData.SelectedLink.Id;
            }
            else
            {
                MySettings.LinkId = ElementId.InvalidElementId;
            }
            

            MySettingStorage.WriteSettings(doc, MySettings);

            trans.Commit();
            return Result.Succeeded;
        }



    }

}
