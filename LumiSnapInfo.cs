using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using EpicLumiSnap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLumiSnap
{
    [Transaction(TransactionMode.Manual)]
    public class LumiSnapInfo : HelperOps, IExternalCommand
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
            trans.Start("LumiSnap Info");

            System.Windows.Forms.MessageBox.Show("Epic Tools © EDGARS.M, DAINA EL", "About");

            trans.Commit();
            return Result.Succeeded;
        }
    }
}
