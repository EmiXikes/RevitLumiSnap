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
        public static XYZ GetCeilingPoint(View3D view3D, XYZ initialPosition, double revDistance)
        {

            XYZ initialDeltaPosition = initialPosition - new XYZ(0, 0, revDistance);
            XYZ rayDirection = new XYZ(0, 0, 1);

            List<BuiltInCategory> builtInCats = new List<BuiltInCategory>();

            builtInCats.Add(BuiltInCategory.OST_Roofs);
            builtInCats.Add(BuiltInCategory.OST_Ceilings);
            builtInCats.Add(BuiltInCategory.OST_Floors);
            builtInCats.Add(BuiltInCategory.OST_Walls);
            builtInCats.Add(BuiltInCategory.OST_Stairs);

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
                return referenceWithContext.GetReference().GlobalPoint;
            }

            return initialPosition;
        }

        #region Settings

        public class LumiSnapSettingsData
        {
            public string ViewName { get; set; }
            public string DistanceRev { get; set; }
            public string DistanceFwd { get; set; }
        }

        public static class LumiSnapSettingsSchema
        {
            readonly static Guid schemaGuid = new Guid(
              "FD5ADE10-367E-48A5-B21A-5E44D73B8224");

            public static Schema GetSchema()
            {
                Schema schema = Schema.Lookup(schemaGuid);

                if (schema != null) return schema;

                SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);

                schemaBuilder.SetSchemaName("LumiSnapSettings");

                schemaBuilder.AddSimpleField("ViewName", typeof(string));
                schemaBuilder.AddSimpleField("DistanceRev", typeof(string));
                schemaBuilder.AddSimpleField("DistanceFwd", typeof(string));

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
                settings.DistanceRev = settingsEntity.Get<string>("DistanceRev");
                settings.DistanceFwd = settingsEntity.Get<string>("DistanceFwd");

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
                settingsEntity.Set("DistanceRev", settings.DistanceRev);
                settingsEntity.Set("DistanceFwd", settings.DistanceFwd);

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