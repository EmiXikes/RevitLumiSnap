using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
//using EpicLumImporter.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace EpicLumiSnap
{
    [Transaction(TransactionMode.Manual)]
    public class LumiSnap : HelperOps, IExternalCommand
    {

        double mmInFt = 304.8;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;

            string ceilCheckViewName = "EpicC";

            FilteredElementCollector colView = new FilteredElementCollector(doc);
            View3D ceilCheckView = colView.OfClass(typeof(View3D)).Cast<View3D>().First<View3D>(x => x.Name == ceilCheckViewName);

            FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
            List<Element> rvtLevels = levelCollector.OfClass(typeof(Level)).ToElements().ToList();
            List<string> rvtLevelsStr = new List<string>();
            foreach (Level level in rvtLevels)
            {
                rvtLevelsStr.Add(level.Name);
            }

            List<Document> linkedDocs = new List<Document>();
            List<string> linkedDocsStr = new List<string>();
            foreach (Document LinkedDoc in uiapp.Application.Documents)
            {
                if (LinkedDoc.IsLinked)
                {
                    linkedDocs.Add(LinkedDoc);
                    linkedDocsStr.Add(System.IO.Path.GetFileName(LinkedDoc.PathName));
                }
            }

            Transaction trans = new Transaction(doc);
            trans.Start("LumiSnap");

            Level SelectedLevel = (Level)rvtLevels[0];

            var selection = uidoc.Selection.GetElementIds();

            double tolerance = 20 / mmInFt;

            foreach (ElementId id in selection)
            {
                Element selectedElement = uidoc.Document.GetElement(id);
                FamilyInstance selectedFamInstance = (FamilyInstance)selectedElement;

                // Getting parameters to be carried over
                string paramElConnection = selectedFamInstance.get_Parameter(new System.Guid("41a9849c-f9a0-48fd-8b79-9a51cb222a8e")).AsString();

                // Getting position and rotation
                var trf = selectedFamInstance.GetTransform();
                double elmAngle3 = Math.Atan2(trf.BasisY.X, trf.BasisX.X);// * 180 / Math.PI;

                Debug.Print(String.Format("Rotation Angle: {0}",
                                            elmAngle3 * 180 / Math.PI
                                            ));

                // Adjusting position based on found ceilings
                // Creating new RefPlane for new postition
                double reverseSearchDistance = 500 / mmInFt;
                XYZ ePoint = (selectedElement.Location as LocationPoint).Point;
                XYZ cPoint = GetCeilingPoint(ceilCheckView, ePoint, reverseSearchDistance);

                string newRefPlaneName = String.Format(
                    "EpicLumWorkPlane_{0}_EL{1}",
                    SelectedLevel.Name, (cPoint.Z - SelectedLevel.Elevation) * mmInFt);

                ReferencePlane newRefPlane = CreateNewRefPlane(doc, cPoint.Z, newRefPlaneName);

                Debug.Print("\nNew RP created: " + newRefPlaneName);

                var famLoc = (selectedFamInstance.Location as LocationPoint).Point;

                // creating new family instance
                var familySymbol = selectedFamInstance.Symbol;
                familySymbol.Activate();
                FamilyInstance instance = doc.Create.NewFamilyInstance(
                    newRefPlane.GetReference(), famLoc, new XYZ(1, 0, 0), familySymbol);

                XYZ axisPoint1 = famLoc;
                XYZ axisPoint2 = new XYZ(
                    famLoc.X,
                    famLoc.Y,
                    famLoc.Z + 1
                    );

                Line axis = Line.CreateBound(axisPoint1, axisPoint2);
                double setRotation = elmAngle3;// * (Math.PI / 180); 

                ElementTransformUtils.RotateElement(
                    doc,
                    instance.Id,
                    axis,
                    setRotation);

                // resetting parameters
                instance.get_Parameter(new System.Guid("41a9849c-f9a0-48fd-8b79-9a51cb222a8e")).Set(paramElConnection);



                // deleting the old one
                doc.Delete(id);
            }


            trans.Commit();
            return Result.Succeeded;

        }
    }

}
