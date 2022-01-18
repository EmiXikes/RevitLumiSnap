using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
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

            List<BuiltInCategory> snapCats = new List<BuiltInCategory>();

            snapCats.Add(BuiltInCategory.OST_Roofs);
            snapCats.Add(BuiltInCategory.OST_Ceilings);
            snapCats.Add(BuiltInCategory.OST_Floors);
            snapCats.Add(BuiltInCategory.OST_Stairs);

            FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
            List<Level> rvtLevels = levelCollector.OfClass(typeof(Level)).OfType<Level>().OrderBy(lev => lev.Elevation).ToList();

            string ProxyAnnoTagName = "EpicAnnotationProxy";

            var epicAnotationInstances = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>().Where(x => x.Name == ProxyAnnoTagName).ToList();

            Transaction trans = new Transaction(doc);
            trans.Start("LumiSnap");

            #region Getting saved settings
            // getting saved settings
            LumiSnapSettingsStorage MySettingStorage = new LumiSnapSettingsStorage();
            LumiSnapSettingsData MySettings = MySettingStorage.ReadSettings(doc);
            if (MySettings == null)
            {
                // Default Values
                MySettings = new LumiSnapSettingsData()
                {
                    DistanceFwd = 1500,
                    DistanceRev = 500,
                    ViewName = "Epic LumiSnap ceiling check View"
                };
            }

            #endregion

            #region creating snap check View
            // Getting or creating a new view and setting correct VV settings
            FilteredElementCollector colView = new FilteredElementCollector(doc);
            View3D ceilCheckView = colView.OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault<View3D>(x => x.Name == MySettings.ViewName);

            if (ceilCheckView == null)
            {
                ceilCheckView = CreateNewView(doc, MySettings.ViewName);
            }

            SetVisibleCats(doc, snapCats, ceilCheckView);

            SetVisibleLink(doc, MySettings, ceilCheckView);


            #endregion

            var selection = uidoc.Selection.GetElementIds();
            Level SelectedLevel = (Level)rvtLevels[0];

            foreach (ElementId id in selection)
            {
                Element selectedElement = uidoc.Document.GetElement(id);
                FamilyInstance selectedFamInstance = (FamilyInstance)selectedElement;

                #region Parameters to carry over
                // Getting parameters to be carried over
                string paramElConnection = "";
                var p = selectedFamInstance.get_Parameter(new System.Guid("41a9849c-f9a0-48fd-8b79-9a51cb222a8e"));
                if (p != null)
                {
                    paramElConnection = p.AsString();
                }
                #endregion

                // Getting position and rotation
                var trf = selectedFamInstance.GetTransform();
                double elmAngle3 = Math.Atan2(trf.BasisY.X, trf.BasisX.X);// * 180 / Math.PI;

                Debug.Print(String.Format("Rotation Angle: {0}",
                                            elmAngle3 * 180 / Math.PI
                                            ));

                // Adjusting position based on found ceilings
                // Creating new RefPlane for new postition
                double reverseSearchDistance = 500 / mmInFt;
                XYZ initialPoint = (selectedElement.Location as LocationPoint).Point;
                XYZ snapPoint = GetSnapSurfacePoint(
                    ceilCheckView,
                    initialPoint,
                    MySettings.DistanceRev / mmInFt,
                    MySettings.DistanceFwd / mmInFt,
                    snapCats,
                    out Reference snapRef);

                // Get level that corresponds to actual location of the element
                foreach (Level lvl in rvtLevels)
                {
                    if ((selectedElement.Location as LocationPoint).Point.Z < lvl.Elevation)
                    {
                        break;
                    }
                    SelectedLevel = lvl;
                }

                // Create new Reference plane

                string newElevationAtLvl =String.Format("{0}", Math.Round( (snapPoint.Z - SelectedLevel.Elevation) * mmInFt  ));
                string newRefPlaneName = String.Format(
                    "EpicLum_##{0}##_EL{1}",
                    SelectedLevel.Name, 
                    newElevationAtLvl
                    );

                ReferencePlane newRefPlane = CreateNewRefPlane(doc, snapPoint.Z, newRefPlaneName);



                // creating new family instance
                var famLoc = (selectedFamInstance.Location as LocationPoint).Point;

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

                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<Element> linkedDocIdSet =
                  collector
                  .OfCategory(BuiltInCategory.OST_RvtLinks)
                  .OfClass(typeof(RevitLinkType))
                  .ToElements();


                //Document linkedDoc = linkedDocs.FirstOrDefault(d=>d.);
                //var eID = snapRef.LinkedElementId;
                //Element E = MySettings.LinkId.GetElement(eID);

                Debug.Print(
                    String.Format("\nLum [{0}] snapped to {1} [{2}] on '{3}' at {4}",
                    instance.Id,
                    snapRef.ElementId,
                    snapRef.ElementId,
                    SelectedLevel.Name,
                    newElevationAtLvl));

                // resetting parameters
                instance.get_Parameter(new System.Guid("41a9849c-f9a0-48fd-8b79-9a51cb222a8e")).Set(paramElConnection);

                // Updating proxy tags
                foreach (var annoInstance in epicAnotationInstances)
                {
                    Entity retrievedEntity = annoInstance.GetEntity(LumiAnnoProxySchema.GetSchema());
                    IList<ElementId> assignedLumIds = retrievedEntity.Get<IList<ElementId>>("LumiIds");
                    IList<ElementId> newAssignedLums = new List<ElementId>();
                    

                    foreach (ElementId assignedId in assignedLumIds)
                    {
                        if (id == assignedId)
                        {
                            newAssignedLums.Add(instance.Id);
                        }
                        else
                        {
                            newAssignedLums.Add(assignedId);
                        }
                    }

                    Entity entity = new Entity(LumiAnnoProxySchema.GetSchema());
                    entity.Set<IList<ElementId>>("LumiIds", newAssignedLums);
                    annoInstance.SetEntity(entity);

                }



                // deleting the old element
                doc.Delete(id);
            }

            trans.Commit();
            return Result.Succeeded;

        }




    }

}
