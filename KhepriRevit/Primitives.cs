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
        private int customFamilyCounter = 0;
        static private Dictionary<string, Family> fileNameToFamily = new Dictionary<string, Family>();
        static private Dictionary<Family, Dictionary<string, FamilySymbol>> loadedFamiliesSymbols = 
            new Dictionary<Family, Dictionary<string, FamilySymbol>>();

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

        public Level FindLevelAtElevation(double elevation)
        {
            return new FilteredElementCollector(doc)
                .WherePasses(new ElementClassFilter(typeof(Level), false))
                .Cast<Level>().FirstOrDefault(e => e.Elevation == elevation);
        }
        public ElementId CreateLevelAtElevation(double elevation)
        {
            Level level = doc.Create.NewLevel(elevation);
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
            return level.Id;
        }
        public ElementId FindOrCreateLevelAtElevation(double elevation)
        {
            Level level = FindLevelAtElevation(elevation);
            return (level == null) ? CreateLevelAtElevation(elevation) : level.Id;
        }
        public ElementId UpperLevel(ElementId currentLevelId, double addedElevation)
        {
            Level level = doc.GetElement(currentLevelId) as Level;
            return FindOrCreateLevelAtElevation(level.Elevation + addedElevation);
        }

        public Family LoadFamily(string fileName)
        {
            Family family;
            if (!fileNameToFamily.TryGetValue(fileName, out family))
            {
                Debug.Assert(doc.LoadFamily(fileName, out family));
                fileNameToFamily[fileName] = family;
            }
            return family;
        }

        bool FamilyElementMatches(FamilySymbol symb, string[] namesList, double[] valuesList)
        {
            double epsilon = 0.022;
            for (int i = 0; i < namesList.Length; i++)
            {
                double valueTest = symb.get_Parameter(namesList[i]).AsDouble();
                if (Math.Abs(valueTest - valuesList[i]) > epsilon)
                {
                    return false;
                }
            }
            return true;
        }
        public ElementId FamilyElement(Family family, string[] namesList, double[] valuesList)
        {
            Dictionary<string, FamilySymbol> loadedFamilySymbols;
            if (!loadedFamiliesSymbols.TryGetValue(family, out loadedFamilySymbols))
            {
                loadedFamilySymbols = new Dictionary<string, FamilySymbol>();
                loadedFamiliesSymbols[family] = loadedFamilySymbols;
            }
            string parametersStr = "";
            for (int i = 0; i < namesList.Length; i++)
            {
                parametersStr += namesList[i] + ":" + valuesList[i] + ",";
            }
            FamilySymbol familySymbol;
            if (!loadedFamilySymbols.TryGetValue(parametersStr, out familySymbol))
            {
                familySymbol = family.GetFamilySymbolIds()
                    .Select(id => doc.GetElement(id) as FamilySymbol)
                    .FirstOrDefault(sym => FamilyElementMatches(sym, namesList, valuesList));
                if (familySymbol == null)
                {
                    familySymbol = doc.GetElement(family.GetFamilySymbolIds().First()) as FamilySymbol;
                    string nName = "CustomFamily" + customFamilyCounter.ToString();
                    customFamilyCounter++;
                    familySymbol = familySymbol.Duplicate(nName) as FamilySymbol;
                    for (int i = 0; i < namesList.Length; i++)
                    {
                        familySymbol.get_Parameter(namesList[i]).Set(valuesList[i]);
                    }
                }
                loadedFamilySymbols[parametersStr] = familySymbol;
            }
            return familySymbol.Id;
        }
        public ElementId CreatePolygonalFloor(XYZ[] pts, ElementId levelId)
        {
            Level level = doc.GetElement(levelId) as Level;
            FloorType floorType = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).First() as FloorType;
            CurveArray profile = new CurveArray();
            for (int i = 0; i < pts.Length; i++)
            {
                profile.Append(Line.CreateBound(pts[i], pts[(i + 1) % pts.Length]));
            }
            Floor floor = doc.Create.NewFloor(profile, floorType, level, false);
            floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0); //Below the level line
            return floor.Id;
        }
        public ElementId CreatePolygonalRoof(XYZ[] pts, ElementId levelId, ElementId famId)
        {
            Level level = doc.GetElement(levelId) as Level;
            RoofType roofType = null;
            if (famId != null)
            {
                roofType = doc.GetElement(famId) as RoofType;
            }
            else
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
            return roof.Id;
        }
        public ElementId CreateColumn(XYZ location, ElementId baseLevelId, ElementId topLevelId, ElementId famId)
        {
            Level level0 = doc.GetElement(baseLevelId) as Level;
            Level level1 = doc.GetElement(topLevelId) as Level;
            FamilySymbol symbol = (famId == null) ?
                GetFirstSymbol(FindColumnFamilies(doc).FirstOrDefault()) :
                doc.GetElement(famId) as FamilySymbol;
            FamilyInstance col = doc.Create.NewFamilyInstance(location, symbol, level0, Autodesk.Revit.DB.Structure.StructuralType.Column);
            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM).Set(level1.Id);
            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM).Set(0.0);
            col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM).Set(0.0);
            return col.Id;
        }
        public ElementId CreateBeam(XYZ p0, XYZ p1, ElementId famId)
        {
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
            return beam.Id;
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
