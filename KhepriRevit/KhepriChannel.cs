using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KhepriRevit
{
    public class KhepriChannel : IDisposable
    {
        public Document doc;
        public NetworkStream stream;
        public BinaryReader r;
        public BinaryWriter w;
        Primitives primitives;
        public List<Action<KhepriChannel, Primitives>> operations;
        public int DebugMode;
        public bool FastMode;

        public KhepriChannel(Document doc, Primitives primitives, NetworkStream stream)
        {
            this.doc = doc;
            this.primitives = primitives;
            this.stream = stream;
            this.r = new BinaryReader(stream);
            this.w = new BinaryWriter(stream);
            // Storage for operations made available. The starting one is the operation that makes other operations available 
            this.operations = new List<Action<KhepriChannel, Primitives>> {
                new Action<KhepriChannel, Primitives>(ProvideOperation)
            };
            this.DebugMode = 0;
            this.FastMode = false;
        }

        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                r.Dispose();
                w.Dispose();
                operations.Clear();
            }
            // Free any unmanaged objects here.
            disposed = true;
        }


        public void flush() => w.Flush();
        /*
         * We use, as convention, that the name of the reader is 'r' + type
         * and the name of the writer is 'w' + type
         * For handling errors, we also include the error signaller, which
         * is 'e' + type.
         * WARNING: This is used by the code generation part
         */

        void dumpException(Exception e) { wString(e.Message + "\n" + e.StackTrace); }
        public void wVoid() => w.Write((byte)0);
        public void eVoid(Exception e) { w.Write((byte)127); dumpException(e); }

        public int rByte() => r.ReadByte();
        public void wByte(byte b) => w.Write(b);
        public void eByte(Exception e) { w.Write(-123); dumpException(e); }

        public bool rBoolean() => r.ReadByte() == 1;
        public void wBoolean(bool b) => w.Write(b ? (byte)1 : (byte) 2);
        public void eBoolean(Exception e) { w.Write((byte)127); dumpException(e); }

        public int rInt16() => r.ReadInt16();

        public int rInt32() => r.ReadInt32();
        public void wInt32(Int32 i) => w.Write(i);
        public void eInt32(Exception e) { w.Write(-12345); dumpException(e); }

        public string rString() => r.ReadString();
        public void wString(string s) => w.Write(s);
        public void eString(Exception e) { w.Write("This an error!"); dumpException(e); }

        public double rDouble() => r.ReadDouble();
        public void wDouble(double d) => w.Write(d);
        public void eDouble(Exception e) { w.Write(Double.NaN); dumpException(e); }

        public XYZ rXYZ() => new XYZ(rDouble(), rDouble(), rDouble());
        public void wXYZ(XYZ p) { w.Write(p.X); w.Write(p.Y); w.Write(p.Z); }
        public void eXYZ(Exception e) { eDouble(e); }
        /*
                public Vector3d rVector3d() => new Vector3d(rDouble(), rDouble(), rDouble());
                public void wVector3d(Vector3d p) { w.Write(p.X); w.Write(p.Y); w.Write(p.Z); }
                public void eVector3d(Exception e) { eDouble(e); }

                public void wFrame3d(Frame3d f)
                {
                    wPoint3d(f.origin);
                    wVector3d(f.xaxis);
                    wVector3d(f.yaxis);
                    wVector3d(f.zaxis);
                }
                public void eFrame3d(Exception e) { ePoint3d(e); }
*/
        public ElementId rElementId()
        {
            int id = r.ReadInt32();
            //Check this number. Should we use -1?
            return (id == 0) ? null : new ElementId(id);
        }
        public void wElementId(ElementId id) => wInt32(id.IntegerValue);
        public void eElementId(Exception e) { wInt32(-1); dumpException(e); }

        public Element rElement() => doc.GetElement(rElementId());
        public void wElement(Element e) { using (e) { wElementId(e.Id); } }
        public void eElement(Exception e) => eElementId(e);

        public Level rLevel() => rElement() as Level;
        public void wLevel(Level e) => wElement(e);
        public void eLevel(Exception e) => eElement(e);

        /*
                        public Frame3d rFrame3d() => new Frame3d(rPoint3d(), rVector3d(), rVector3d(), rVector3d());

                        public void wDoubleArray(double[] ds)
                        {
                            wInt32(ds.Length);
                            foreach (var d in ds)
                            {
                                wDouble(d);
                            }
                        }
                        public void eDoubleArray(Exception e) { wInt32(-1); dumpException(e); }
                */
        public XYZ[] rXYZArray()
        {
            int length = rInt32();
            XYZ[] pts = new XYZ[length];
            for (int i = 0; i < length; i++)
            {
                pts[i] = rXYZ();
            }
            return pts;
        }
        public void wXYZArray(XYZ[] pts)
        {
            wInt32(pts.Length);
            foreach (var pt in pts)
            {
                wXYZ(pt);
            }
        }
        public void eXYZArray(Exception e) => wInt32(-1);

        public ElementId[] rElementIdArray()
        {
            int length = rInt32();
            ElementId[] ids = new ElementId[length];
            for (int i = 0; i < length; i++)
            {
                ids[i] = rElementId();
            }
            return ids;
        }
        public void wElementIdArray(ElementId[] ids)
        {
            wInt32(ids.Length);
            foreach (var id in ids)
            {
                wElementId(id);
            }
        }
        public string[] rStringArray()
        {
            int length = rInt32();
            string[] strs = new string[length];
            for (int i = 0; i < length; i++)
            {
                strs[i] = rString();
            }
            return strs;
        }
        public void wStringArray(string[] strs)
        {
            wInt32(strs.Length);
            foreach (var str in strs)
            {
                wString(str);
            }
        }
        public void eStringArray(Exception e) { wInt32(-1); dumpException(e); }
        public double[] rDoubleArray()
        {
            int length = rInt32();
            double[] ds = new double[length];
            for (int i = 0; i < length; i++)
            {
                ds[i] = rDouble();
            }
            return ds;
        }
        public void wDoubleArray(double[] ds)
        {
            wInt32(ds.Length);
            foreach (var d in ds)
            {
                wDouble(d);
            }
        }
        public void eDoubleArray(Exception e) { wInt32(-1); dumpException(e); }


        static private Dictionary<int, Family> loadedFamilies = new Dictionary<int, Family>();
        public Family rFamily() => loadedFamilies[rInt32()];
        public void wFamily(Family f) { int i = f.Id.IntegerValue; loadedFamilies[i] = f; wInt32(i); }
        public void eFamily(Exception e) => eElementId(e);

        /*        
                public Document getDoc() => Application.DocumentManager.MdiActiveDocument;
                public Transaction getTrans(Document doc) => doc.Database.TransactionManager.StartTransaction();
                public ObjectId addShape(Entity shape)
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    using (doc.LockDocument())
                    using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        ObjectId id;
                        using (shape)
                        {
                            id = btr.AppendEntity(shape);
                            tr.AddNewlyCreatedDBObject(shape, true);
                        }
                        tr.Commit();
                        //doc.Editor.UpdateScreen();
                        return id;
                    }
                }
                public void SetDebugMode(int mode)
                {
                    DebugMode = mode;
                }
                public void SetFastMode(bool mode)
                {
                    FastMode = mode;
                }
                public Entity getShape(ObjectId id)
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    using (doc.LockDocument())
                    using (Transaction tr = doc.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        //This doesn't seem very safe, but it is working
                        return (Entity)tr.GetObject(id, OpenMode.ForRead);
                    }
                }

                public void shapeGetter(ObjectId id, Action<Entity> f)
                {
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    using (doc.LockDocument())
                    using (Transaction tr = doc.Database.TransactionManager.StartOpenCloseTransaction())
                    {
                        f((Entity)tr.GetObject(id, OpenMode.ForRead));
                    }
                }
                */
        public static void ProvideOperation(KhepriChannel c, Primitives p)
        {
            var action = RMIfy.RMIFor(c, p, c.rString());
            if (action == null)
            {
                c.wInt32(-1);
            }
            else
            {
                c.operations.Add(action);
                c.wInt32(c.operations.Count - 1);
            }
        }
        public int ReadOperation()
        {
            try
            {
                return rByte();
            }
            catch (EndOfStreamException)
            {
                return -1;
            }
        }
        public bool ExecuteOperation(int op)
        {
            using (Transaction t = new Transaction(doc, "Execute"))
            {
                t.Start();
                WarningSwallower.KhepriWarnings(t);
                while (true)
                {
                    if (op == -1)
                    {
                        t.Commit();
                        return false;
                    }
                    operations[op](this, primitives);
                    flush();
                    stream.ReadTimeout = 20;
                    try
                    {
                        op = ReadOperation();
                    }
                    catch (IOException)
                    {
                        break;
                    }
                    finally
                    {
                        stream.ReadTimeout = -1;
                    }
                }
                t.Commit();
                return true;
            }
        }
    }
}
