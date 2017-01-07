using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace KhepriRevit
{
    class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    public class ClientHandler : IExternalEventHandler
    {
        public int op;
        public bool result = true;
        public ManualResetEvent resumeEvt = new ManualResetEvent(false);
        public KhepriChannel channel;

        public void Execute(UIApplication uiapp)
        {
            try
            {
                result = channel.ExecuteOperation(op);
                resumeEvt.Set();
            }
            catch (Exception e)
            {
                PlugIn.WriteMessage(e.Message + e.StackTrace);
            }
        }
        public string GetName()
        {
            return "Khepri Handler";
        }
    }

    public class PlugIn : IExternalApplication
    {
        public static PlugIn thisApp = null;
        private static UIApplication thisUIapp;
        //private Dictionary<string, Func<bool>> funcDictionary = new Dictionary<string, Func<bool>>();
        //private List<FailureDefinitionId> failureDefinitionIdList = null;

        void WaitForConnections(ClientHandler c, ExternalEvent evt)
        {
            try
            {
                Int32 port = 11001;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                TcpListener server = new TcpListener(localAddr, port);
                server.Start();
                //WriteMessage("Waiting for connections\n");
                while (true)
                {
                    KhepriChannel channel = new KhepriChannel(
                        thisUIapp.ActiveUIDocument.Document,
                        new Primitives(thisUIapp),
                        server.AcceptTcpClient().GetStream());
                    Thread thread = new Thread(() => HandleClient(c, evt, channel));
                    thread.Start();
                    //WriteMessage("Connection established\n");
                }
            }
            catch (Exception e)
            {
                WriteMessage(e.ToString() + "\n");
            }
        }

        void HandleClient(ClientHandler c, ExternalEvent evt, KhepriChannel channel)
        {
            c.channel = channel;
            try
            {
                while (true)
                {
                    c.op = channel.ReadOperation();
                    c.resumeEvt.Reset();
                    evt.Raise();
                    c.resumeEvt.WaitOne();
                    if (!c.result) break;
                }
                WriteMessage("Client disconnected\n");
            }
            catch (IOException e)
            {
                WriteMessage(e.ToString() + "\n");
                WriteMessage("Disconneting from client\n");
            }
            catch (Exception e)
            {
                WriteMessage(e.ToString() + "\n");
                WriteMessage("Terminating client\n");
            }
        }

        public static void WriteMessage(String msg)
        {
            MessageBox.Show(msg);
        }


        public struct MyLevel
        {
            public string name;
            public double height;

            public MyLevel(string n, double h)
            {
                name = n;
                height = h;
            }
        }

        /*
        public struct MyWall
        {
            public XYZ endPoint0;
            public XYZ endPoint1;
            public MyLevel bottomLevel;
            public MyLevel topLevel;
            public string wallType;

            public MyWall(XYZ ep0, XYZ ep1, MyLevel bl, MyLevel tl, string wt)
            {
                endPoint0 = ep0;
                endPoint1 = ep1;
                bottomLevel = bl;
                topLevel = tl;
                wallType = wt;
            }
        }

        public int callCounter = 0;

        public System.Drawing.Point lastCursor = new System.Drawing.Point(-5, -5);

            */
        public Result OnStartup(UIControlledApplication application)
        {
            thisApp = this;
            //application.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
            application.ControlledApplication.DocumentCreated += OnDocumentCreated;
            return Result.Succeeded;
        }

        void OnDocumentCreated(object sender, DocumentCreatedEventArgs e)
        {
            //          ClientHandler clientHandler = new ClientHandler();
            Application app = sender as Application;
            thisUIapp = new UIApplication(app);
            ClientHandler cli = new ClientHandler();
            ExternalEvent evt = ExternalEvent.Create(cli);
            Thread thread = new Thread(() => WaitForConnections(cli, evt));
            thread.Start();
        }

        /*        void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
                {
                    //WriteMessage("sender:" + sender);
                    Application app = sender as Application;
                    thisUIapp = new UIApplication(app);
                    Thread thread = new Thread(() => WaitForConnections());
                    thread.Start();

                    thisApp = this;
                    RibbonPanel ribbonPanel = application.CreateRibbonPanel("NewRibbonPanel");
                    string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
                    PushButtonData buttonData = new PushButtonData("cmdKhepriConnect", "Khepri", thisAssemblyPath, "KhepriRevit.ConnectionKhepri");

                Uri uriImage = new Uri(@"C:\Users\BrunoF\Pictures\rosetta.png");

                     BitmapImage largeImage = new BitmapImage(uriImage);

                     pushButton.LargeImage = largeImage;*/


        //            application.Idling += OnIdling;
        //application.DialogBoxShowing += WarningHandler;
        /*
                    funcDictionary.Add("test", TestFunction);
                    funcDictionary.Add("box", BoxCMD);
                    funcDictionary.Add("cylinder", CylinderCMD);
                    funcDictionary.Add("cylinderb", CylinderBCMD);
                    funcDictionary.Add("cylinderMetric", CylinderMetricCMD);
                    funcDictionary.Add("sphere", SphereCMD);
                    funcDictionary.Add("sphereMetric", SphereMetricCMD);
                    funcDictionary.Add("createMassSweep", CreateMassSweepCMD);
                    funcDictionary.Add("createExtrusionMass", CreateExtrusionMassCMD);
                    funcDictionary.Add("moveElement", MoveElementCMD);
                    funcDictionary.Add("rotateElement", RotateElementCMD);
                    funcDictionary.Add("wall", WallCMD);
                    funcDictionary.Add("wallH", WallHCMD);
                    funcDictionary.Add("wallL", WallLCMD);
                    funcDictionary.Add("polyWall", PolyWallCMD);
                    funcDictionary.Add("getWallsInfo", GetWallsInfoCMD);
                    funcDictionary.Add("curtainWall", CurtainWallCMD);
                    funcDictionary.Add("massWall", MassWallCMD);
                    funcDictionary.Add("wallsFromSlabs", WallsFromSlabsCMD);
                    funcDictionary.Add("insertDoor", InsertDoorCMD);
                    funcDictionary.Add("insertDoor1", InsertDoorBCMD);
                    funcDictionary.Add("insertWindow", InsertWindowCMD);
                    funcDictionary.Add("deleteElement", DeleteElementCMD);
                    funcDictionary.Add("createLevel", CreateLevelCMD);
                    funcDictionary.Add("upperLevel", UpperLevelCMD);
                    funcDictionary.Add("getLevel", GetLevelCMD);
                    funcDictionary.Add("getLevelByName", GetLevelByNameCMD);
                    funcDictionary.Add("levelElevation", LevelElevationCMD);
                    funcDictionary.Add("getLevelsInfo", GetLevelsInfoCMD);
                    funcDictionary.Add("deleteLevel", DeleteLevelCMD);
                    funcDictionary.Add("createRoundFloor", CreateRoundFloorCMD);
                    funcDictionary.Add("createFloor", CreateFloorCMD);
                    funcDictionary.Add("createColumn", CreateColumnCMD);
                    funcDictionary.Add("createColumnPoints", CreateColumnPointsCMD);
                    funcDictionary.Add("createBeam", CreateBeamCMD);
                    funcDictionary.Add("createOpening", CreateOpeningCMD);
                    funcDictionary.Add("createRoof", CreateRoofCMD);
                    funcDictionary.Add("createFloorFromPoints", FloorFromPointsCMD);
                    funcDictionary.Add("holeSlab", HoleSlabCMD);
                    funcDictionary.Add("stairsRun", StairsRunCMD);
                    funcDictionary.Add("createStairs", CreateStairsCMD);
                    funcDictionary.Add("intersectWF", IntersectWallFloorCMD);
                    funcDictionary.Add("createRailings", CreateRailingsCMD);
                    funcDictionary.Add("getWallVolume", GetWallVolumeCMD);
                    funcDictionary.Add("createTopoSurface", CreateTopoSurfaceCMD);
                    funcDictionary.Add("createBuildingPad", CreateBuildingPadCMD);
                    funcDictionary.Add("highlightElement", HighlightElementCMD);
                    funcDictionary.Add("getSelectedElement", GetSelectedElementCMD);
                    funcDictionary.Add("importDWG", ImportDWGCMD);
                    funcDictionary.Add("isProject", IsProjectCMD);
                    funcDictionary.Add("loadFamily", LoadFamilyCMD);
                    funcDictionary.Add("familyElement", FamilyElementCMD);
                    funcDictionary.Add("disconnect", DisconnectCMD);

                    /*
                    failureDefinitionIdList = new List<FailureDefinitionId>();
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.WallSpaceSeparationOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.WallsOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.WallRoomSeparationOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.WallAreaBoundaryOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.SpaceSeparationLinesOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.RoomSeparationLinesOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.LevelsOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.DuplicateRebar);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.DuplicatePoints);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.DuplicateInstances);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.CurvesOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.OverlapFailures.AreaBoundaryLinesOverlap);
                    failureDefinitionIdList.Add(BuiltInFailures.InaccurateFailures.InaccurateLine);
    }
                    */

        public Result OnShutdown(UIControlledApplication application)
        {
            //application.Idling -= OnIdling;
            //application.DialogBoxShowing -= WarningHandler;
            return Result.Succeeded;
        }
    }
    /*

    private void OnIdling(object sender, IdlingEventArgs e)
    {
        if (thisUiapp == null)
        {
            thisUiapp = sender as UIApplication;
        }

        if (racket == null)
            return;

        callCounter++;
        try
        {
            if (racket.IsConnected())
            {
                while (racket.ReadCommandBuff())
                {
                    //ExecuteCommand();
                    namestrc n = namestrc.ParseDelimitedFrom(racket.getChannel());

                    Func<bool> f = funcDictionary[n.Name];

                    if (f == null)
                    {
                        TaskDialog.Show("Revit", "Unknown Form");
                    }
                    else
                    {
                        f.Invoke();
                    }
                }

                //When Revit users don't move the mouse
                if (Math.Abs(lastCursor.X - Cursor.Position.X) <= 1)
                {
                    //move the cursor left and right with small distance:
                    //1 pixel. so it looks like it is stable
                    //this way can trigger the Idling event repeatedly
                    if (callCounter % 2 == 0)
                    {
                        Cursor.Position = new System.Drawing.Point(
                        Cursor.Position.X + 1, Cursor.Position.Y);
                    }
                    else if (callCounter % 2 == 1)
                    {
                        Cursor.Position = new System.Drawing.Point(
                        Cursor.Position.X - 1, Cursor.Position.Y);
                    }
                }
                lastCursor = Cursor.Position;
            }
            else
            {
                DisconnectCMD();
            }
        }
        catch (Exception exc)
        {
            TaskDialog.Show("Revit", exc.ToString());
        }

    }
    */
    /*
    public void WarningHandler(Object sender, EventArgs args)
    {
        FailuresProcessingEventArgs fpArgs = args as FailuresProcessingEventArgs;
        FailuresAccessor accessor = fpArgs.GetFailuresAccessor();

        foreach (FailureMessageAccessor msgAccessor in accessor.GetFailureMessages())
        {
            FailureDefinitionId id = msgAccessor.GetFailureDefinitionId();
            if (!failureDefinitionIdList.Exists(e => e.Guid.ToString() == id.Guid.ToString()))
            {
                continue;
            }

            if (msgAccessor.GetSeverity() == FailureSeverity.Warning)
            {
                accessor.DeleteWarning(msgAccessor);
                continue;
            }
            if (msgAccessor.GetSeverity() == FailureSeverity.Error)
            {
                accessor.DeleteWarning(msgAccessor);
                continue;
            }
            if (msgAccessor.GetSeverity() == FailureSeverity.DocumentCorruption)
            {
                accessor.DeleteWarning(msgAccessor);
                continue;
            }
        }

        fpArgs.SetProcessingResult(FailureProcessingResult.Continue);
    }
    */
    /*
    public void InitiateConnection(UIApplication uiapp)
    {
        thisUiapp = uiapp;
        racket = new RacketChannel();
        facade = new Facade(uiapp);
        //TaskDialog.Show("Revit", "Connected to rosetta");
    }
    */
    //Codigo para ProtoBuffs
    /*
            bool TestFunction()
            {
                facade.TestFunc();

                return true;
            }

            bool BoxCMD()
            {
                boxstrc b = boxstrc.ParseDelimitedFrom(racket.getChannel());

                double h = b.Height;

                int id = facade.DrawBox(b.P0Coordx, b.P0Coordy, b.P0Coordz, b.P1Coordx, b.P1Coordy, b.P1Coordz, b.P2Coordx, b.P2Coordy, b.P2Coordz, b.P3Coordx, b.P3Coordy, b.P3Coordz, h);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CylinderCMD()
            {
                cylinderstrc c = cylinderstrc.ParseDelimitedFrom(racket.getChannel());
                double radius = c.Radius;
                double heigth = c.Height;

                int id = facade.DrawCylinder(c.P0Coordx, c.P0Coordy, c.P0Coordz, radius, heigth);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CylinderBCMD()
            {
                cylinderbstrc c = cylinderbstrc.ParseDelimitedFrom(racket.getChannel());

                double radius = c.Radius;

                int id = facade.DrawCylinder(c.P0Coordx, c.P0Coordy, c.P0Coordz, radius, c.P1Coordx, c.P1Coordy, c.P1Coordz);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CylinderMetricCMD()
            {
                cylinderbstrc c = cylinderbstrc.ParseDelimitedFrom(racket.getChannel());

                double radius = c.Radius;

                int id = facade.DrawCylinderMetric(c.P0Coordx, c.P0Coordy, c.P0Coordz, radius, c.P1Coordx, c.P1Coordy, c.P1Coordz);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool SphereCMD()
            {
                spherestrc s = spherestrc.ParseDelimitedFrom(racket.getChannel());

                IList<int> ids = facade.DrawSphere(s.P0Coordx, s.P0Coordy, s.P0Coordz, s.P1Coordx, s.P1Coordy, s.P1Coordz, s.P2Coordx, s.P2Coordy, s.P2Coordz);

                polyidstrc.Builder resultBuilder = polyidstrc.CreateBuilder();

                for (int i = 0; i < ids.Count; i++)
                {
                    resultBuilder.AddIds(idstrc.CreateBuilder().SetId(ids[i]).Build());
                }

                polyidstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool SphereMetricCMD()
            {
                spherestrc s = spherestrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.DrawSphereMetric(s.P0Coordx, s.P0Coordy, s.P0Coordz, s.P1Coordx, s.P1Coordy, s.P1Coordz, s.P2Coordx, s.P2Coordy, s.P2Coordz);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }


            bool CreateMassSweepCMD()
            {
                masssweepstrc sweepInfo = masssweepstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateMassSweep(sweepInfo.Profile1List, sweepInfo.PathList, sweepInfo.Profile2List);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateExtrusionMassCMD()
            {
                extrusionstrc extrusionInfo = extrusionstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateExtrusionMass(extrusionInfo.PtsList, extrusionInfo.Elevation);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool MoveElementCMD()
            {
                movestrc moveInfo = movestrc.ParseDelimitedFrom(racket.getChannel());

                facade.MoveElement(moveInfo.Element.Id, moveInfo.Vectorx, moveInfo.Vectory, moveInfo.Vectorz);

                return true;
            }

            bool RotateElementCMD()
            {
                rotatestrc rotateInfo = rotatestrc.ParseDelimitedFrom(racket.getChannel());

                facade.RotateElement(rotateInfo.Element.Id, rotateInfo.Angle, rotateInfo.P0X, rotateInfo.P0Y, rotateInfo.P0Z, rotateInfo.P1X, rotateInfo.P1Y, rotateInfo.P1Z);

                return true;
            }
            bool WallCMD()
            {
                wallstrc w = wallstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateWall(w.P0Coordx, w.P0Coordy, w.P0Coordz, w.P1Coordx, w.P1Coordy, w.P1Coordz, w.Level.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool WallHCMD()
            {
                wallheightstrc w = wallheightstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateWall(w.P0Coordx, w.P0Coordy, w.P0Coordz, w.P1Coordx, w.P1Coordy, w.P1Coordz, w.Height, w.Level.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool WallLCMD()
            {
                walllevelstrc w = walllevelstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateWall(w.P0Coordx, w.P0Coordy, w.P0Coordz, w.P1Coordx, w.P1Coordy, w.P1Coordz, w.Levelb.Id, w.Levelt.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool PolyWallCMD()
            {
                polywallstrc ws = polywallstrc.ParseDelimitedFrom(racket.getChannel());

                IList<int> ids = facade.CreatePolyWalls(ws.PtsList, ws.Levelb.Id, ws.Levelt.Id, ws.Familyid.Id);

                polyidstrc.Builder resultBuilder = polyidstrc.CreateBuilder();

                for (int i = 0; i < ids.Count; i++)
                {
                    resultBuilder.AddIds(idstrc.CreateBuilder().SetId(ids[i]).Build());
                }

                polyidstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool GetWallsInfoCMD()
            {
                List<MyWall> wallsInfo = facade.GetWallsInfo();

                polywallinfostrc.Builder resultBuilder = polywallinfostrc.CreateBuilder();

                wallinfostrc.Builder wallinfoBuilder = wallinfostrc.CreateBuilder();


                foreach (MyWall w in wallsInfo)
                {
                    wallinfoBuilder.P0Coordx = w.endPoint0.X;
                    wallinfoBuilder.P0Coordy = w.endPoint0.Y;
                    wallinfoBuilder.P0Coordz = w.endPoint0.Z;

                    wallinfoBuilder.P1Coordx = w.endPoint1.X;
                    wallinfoBuilder.P1Coordy = w.endPoint1.Y;
                    wallinfoBuilder.P1Coordz = w.endPoint1.Z;

                    wallinfoBuilder.Baseelevation = w.bottomLevel.height;
                    wallinfoBuilder.Baselevelname = w.bottomLevel.name;

                    wallinfoBuilder.Topelevation = w.topLevel.height;
                    wallinfoBuilder.Toplevelname = w.topLevel.name;

                    wallinfoBuilder.Walltype = w.wallType;

                    resultBuilder.AddWalls(wallinfoBuilder.Build());
                }

                polywallinfostrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CurtainWallCMD()
            {
                curtainwallstrc w = curtainwallstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateCurtainWall(w.P0Coordx, w.P0Coordy, w.P0Coordz,
                                                  w.P1Coordx, w.P1Coordy, w.P1Coordz,
                                                  w.UlinecoordList, w.VlinecoordList,
                                                  w.Baselevel.Id, w.Toplevel.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool MassWallCMD()
            {
                masswallstrc w = masswallstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateMassWall(w.Bottomleftcornerx, w.Bottomleftcornery, w.Bottomleftcornerz, w.Topleftcornerx, w.Topleftcornery, w.Topleftcornerz,
                                               w.Bottomrightcornerx, w.Bottomrightcornery, w.Bottomrightcornerz, w.Toprightcornerx, w.Toprightcornery, w.Topleftcornerz,
                                               w.Height, w.LevelId.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool WallsFromSlabsCMD()
            {
                wallsfromslabsstrc ws = wallsfromslabsstrc.ParseDelimitedFrom(racket.getChannel());

                IList<int> ids = facade.CreateWallsFromSlabs(ws.Slabid.Id, ws.Blevel.Id, ws.Height);

                polyidstrc.Builder resultBuilder = polyidstrc.CreateBuilder();

                for (int i = 0; i < ids.Count; i++)
                {
                    resultBuilder.AddIds(idstrc.CreateBuilder().SetId(ids[i]).Build());
                }

                polyidstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool InsertDoorCMD()
            {
                insertdoorstrc door = insertdoorstrc.ParseDelimitedFrom(racket.getChannel());

                int hostId = door.Hostid;

                int id = facade.InsertDoor(door.P0Coordx, door.P0Coordy, door.P0Coordz, hostId, door.Family.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool InsertDoorBCMD()
            {
                insertdoorbstrc door = insertdoorbstrc.ParseDelimitedFrom(racket.getChannel());

                int hostId = door.Hostid;

                int id = facade.InsertDoor1(door.Deltax, door.Deltay, hostId, door.Family.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool InsertWindowCMD()
            {
                insertwindowstrc window = insertwindowstrc.ParseDelimitedFrom(racket.getChannel());

                int hostId = window.Hostid;

                int id = facade.InsertWindow(window.Deltax, window.Deltay, hostId);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool DeleteElementCMD()
            {
                idstrc id = idstrc.ParseDelimitedFrom(racket.getChannel());

                facade.DeleteSelectedElement(id.Id);

                return true;
            }

            bool CreateLevelCMD()
            {
                doublestrc level = doublestrc.ParseDelimitedFrom(racket.getChannel());

                double h = level.Height;

                int id = facade.CreateLevel(h);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool UpperLevelCMD()
            {
                upperlevelstrc upperLevel = upperlevelstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.UpperLevel(upperLevel.Current.Id, upperLevel.Elevation);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool GetLevelCMD()
            {
                levelstrc level = levelstrc.ParseDelimitedFrom(racket.getChannel());

                double h = level.H;

                string levelName = level.Name;

                int id = facade.GetLevel(h, levelName);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool GetLevelByNameCMD()
            {
                namestrc name = namestrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.GetLevelByName(name.Name);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool LevelElevationCMD()
            {
                idstrc level = idstrc.ParseDelimitedFrom(racket.getChannel());

                double h = facade.GetLevelElevation(level.Id);

                doublestrc.Builder resultBuilder = doublestrc.CreateBuilder();
                resultBuilder.SetHeight(h);

                doublestrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool GetLevelsInfoCMD()
            {
                List<MyLevel> levelsInfo = facade.GetLevelsInfo();

                polylevelstrc.Builder resultBuilder = polylevelstrc.CreateBuilder();

                levelstrc.Builder levelBuilder = levelstrc.CreateBuilder();


                foreach (MyLevel l in levelsInfo)
                {

                    levelBuilder.SetH(l.height);
                    levelBuilder.SetName(l.name);
                    resultBuilder.AddLevels(levelBuilder.Build());
                }

                polylevelstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool DeleteLevelCMD()
            {
                namestrc lname = namestrc.ParseDelimitedFrom(racket.getChannel());

                string levelName = lname.Name;

                facade.DeleteLevel(levelName);

                return true;
            }

            bool CreateRoundFloorCMD()
            {
                roundfloorstrc roundFloor = roundfloorstrc.ParseDelimitedFrom(racket.getChannel());

                double centerX = roundFloor.CenterX;
                double centerY = roundFloor.CenterY;
                double centerZ = roundFloor.CenterZ;

                double radius = roundFloor.Radius;

                int id = facade.CreateRoundFloor(centerX, centerY, centerZ, radius, roundFloor.Level.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateFloorCMD()
            {
                floorstrc floor = floorstrc.ParseDelimitedFrom(racket.getChannel());

                double corner1x = floor.P0Coordx;
                double corner1y = floor.P0Coordy;
                double corner1z = floor.P0Coordz;

                double corner2x = floor.P1Coordx;
                double corner2y = floor.P1Coordy;
                double corner2z = floor.P1Coordz;

                int id = facade.CreateFloor(corner1x, corner1y, corner1z, corner2x, corner2y, corner2z, floor.Level.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateColumnCMD()
            {
                columnstrc inputColumn = columnstrc.ParseDelimitedFrom(racket.getChannel());

                double locationx = inputColumn.P0Coordx;
                double locationy = inputColumn.P0Coordy;
                double locationz = inputColumn.P0Coordz;

                int id = facade.CreateColumn(locationx, locationy, locationz, inputColumn.Baselevel.Id, inputColumn.Toplevel.Id, inputColumn.Width, inputColumn.Familyid.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateColumnPointsCMD()
            {
                columnpointsstrc inputColumn = columnpointsstrc.ParseDelimitedFrom(racket.getChannel());

                double p0x = inputColumn.P0Coordx;
                double p0y = inputColumn.P0Coordy;
                double p0z = inputColumn.P0Coordz;

                double p1x = inputColumn.P1Coordx;
                double p1y = inputColumn.P1Coordy;
                double p1z = inputColumn.P1Coordz;

                int id = facade.CreateColumnPoints(p0x, p0y, p0z, p1x, p1y, p1z, inputColumn.Baselevel.Id, inputColumn.Toplevel.Id, inputColumn.Width, inputColumn.Familyid.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateBeamCMD()
            {
                beaminfostrc beamInfo = beaminfostrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateBeam(beamInfo.P0Coordx, beamInfo.P0Coordy, beamInfo.P0Coordz, beamInfo.P1Coordx, beamInfo.P1Coordy, beamInfo.P1Coordz, beamInfo.Family.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateOpeningCMD()
            {
                flooropeningstrc opening = flooropeningstrc.ParseDelimitedFrom(racket.getChannel());

                double corner1x = opening.P0Coordx;
                double corner1y = opening.P0Coordy;
                double corner1z = opening.P0Coordz;

                double corner2x = opening.P1Coordx;
                double corner2y = opening.P1Coordy;
                double corner2z = opening.P1Coordz;

                int floorid = opening.Floorid;

                facade.CreateOpeningInFloor(corner1x, corner1y, corner1z, corner2x, corner2y, corner2z, floorid);

                return true;
            }

            bool CreateRoofCMD()
            {
                polylinefloorstrc p = polylinefloorstrc.ParseDelimitedFrom(racket.getChannel());

                IList<double> values = p.PointsList;

                IList<XYZ> points = new List<XYZ>();

                int i = 0;

                for (i = 0; i < values.Count; i = i + 3)
                {
                    XYZ pt = new XYZ(values[i], values[i + 1], values[i + 2]);

                    points.Add(pt);
                }

                int id = facade.CreateRoof(points, p.Floor.Id, p.Familyid.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool FloorFromPointsCMD()
            {
                polylinefloorstrc p = polylinefloorstrc.ParseDelimitedFrom(racket.getChannel());

                IList<double> values = p.PointsList;

                IList<XYZ> points = new List<XYZ>();

                int i = 0;

                for (i = 0; i < values.Count; i = i + 3)
                {
                    XYZ pt = new XYZ(values[i], values[i + 1], values[i + 2]);

                    points.Add(pt);
                }

                int id = facade.CreateFloorFromPoints(points, p.Floor.Id, p.Familyid.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool HoleSlabCMD()
            {
                holeslabstrc h = holeslabstrc.ParseDelimitedFrom(racket.getChannel());

                IList<double> values = h.PtsList;

                IList<XYZ> points = new List<XYZ>();

                int i = 0;

                for (i = 0; i < values.Count; i = i + 3)
                {
                    XYZ pt = new XYZ(values[i], values[i + 1], values[i + 2]);

                    points.Add(pt);
                }

                facade.CreateHoleInSlab(points, h.Slabid.Id);

                return true;
            }

            bool StairsRunCMD()
            {
                stairrunstrc stairInf = stairrunstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateStairsRun(stairInf.BottomLevel.Id, stairInf.TopLevel.Id, stairInf.P0Coordx, stairInf.P0Coordy, stairInf.P0Coordz,
                                                                                         stairInf.P1Coordx, stairInf.P1Coordy, stairInf.P1Coordz,
                                                                                         stairInf.Width, stairInf.Family.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateStairsCMD()
            {
                stairstrc stairInf = stairstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateStairsRun(stairInf.BottomLevel.Id, stairInf.TopLevel.Id, stairInf.Bottomp0Coordx, stairInf.Bottomp0Coordy, stairInf.Bottomp0Coordz, stairInf.Topp0Coordx, stairInf.Topp0Coordy, stairInf.Topp0Coordz);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool IntersectWallFloorCMD()
            {
                idstrc wallID = idstrc.ParseDelimitedFrom(racket.getChannel());

                idstrc floorID = idstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.IntersectWallFloor(wallID.Id, floorID.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateRailingsCMD()
            {
                railingsstrc rail = railingsstrc.ParseDelimitedFrom(racket.getChannel());

                idstrc slabid = rail.Slabid;

                facade.CreateRailings(slabid.Id);

                return true;
            }

            bool GetWallVolumeCMD()
            {
                idstrc wallid = idstrc.ParseDelimitedFrom(racket.getChannel());

                double volume = facade.GetWallVolume(wallid.Id);

                doublevolumestrc.Builder resultBuilder = doublevolumestrc.CreateBuilder();
                resultBuilder.SetVolume(volume);

                doublevolumestrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateTopoSurfaceCMD()
            {
                toposurfacestrc info = toposurfacestrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateTopoSurface(info.PtsList);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool CreateBuildingPadCMD()
            {
                buildingpadstrc infor = buildingpadstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.CreateBuildingPad(infor.PtsList, infor.LevelID.Id);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool HighlightElementCMD()
            {
                idstrc id = idstrc.ParseDelimitedFrom(racket.getChannel());

                facade.HighlightElement(id.Id);

                return true;
            }

            bool GetSelectedElementCMD()
            {
                IList<int> ids = facade.GetSelectedElements();

                polyidstrc.Builder resultBuilder = polyidstrc.CreateBuilder();

                for (int i = 0; i < ids.Count; i++)
                {
                    resultBuilder.AddIds(idstrc.CreateBuilder().SetId(ids[i]).Build());
                }

                polyidstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool ImportDWGCMD()
            {
                namestrc fileName = namestrc.ParseDelimitedFrom(racket.getChannel());

                facade.ImportDWG(fileName.Name);

                return true;
            }

            bool IsProjectCMD()
            {
                bool answer = facade.IsProject();

                boolstrc.Builder resultBuilder = boolstrc.CreateBuilder();
                resultBuilder.SetAnswer(answer);

                boolstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool LoadFamilyCMD()
            {
                namestrc pathName = namestrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.LoadFamilyFunc(pathName.Name);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool FamilyElementCMD()
            {
                familyelementstrc elementInfo = familyelementstrc.ParseDelimitedFrom(racket.getChannel());

                int id = facade.FamilyElementFunc(elementInfo.Familyid.Id, elementInfo.Flag, elementInfo.NamesList, elementInfo.ValuesList);

                idstrc.Builder resultBuilder = idstrc.CreateBuilder();
                resultBuilder.SetId(id);

                idstrc result = resultBuilder.Build();

                result.WriteDelimitedTo(racket.getChannel());

                return true;
            }

            bool DisconnectCMD()
            {
                stop = true;
                racket.StopRevitServer();
                racket = null;
                return true;
            }

        }
        */

    /*
        [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]

        public class ConnectionRosetta : IExternalCommand
        {
            public Autodesk.Revit.UI.Result Execute(ExternalCommandData revit, ref string message, ElementSet elements)
            {
                PlugIn.thisApp.InitiateConnection(revit.Application);
                return Autodesk.Revit.UI.Result.Succeeded;
            }
        }
        */
}
