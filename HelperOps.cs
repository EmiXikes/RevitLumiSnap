using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLumiSnap
{
    [Transaction(TransactionMode.Manual)]
    public class HelperOps
    {
        public static ReferencePlane CreateNewRefPlane(Document doc, double wpElevation, string newRefPlaneName)
        {
            ReferencePlane refPlane;
            // Build now data before creation
            XYZ bubbleEnd = new XYZ(1, 0, wpElevation);   // bubble end applied to reference plane
            XYZ freeEnd = new XYZ(0, 1, wpElevation);    // free end applied to reference plane.
            XYZ thirdPnt = new XYZ(1, 1, wpElevation);   // 3rd point to define reference plane.  Third point should not be on the bubbleEnd-freeEnd line 

            // Create the reference plane in X-Y, applying the active view

            FilteredElementCollector planescollector = new FilteredElementCollector(doc);
            List<Element> WorkPlanes = planescollector.OfClass(typeof(ReferencePlane)).ToElements().ToList();

            var wp = WorkPlanes.FirstOrDefault(W => W.Name == newRefPlaneName);
            if (wp != null)
            {
                refPlane = (ReferencePlane)wp;
            }
            else
            {
                refPlane = doc.Create.NewReferencePlane2(bubbleEnd, freeEnd, thirdPnt, doc.ActiveView);
                refPlane.Name = newRefPlaneName;
            }

            return refPlane;
        }
        public static XYZ GetSnapSurfacePoint(View3D view3D, XYZ initialPosition, double revDistance, double fwdDistance, List<BuiltInCategory> builtInCats, out Reference snapRef)
        {

            XYZ initialDeltaPosition = initialPosition - new XYZ(0, 0, revDistance);
            XYZ rayDirection = new XYZ(0, 0, 1);

            //List<BuiltInCategory> builtInCats = new List<BuiltInCategory>();

            //builtInCats.Add(BuiltInCategory.OST_Roofs);
            //builtInCats.Add(BuiltInCategory.OST_Ceilings);
            //builtInCats.Add(BuiltInCategory.OST_Floors);
            //builtInCats.Add(BuiltInCategory.OST_Walls);
            //builtInCats.Add(BuiltInCategory.OST_Stairs);

            ElementMulticategoryFilter intersectFilter
              = new ElementMulticategoryFilter(builtInCats);


            ReferenceIntersector refIntersector;
            ReferenceWithContext referenceWithContext;

            refIntersector = new ReferenceIntersector(intersectFilter, FindReferenceTarget.Element, view3D);
            refIntersector.FindReferencesInRevitLinks = true;
            //refIntersector = new ReferenceIntersector(view3D) { FindReferencesInRevitLinks = true };
            referenceWithContext = refIntersector.FindNearest(initialDeltaPosition, rayDirection);

            var fref = refIntersector.Find(initialDeltaPosition, rayDirection);

            if (referenceWithContext != null)
            {
                //var i = referenceWithContext.GetType();
                snapRef = referenceWithContext.GetReference();
                return referenceWithContext.GetReference().GlobalPoint;
            }
            snapRef = null;
            return initialPosition;
        }

        public View3D CreateNewView(Document doc, string viewName)
        {
            var collector = new FilteredElementCollector(doc);
            var viewFamilyType = collector.OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
              .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

            View3D view3D = View3D.CreateIsometric(doc, viewFamilyType.Id);
            view3D.Name = viewName;
            view3D.Discipline = ViewDiscipline.Coordination;

            return view3D;
        }

        public static void SetVisibleCats(Document doc, List<BuiltInCategory> snapCats, View3D view3D)
        {
            List<int> snapCatsIds = new List<int>();
            foreach (var sC in snapCats)
            {
                snapCatsIds.Add(new ElementId(sC).IntegerValue);
            }

            Categories categories = doc.Settings.Categories;

            foreach (Category category in categories)
            {
                if (snapCatsIds.Contains(category.Id.IntegerValue))
                {
                    category.set_Visible(view3D, true);
                }
                else
                {
                    if (category.get_AllowsVisibilityControl(view3D))
                    {
                        category.set_Visible(view3D, false);
                    }
                }
            }
        }

        public static void SetVisibleLink(Document doc, LumiSnapSettingsData MySettings, View3D ceilCheckView)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<ElementId> linkedDocIdSet =
              collector
              .OfCategory(BuiltInCategory.OST_RvtLinks)
              .OfClass(typeof(RevitLinkType))
              .ToElementIds();

            foreach (ElementId linkedFileId in linkedDocIdSet)
            {
                Element link = doc.GetElement(linkedFileId);
                if (link.CanBeHidden(ceilCheckView))
                {
                    if (MySettings.LinkId == link.Id)
                    {
                        ceilCheckView.UnhideElements(new List<ElementId>() { link.Id });
                    }
                    else
                    {
                        ceilCheckView.HideElements(new List<ElementId>() { link.Id });
                    }
                }


            }
        }

        public static string GetConnectionAnnoProxyData(Document doc, FamilyInstance instance)
        {
            Entity retrievedEntity = instance.GetEntity(LumiAnnoProxySchema.GetSchema());
            IList<ElementId> assignedLumIds = retrievedEntity.Get<IList<ElementId>>("LumiIds");


            List<string> selectionELConnections = new List<string>();
            List<string> selectionELPositions = new List<string>();

            foreach (ElementId id in assignedLumIds)
            {
                Element selectedElement = doc.GetElement(id);

                if (selectedElement == null) continue;

                FamilyInstance selectedFamInstance = (FamilyInstance)selectedElement;
                ElementType selectedType = doc.GetElement(selectedElement.GetTypeId()) as ElementType;

                string paramElConnection = "";
                string paramElPos = "";
                var p = selectedFamInstance.get_Parameter(new System.Guid("41a9849c-f9a0-48fd-8b79-9a51cb222a8e"));
                if (p != null)
                {
                    paramElConnection = p.AsString();
                }
                p = selectedType.get_Parameter(new System.Guid("4ad68b64-b7cf-4e80-a76f-660a4aadc4c1"));
                if (p != null)
                {
                    paramElPos = p.AsString();
                }

                selectionELConnections.Add(paramElConnection);
                selectionELPositions.Add(paramElPos);
            }

            Dictionary<string, List<string>> groupedConnections = selectionELConnections.GroupBy(x => x).ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<string, List<string>> groupedPositions = selectionELPositions.GroupBy(x => x).ToDictionary(g => g.Key, g => g.ToList());

            string resultTxt = "";

            foreach (string connectionTxt in groupedConnections.Keys)
            {
                resultTxt = resultTxt + connectionTxt + "\n";
            }

            foreach (var positionValPair in groupedPositions)
            {
                if (positionValPair.Value.Count == 1)
                {
                    resultTxt = resultTxt + positionValPair.Key + "\n";
                }
                else
                {
                    resultTxt = resultTxt + positionValPair.Value.Count.ToString() + "x" + positionValPair.Key + "\n";
                }
            }

            return resultTxt;
        }

        public static class LumiAnnoProxySchema
        {
            readonly static Guid schemaGuid = new Guid(
              "F0A1D091-5064-45CE-8105-0F6774AC32E3");  // change this

            public static Schema GetSchema()
            {
                Schema schema = Schema.Lookup(schemaGuid);

                if (schema != null) return schema;

                SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

                schemaBuilder.SetSchemaName("LumiAnnoProxyData");

                //FieldBuilder myField;

                schemaBuilder.AddArrayField("LumiIds", typeof(ElementId));

                return schemaBuilder.Finish();
            }
        }


        #region Settings

        public class LumiSnapSettingsData
        {
            public string ViewName { get; set; }
            public double DistanceRev { get; set; }
            public double DistanceFwd { get; set; }
            public ElementId LinkId { get; set; }
        }

        public static class LumiSnapSettingsSchema
        {
            readonly static Guid schemaGuid = new Guid(
              "41EB8254-C9F3-418D-AD4B-1FE08FD0A1A2");

            public static Schema GetSchema()
            {
                Schema schema = Schema.Lookup(schemaGuid);

                if (schema != null) return schema;

                SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

                schemaBuilder.SetSchemaName("LumiSnapSettings");

                FieldBuilder myField;
                schemaBuilder.AddSimpleField("ViewName", typeof(string));

                myField = schemaBuilder.AddSimpleField("DistanceRev", typeof(double));
                myField.SetUnitType(UnitType.UT_Length);

                myField = schemaBuilder.AddSimpleField("DistanceFwd", typeof(double));
                myField.SetUnitType(UnitType.UT_Length);

                myField = schemaBuilder.AddSimpleField("LinkId", typeof(ElementId));

                return schemaBuilder.Finish();
            }
        }

        public static class LumiSnapSettingsIdSchema
        {
            readonly static Guid schemaGuid = new Guid(
              "57B90136-63D8-41E8-BCD2-E66B65E6D243");

            public static Schema GetSchema()
            {
                Schema schema = Schema.Lookup(schemaGuid);

                if (schema != null) return schema;

                SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

                schemaBuilder.SetSchemaName("SettingsID");

                schemaBuilder.AddSimpleField("ID", typeof(Guid));

                return schemaBuilder.Finish();
            }
        }

        public class LumiSnapSettingsStorage
        {
            readonly Guid settingDsId = new Guid(
              "57B90136-63D8-41E8-BCD2-E66B65E6D243");

            public LumiSnapSettingsData ReadSettings(Document doc)
            {
                var settingsEntity = GetSettingsEntity(doc);

                if (settingsEntity == null
                  || !settingsEntity.IsValid())
                {
                    return null;
                }

                LumiSnapSettingsData settings = new LumiSnapSettingsData();

                settings.ViewName = settingsEntity.Get<string>("ViewName");
                settings.DistanceRev = settingsEntity.Get<double>("DistanceRev", DisplayUnitType.DUT_MILLIMETERS);
                settings.DistanceFwd = settingsEntity.Get<double>("DistanceFwd", DisplayUnitType.DUT_MILLIMETERS);
                settings.LinkId = settingsEntity.Get<ElementId>("LinkId");

                return settings;
            }

            public void WriteSettings(
              Document doc,
              LumiSnapSettingsData settings)
            {
                DataStorage settingDs = GetSettingsDataStorage(doc);

                if (settingDs == null)
                {
                    settingDs = DataStorage.Create(doc);
                }

                Entity settingsEntity = new Entity(LumiSnapSettingsSchema.GetSchema());

                settingsEntity.Set("ViewName", settings.ViewName);
                settingsEntity.Set("DistanceRev", settings.DistanceRev, DisplayUnitType.DUT_MILLIMETERS);
                settingsEntity.Set("DistanceFwd", settings.DistanceFwd, DisplayUnitType.DUT_MILLIMETERS);
                settingsEntity.Set("LinkId", settings.LinkId);

                //Identify settings data storage

                Entity idEntity = new Entity(LumiSnapSettingsIdSchema.GetSchema());

                idEntity.Set("ID", settingDsId);

                settingDs.SetEntity(idEntity);
                settingDs.SetEntity(settingsEntity);
            }

            private DataStorage GetSettingsDataStorage(Document doc)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                var dataStorages = collector.OfClass(typeof(DataStorage));


                foreach (DataStorage dataStorage in dataStorages)
                {
                    Entity settingIdEntity = dataStorage.GetEntity(LumiSnapSettingsIdSchema.GetSchema());

                    // If a DataStorage contains 
                    // setting entity, we found it

                    if (!settingIdEntity.IsValid()) continue;

                    return dataStorage;
                }

                return null;

            }

            private Entity GetSettingsEntity(Document doc)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                var dataStorages = collector.OfClass(typeof(DataStorage));

                // Find setting data storage

                foreach (DataStorage dataStorage in dataStorages)
                {
                    Entity settingEntity = dataStorage.GetEntity(LumiSnapSettingsSchema.GetSchema());

                    // If a DataStorage contains 
                    // setting entity, we found it

                    if (!settingEntity.IsValid()) continue;

                    return settingEntity;
                }

                return null;
            }
        }

        #endregion

    }
}