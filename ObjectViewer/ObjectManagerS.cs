// ╔═════════════════════════════════════════════════════════════╗
// ║ ObjectManager.cs for the Structure Viewer                   ║
// ╠═════════════════════════════════════════════════════════════╣
// ║ This file cannot be used in the openBVE main program.       ║
// ║ The file from the openBVE main program cannot be used here. ║
// ╚═════════════════════════════════════════════════════════════╝

using System;

namespace OpenBve {
    internal static class ObjectManager {

        // unified objects
        internal abstract class UnifiedObject { }

        // objects
        internal class StaticObject : UnifiedObject {
            internal World.Mesh Mesh;
            internal int RendererIndex;
            internal float StartingDistance;
            internal float EndingDistance;
            internal byte Dynamic;
        }
        internal static StaticObject[] Objects = new StaticObject[16];
        internal static int ObjectsUsed;

        // load object
        internal enum ObjectLoadMode { Normal, DontAllowUnloadOfTextures, PreloadTextures }
        internal static UnifiedObject LoadObject(string FileName, System.Text.Encoding Encoding, ObjectLoadMode LoadMode, bool PreserveVertices, bool ForceTextureRepeat) {
#if !DEBUG
            try {
#endif
                if (!System.IO.Path.HasExtension(FileName)) {
                    while (true) {
                        string f;
                        f = Interface.GetCorrectedFileName(FileName + ".x");
                        if (System.IO.File.Exists(f)) {
                            FileName = f;
                            break;
                        }
                        f = Interface.GetCorrectedFileName(FileName + ".csv");
                        if (System.IO.File.Exists(f)) {
                            FileName = f;
                            break;
                        }
                        f = Interface.GetCorrectedFileName(FileName + ".b3d");
                        if (System.IO.File.Exists(f)) {
                            FileName = f;
                            break;
                        }
                        break;
                    }
                }
                UnifiedObject Result;
                switch (System.IO.Path.GetExtension(FileName).ToLowerInvariant()) {
                    case ".csv":
                    case ".b3d":
                        Result = CsvB3dObjectParser.ReadObject(FileName, Encoding, LoadMode, ForceTextureRepeat);
                        break;
                    case ".x":
                        Result = XObjectParser.ReadObject(FileName, Encoding, LoadMode, ForceTextureRepeat);
                        break;
                    default:
                        Interface.AddMessage(Interface.MessageType.Error, false, "The file extension is not supported in " + FileName);
                        return null;
                }
                OptimizeObject(Result, PreserveVertices);
                return Result;
#if !DEBUG
            } catch (Exception ex) {
                Interface.AddMessage(Interface.MessageType.Error, true, "An unexpected error occured (" + ex.Message + ") while attempting to load the file " + FileName);
                return null;
            }
#endif
        }
        internal static StaticObject LoadStaticObject(string FileName, System.Text.Encoding Encoding, ObjectLoadMode LoadMode, bool PreserveVertices, bool ForceTextureRepeat) {
#if !DEBUG
            try {
#endif
                if (!System.IO.Path.HasExtension(FileName)) {
                    while (true) {
                        string f;
                        f = Interface.GetCorrectedFileName(FileName + ".x");
                        if (System.IO.File.Exists(f)) {
                            FileName = f;
                            break;
                        }
                        f = Interface.GetCorrectedFileName(FileName + ".csv");
                        if (System.IO.File.Exists(f)) {
                            FileName = f;
                            break;
                        }
                        f = Interface.GetCorrectedFileName(FileName + ".b3d");
                        if (System.IO.File.Exists(f)) {
                            FileName = f;
                            break;
                        }
                        break;
                    }
                }
                StaticObject Result;
                switch (System.IO.Path.GetExtension(FileName).ToLowerInvariant()) {
                    case ".csv":
                    case ".b3d":
                        Result = CsvB3dObjectParser.ReadObject(FileName, Encoding, LoadMode, ForceTextureRepeat);
                        break;
                    case ".x":
                        Result = XObjectParser.ReadObject(FileName, Encoding, LoadMode, ForceTextureRepeat);
                        break;
                    default:
                        Interface.AddMessage(Interface.MessageType.Error, false, "The file extension is not supported in " + FileName);
                        return null;
                }
                OptimizeObject(Result, PreserveVertices);
                return Result;
#if !DEBUG
            } catch (Exception ex) {
                Interface.AddMessage(Interface.MessageType.Error, true, "An unexpected error occured (" + ex.Message + ") while attempting to load the file " + FileName);
                return null;
            }
#endif
        }

        // optimize object
        internal static void OptimizeObject(UnifiedObject Prototype, bool PreserveVertices) {
            if (Prototype is StaticObject) {
                StaticObject s = (StaticObject)Prototype;
                OptimizeObject(s, PreserveVertices);
            }
        }
        internal static void OptimizeObject(StaticObject Prototype, bool PreserveVertices) {
            if (Prototype == null) return;
            // materials
            if (Prototype.Mesh.Materials.Length >= 1) {
                // merge
                int m = Prototype.Mesh.Materials.Length;
                for (int i = m - 1; i >= 1; i--) {
                    for (int j = i - 1; j >= 0; j--) {
                        if (World.MeshMaterial.Equals(Prototype.Mesh.Materials[i], Prototype.Mesh.Materials[j])) {
                            for (int k = i; k < m - 1; k++) {
                                Prototype.Mesh.Materials[k] = Prototype.Mesh.Materials[k + 1];
                            }
                            for (int k = 0; k < Prototype.Mesh.Faces.Length; k++) {
                                int a = (int)Prototype.Mesh.Faces[k].Material;
                                if (a == i) {
                                    Prototype.Mesh.Faces[k].Material = (ushort)j;
                                } else if (a > i) {
                                    Prototype.Mesh.Faces[k].Material--;
                                }
                            }
                            m--;
                            break;
                        }
                    }
                }
                // eliminate unsed
                for (int i = m - 1; i >= 0; i--) {
                    int j; for (j = 0; j < Prototype.Mesh.Faces.Length; j++) {
                        if ((int)Prototype.Mesh.Faces[j].Material == i) break;
                    } if (j == Prototype.Mesh.Faces.Length) {
                        for (int k = i; k < m - 1; k++) {
                            Prototype.Mesh.Materials[k] = Prototype.Mesh.Materials[k + 1];
                        }
                        for (int k = 0; k < Prototype.Mesh.Faces.Length; k++) {
                            int a = (int)Prototype.Mesh.Faces[k].Material;
                            if (a > i) {
                                Prototype.Mesh.Faces[k].Material--;
                            }
                        } m--;
                    }
                }
                if (m != Prototype.Mesh.Materials.Length) {
                    Array.Resize<World.MeshMaterial>(ref Prototype.Mesh.Materials, m);
                }
            }
            // vertices
            if (Prototype.Mesh.Vertices.Length >= 1 & !PreserveVertices) {
                // merge
                int v = Prototype.Mesh.Vertices.Length;
                for (int i = v - 1; i >= 1; i--) {
                    for (int j = i - 1; j >= 0; j--) {
                        if (World.Vertex.Equals(Prototype.Mesh.Vertices[i], Prototype.Mesh.Vertices[j])) {
                            for (int k = i; k < v - 1; k++) {
                                Prototype.Mesh.Vertices[k] = Prototype.Mesh.Vertices[k + 1];
                            }
                            for (int k = 0; k < Prototype.Mesh.Faces.Length; k++) {
                                for (int h = 0; h < Prototype.Mesh.Faces[k].Vertices.Length; h++) {
                                    int a = (int)Prototype.Mesh.Faces[k].Vertices[h].Index;
                                    if (a == i) {
                                        Prototype.Mesh.Faces[k].Vertices[h].Index = (ushort)j;
                                    } else if (a > i) {
                                        Prototype.Mesh.Faces[k].Vertices[h].Index--;
                                    }
                                }
                            }
                            v--;
                            break;
                        }
                    }
                }
                // eliminate unused
                for (int i = v - 1; i >= 0; i--) {
                    int j; for (j = 0; j < Prototype.Mesh.Faces.Length; j++) {
                        int k; for (k = 0; k < Prototype.Mesh.Faces[j].Vertices.Length; k++) {
                            if ((int)Prototype.Mesh.Faces[j].Vertices[k].Index == i) break;
                        } if (k != Prototype.Mesh.Faces[j].Vertices.Length) break;
                    } if (j == Prototype.Mesh.Faces.Length) {
                        for (int k = i; k < v - 1; k++) {
                            Prototype.Mesh.Vertices[k] = Prototype.Mesh.Vertices[k + 1];
                        }
                        for (int k = 0; k < Prototype.Mesh.Faces.Length; k++) {
                            for (int h = 0; h < Prototype.Mesh.Faces[k].Vertices.Length; h++) {
                                int a = (int)Prototype.Mesh.Faces[k].Vertices[h].Index;
                                if (a > i) {
                                    Prototype.Mesh.Faces[k].Vertices[h].Index--;
                                }
                            }
                        } v--;
                    }
                }
                if (v != Prototype.Mesh.Vertices.Length) {
                    Array.Resize<World.Vertex>(ref Prototype.Mesh.Vertices, v);
                }
            }
        }

        // create object
        internal static void CreateObject(UnifiedObject Prototype, World.Vector3D Position, World.Transformation BaseTransformation, World.Transformation AuxTransformation, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double TrackPosition) {
            CreateObject(Prototype, Position, BaseTransformation, AuxTransformation, -1, AccurateObjectDisposal, StartingDistance, EndingDistance, TrackPosition, 1.0, false);
        }
        internal static void CreateObject(UnifiedObject Prototype, World.Vector3D Position, World.Transformation BaseTransformation, World.Transformation AuxTransformation, int SectionIndex, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double TrackPosition, double Brightness, bool DuplicateMaterials) {
            if (Prototype is StaticObject) {
                StaticObject s = (StaticObject)Prototype;
                CreateStaticObject(s, Position, BaseTransformation, AuxTransformation, AccurateObjectDisposal, StartingDistance, EndingDistance, TrackPosition, Brightness, DuplicateMaterials);
            }
        }

        // create static object
        internal static int CreateStaticObject(StaticObject Prototype, World.Vector3D Position, World.Transformation BaseTransformation, World.Transformation AuxTransformation, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double TrackPosition) {
            return CreateStaticObject(Prototype, Position, BaseTransformation, AuxTransformation, AccurateObjectDisposal, StartingDistance, EndingDistance, TrackPosition, 1.0, false);
        }
        internal static int CreateStaticObject(StaticObject Prototype, World.Vector3D Position, World.Transformation BaseTransformation, World.Transformation AuxTransformation, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double TrackPosition, double Brightness, bool DuplicateMaterials) {
            int a = ObjectsUsed;
            if (a >= Objects.Length) {
                Array.Resize<StaticObject>(ref Objects, Objects.Length << 1);
            }
            ApplyStaticObjectData(ref Objects[a], Prototype, Position, BaseTransformation, AuxTransformation, AccurateObjectDisposal, StartingDistance, EndingDistance, TrackPosition, Brightness, DuplicateMaterials);
            ObjectsUsed++;
            return a;
        }
        internal static void ApplyStaticObjectData(ref StaticObject Object, StaticObject Prototype, World.Vector3D Position, World.Transformation BaseTransformation, World.Transformation AuxTransformation, bool AccurateObjectDisposal, double StartingDistance, double EndingDistance, double TrackPosition, double Brightness, bool DuplicateMaterials) {
            Object = new StaticObject();
            Object.StartingDistance = float.MaxValue;
            Object.EndingDistance = float.MinValue;
            bool brightnesschange = Brightness != 1.0;
            // vertices
            Object.Mesh.Vertices = new World.Vertex[Prototype.Mesh.Vertices.Length];
            for (int j = 0; j < Prototype.Mesh.Vertices.Length; j++) {
                Object.Mesh.Vertices[j] = Prototype.Mesh.Vertices[j];
                if (AccurateObjectDisposal) {
                    World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, AuxTransformation);
                    if (Object.Mesh.Vertices[j].Coordinates.Z < Object.StartingDistance) {
                        Object.StartingDistance = (float)Object.Mesh.Vertices[j].Coordinates.Z;
                    }
                    if (Object.Mesh.Vertices[j].Coordinates.Z > Object.EndingDistance) {
                        Object.EndingDistance = (float)Object.Mesh.Vertices[j].Coordinates.Z;
                    }
                    Object.Mesh.Vertices[j].Coordinates = Prototype.Mesh.Vertices[j].Coordinates;
                }
                World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, AuxTransformation);
                World.Rotate(ref Object.Mesh.Vertices[j].Coordinates.X, ref Object.Mesh.Vertices[j].Coordinates.Y, ref Object.Mesh.Vertices[j].Coordinates.Z, BaseTransformation);
                Object.Mesh.Vertices[j].Coordinates.X += Position.X;
                Object.Mesh.Vertices[j].Coordinates.Y += Position.Y;
                Object.Mesh.Vertices[j].Coordinates.Z += Position.Z;
            }
            // faces
            Object.Mesh.Faces = new World.MeshFace[Prototype.Mesh.Faces.Length];
            for (int j = 0; j < Prototype.Mesh.Faces.Length; j++) {
                Object.Mesh.Faces[j].Flags = Prototype.Mesh.Faces[j].Flags;
                Object.Mesh.Faces[j].Material = Prototype.Mesh.Faces[j].Material;
                Object.Mesh.Faces[j].Vertices = new World.MeshFaceVertex[Prototype.Mesh.Faces[j].Vertices.Length];
                for (int k = 0; k < Prototype.Mesh.Faces[j].Vertices.Length; k++) {
                    Object.Mesh.Faces[j].Vertices[k] = Prototype.Mesh.Faces[j].Vertices[k];
                    double nx = Object.Mesh.Faces[j].Vertices[k].Normal.X;
                    double ny = Object.Mesh.Faces[j].Vertices[k].Normal.Y;
                    double nz = Object.Mesh.Faces[j].Vertices[k].Normal.Z;
                    if (nx * nx + ny * ny + nz * nz != 0.0) {
                        World.Rotate(ref Object.Mesh.Faces[j].Vertices[k].Normal.X, ref Object.Mesh.Faces[j].Vertices[k].Normal.Y, ref Object.Mesh.Faces[j].Vertices[k].Normal.Z, AuxTransformation);
                        World.Rotate(ref Object.Mesh.Faces[j].Vertices[k].Normal.X, ref Object.Mesh.Faces[j].Vertices[k].Normal.Y, ref Object.Mesh.Faces[j].Vertices[k].Normal.Z, BaseTransformation);
                    }
                }
            }
            World.CreateNormals(ref Object.Mesh);
            // materials
            Object.Mesh.Materials = new World.MeshMaterial[Prototype.Mesh.Materials.Length];
            for (int j = 0; j < Prototype.Mesh.Materials.Length; j++) {
                Object.Mesh.Materials[j] = Prototype.Mesh.Materials[j];
                Object.Mesh.Materials[j].Color.R = (byte)Math.Round((double)Prototype.Mesh.Materials[j].Color.R * Brightness);
                Object.Mesh.Materials[j].Color.G = (byte)Math.Round((double)Prototype.Mesh.Materials[j].Color.G * Brightness);
                Object.Mesh.Materials[j].Color.B = (byte)Math.Round((double)Prototype.Mesh.Materials[j].Color.B * Brightness);
            }
            if (AccurateObjectDisposal) {
                Object.StartingDistance += (float)TrackPosition;
                Object.EndingDistance += (float)TrackPosition;
            } else {
                Object.StartingDistance = (float)StartingDistance;
                Object.EndingDistance = (float)EndingDistance;
            }
        }

        // create dynamic object
        internal static int CreateDynamicObject() {
            int a = ObjectsUsed;
            if (a >= Objects.Length) {
                Array.Resize<StaticObject>(ref Objects, Objects.Length << 1);
            }
            Objects[a] = new StaticObject();
            Objects[a].Mesh.Faces = new World.MeshFace[] { };
            Objects[a].Mesh.Materials = new World.MeshMaterial[] { };
            Objects[a].Mesh.Vertices = new World.Vertex[] { };
            Objects[a].Dynamic = 1;
            ObjectsUsed++;
            return a;
        }

        // clone object
        internal static StaticObject CloneObject(StaticObject Prototype) {
            if (Prototype == null) return null;
            return CloneObject(Prototype, -1, -1);
        }
        internal static StaticObject CloneObject(StaticObject Prototype, int DaytimeTextureIndex, int NighttimeTextureIndex) {
            if (Prototype == null) return null;
            StaticObject Result = new StaticObject();
            Result.StartingDistance = Prototype.StartingDistance;
            Result.EndingDistance = Prototype.EndingDistance;
            // vertices
            Result.Mesh.Vertices = new World.Vertex[Prototype.Mesh.Vertices.Length];
            for (int j = 0; j < Prototype.Mesh.Vertices.Length; j++) {
                Result.Mesh.Vertices[j] = Prototype.Mesh.Vertices[j];
            }
            // faces
            Result.Mesh.Faces = new World.MeshFace[Prototype.Mesh.Faces.Length];
            for (int j = 0; j < Prototype.Mesh.Faces.Length; j++) {
                Result.Mesh.Faces[j].Flags = Prototype.Mesh.Faces[j].Flags;
                Result.Mesh.Faces[j].Material = Prototype.Mesh.Faces[j].Material;
                Result.Mesh.Faces[j].Vertices = new World.MeshFaceVertex[Prototype.Mesh.Faces[j].Vertices.Length];
                for (int k = 0; k < Prototype.Mesh.Faces[j].Vertices.Length; k++) {
                    Result.Mesh.Faces[j].Vertices[k] = Prototype.Mesh.Faces[j].Vertices[k];
                }
            }
            // materials
            Result.Mesh.Materials = new World.MeshMaterial[Prototype.Mesh.Materials.Length];
            for (int j = 0; j < Prototype.Mesh.Materials.Length; j++) {
                Result.Mesh.Materials[j] = Prototype.Mesh.Materials[j];
                if (DaytimeTextureIndex >= 0) {
                    Result.Mesh.Materials[j].DaytimeTextureIndex = DaytimeTextureIndex;
                }
                if (NighttimeTextureIndex >= 0) {
                    Result.Mesh.Materials[j].NighttimeTextureIndex = NighttimeTextureIndex;
                }
            }
            return Result;
        }

        // finish creating objects
        internal static void FinishCreatingObjects() {
            Array.Resize<StaticObject>(ref Objects, ObjectsUsed);
        }

        // make all visible
        internal static void MakeAllVisible() {
            for (int i = 0; i < ObjectsUsed; i++) {
                Renderer.ShowObject(i, false);
            }
        }

    }
}