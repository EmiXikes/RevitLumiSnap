using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LumiSnap.VM;
using LumiSnap.VIEW;
using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;

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
                    DistanceFwd = "1500",
                    DistanceRev = "500",
                    ViewName = "EpicC"
                };
            }

            List<Document> linkedDocs = new List<Document>();
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
            uiWin.Width = 650; uiWin.Height = 300;
            uiWin.ResizeMode = ResizeMode.NoResize;
            uiWin.Title = "LumiSnap Settings";

            LumiSnapSettingsVM uiData = new LumiSnapSettingsVM()
            {
                DistanceRev = MySettings.DistanceRev,
                DistanceFwd = MySettings.DistanceFwd,
                CollisionViewName = MySettings.ViewName,
                CollisionLinks = linkedDocs,

            };
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

            MySettingStorage.WriteSettings(doc, MySettings);

            trans.Commit();
            return Result.Succeeded;
        }



    }

}
