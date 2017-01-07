using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KhepriRevit
{
    public class Primitives
    {
        private UIApplication uiapp;
        private Document doc;
        private int levelCounter = 3;

        public Primitives(UIApplication app)
        {
            uiapp = app;
            doc = uiapp.ActiveUIDocument.Document;
        }

        private static IEnumerable<Family> FindCategoryFamilies(Document doc, BuiltInCategory cat)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .Where(e => e.FamilyCategory != null && e.FamilyCategory.Id.IntegerValue == (int)cat);
        }

        private static IEnumerable<Family> FindDoorFamilies(Document doc) =>
            FindCategoryFamilies(doc, BuiltInCategory.OST_Doors);

        private static IEnumerable<Family> FindWindowFamilies(Document doc) =>
            FindCategoryFamilies(doc, BuiltInCategory.OST_Windows);

        private static IEnumerable<Family> FindColumnFamilies(Document doc) =>
            FindCategoryFamilies(doc, BuiltInCategory.OST_Columns);

        private static IEnumerable<Family> FindStructuralColumnFamilies(Document doc) =>
            FindCategoryFamilies(doc, BuiltInCategory.OST_StructuralColumns);

        private static IEnumerable<Family> FindWallFamilies(Document doc) =>
            FindCategoryFamilies(doc, BuiltInCategory.OST_Walls);

        private static FamilySymbol GetFirstSymbol(Family family)
        {
            return (from FamilySymbol fs in family.Symbols select fs).FirstOrDefault();
        }


        public static IOrderedEnumerable<Level> FindAndSortLevels(Document doc)
        {
            return new FilteredElementCollector(doc)
                            .WherePasses(new ElementClassFilter(typeof(Level), false))
                            .Cast<Level>()
                            .OrderBy(e => e.Elevation);
        }

        public ElementId CreateLevel(double elevation)
        {
            using (Transaction t = new Transaction(doc, "Creating a level"))
            {
                t.Start();
                Level level = new FilteredElementCollector(doc)
                    .WherePasses(new ElementClassFilter(typeof(Level), false))
                    .Cast<Level>().FirstOrDefault(e => e.Elevation == elevation);
                if (level == null)
                {
                    level = doc.Create.NewLevel(elevation);
                    level.Name = "Level " + levelCounter;
                    levelCounter++;
                    IEnumerable<ViewFamilyType> viewFamilyTypes;
                    viewFamilyTypes = from elem in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                                      let type = elem as ViewFamilyType
                                      where type.ViewFamily == ViewFamily.FloorPlan
                                      select type;
                    ViewPlan floorView = ViewPlan.Create(doc, viewFamilyTypes.First().Id, level.Id);
                    viewFamilyTypes = from elem in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                                      let type = elem as ViewFamilyType
                                      where type.ViewFamily == ViewFamily.CeilingPlan
                                      select type;
                    ViewPlan ceilingView = ViewPlan.Create(doc, viewFamilyTypes.First().Id, level.Id);
                }
                t.Commit();
                return level.Id;
            }
        }

        public ElementId CreatePolygonalFloor(XYZ[] pts, ElementId levelId)
        {
            using (Transaction t = new Transaction(doc, "Creating a floor"))
            {
                t.Start();
                WarningSwallower.KhepriWarnings(t);

                Level level = doc.GetElement(levelId) as Level;
                FloorType floorType = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).First() as FloorType;
                CurveArray profile = new CurveArray();
                for (int i = 0; i < pts.Length; i++)
                {
                    profile.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Length]));
                }
                Floor floor = doc.Create.NewFloor(profile, floorType, level, false);
                floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0); //Below the level line
                t.Commit();
                return floor.Id;
            }
        }

        public ElementId CreatePolygonalRoof(XYZ[] pts, ElementId levelId, ElementId famId)
        {
            using (Transaction t = new Transaction(doc, "Creating a roof"))
            {
                t.Start();
                WarningSwallower.KhepriWarnings(t);
                Level level = doc.GetElement(levelId) as Level;
                RoofType roofType = null;
                if (famId != null)
                {
                    roofType = doc.GetElement(famId) as RoofType;
                } else
                {
                    var roofTypeList = new FilteredElementCollector(doc).OfClass(typeof(RoofType));
                    roofType = roofTypeList.First(e =>
                        e.Name.Equals("Generic - 125mm") ||
                        e.Name.Equals("Generic Roof - 300mm")) as RoofType;
                    if (roofType == null)
                    {
                        roofType = roofTypeList.First() as RoofType;
                    }
                }
                CurveArray profile = new CurveArray();
                for (int i = 0; i < pts.Length; i++)
                {
                    profile.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Length]));
                }
                ModelCurveArray curveArray = new ModelCurveArray();
                FootPrintRoof roof = doc.Create.NewFootPrintRoof(profile, level, roofType, out curveArray);
                t.Commit();
                return roof.Id;
            }
        }

        public ElementId CreateColumn(XYZ location, ElementId baseLevelId, ElementId topLevelId, ElementId famId, double width)
        {
            using (Transaction t = new Transaction(doc, "Creating a column"))
            {
                t.Start();
                WarningSwallower.KhepriWarnings(t);

                Level level0 = doc.GetElement(baseLevelId) as Level;
                Level level1 = doc.GetElement(topLevelId) as Level;
                FamilySymbol symbol = (famId == null) ?
                    GetFirstSymbol(FindColumnFamilies(doc).FirstOrDefault()) :
                    doc.GetElement(famId) as FamilySymbol;
                if (width > 0)
                {
                    symbol.get_Parameter("Width").Set(width);
                    symbol.get_Parameter("Depth").Set(width);
                }
                FamilyInstance col = doc.Create.NewFamilyInstance(location, symbol, level0, Autodesk.Revit.DB.Structure.StructuralType.Column);
                col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(level1.Id);
                col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(0.0);
                col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(0.0);
                t.Commit();
                return col.Id;
            }
        }

        public ElementId CreateBeam(XYZ p0, XYZ p1, ElementId famId)
        {
            using (Transaction t = new Transaction(doc, "Creating a beam"))
            {
                t.Start();
                WarningSwallower.KhepriWarnings(t);
                FamilySymbol beamSymb = null;

                if (famId == null)
                {
                    Family defaultBeamFam = FindCategoryFamilies(doc, BuiltInCategory.OST_StructuralFraming).First();
                    beamSymb = doc.GetElement(defaultBeamFam.GetFamilySymbolIds().First()) as FamilySymbol;
                }
                else
                {
                    beamSymb = doc.GetElement(famId) as FamilySymbol;
                }
                FamilyInstance beam = doc.Create.NewFamilyInstance(Line.CreateBound(p0, p1), beamSymb, null, Autodesk.Revit.DB.Structure.StructuralType.Beam);
                t.Commit();
                return beam.Id;
            }
        }
    }

    class WarningSwallower : IFailuresPreprocessor
    {
        private List<FailureDefinitionId> failureDefinitionIdList = null;
        public static WarningSwallower forKhepri = new WarningSwallower();

        public static void KhepriWarnings(Transaction t)
        {
            FailureHandlingOptions failOp = t.GetFailureHandlingOptions();
            failOp.SetFailuresPreprocessor(WarningSwallower.forKhepri);
            t.SetFailureHandlingOptions(failOp);
        }

        public WarningSwallower()
        {
            failureDefinitionIdList = new List<FailureDefinitionId>();
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateLine);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateWall);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateAreaLine);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateBeamOrBrace);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateCurveBasedFamily);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateDriveCurve);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateGrid);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateLevel);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateMassingSketchLine);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateRefPlane);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateRoomSeparation);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateSketchLine);
            failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateSpaceSeparation);
        }
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            foreach (FailureMessageAccessor failure in failuresAccessor.GetFailureMessages())
            {
                FailureDefinitionId failID = failure.GetFailureDefinitionId();
                if (failureDefinitionIdList.Exists(e => e.Guid.ToString() == failID.Guid.ToString()))
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
