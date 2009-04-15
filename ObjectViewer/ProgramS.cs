// ╔═════════════════════════════════════════════════════════════╗
// ║ Program.cs for the Structure Viewer                         ║
// ╠═════════════════════════════════════════════════════════════╣
// ║ This file cannot be used in the openBVE main program.       ║
// ║ The file from the openBVE main program cannot be used here. ║
// ╚═════════════════════════════════════════════════════════════╝

using System;
using System.Windows.Forms;
using Tao.Sdl;
using Tao.OpenGl;

namespace OpenBve {
    internal static class Program {

        // system
        internal enum Platform { Windows, Linux, Mac }
        internal static Platform CurrentPlatform = Platform.Windows;
        internal static bool CurrentlyRunOnMono = false;
        internal static bool UseFilesystemHierarchyStandard = false;

        // members
        private static bool Quit = false;
        private static string[] Files = new string[] { };
        private static int LastTicks = int.MaxValue;
        private static bool ReducedMode = true;
        private static int ReducedModeEnteringTime = 0;
        private static int RotateX = 0;
        private static int RotateY = 0;
        private static double RotateXSpeed = 0.0;
        private static double RotateYSpeed = 0.0;
        private static int MoveX = 0;
        private static int MoveY = 0;
        private static int MoveZ = 0;
        private static double MoveXSpeed = 0.0;
        private static double MoveYSpeed = 0.0;
        private static double MoveZSpeed = 0.0;
        internal static int LightingTarget = 1;
        internal static double LightingRelative = 1.0;

        // mouse
        internal static short MouseCenterX = 0;
        internal static short MouseCenterY = 0;
        internal static World.Vector3D MouseCameraPosition = new World.Vector3D(0.0, 0.0, 0.0);
        internal static World.Vector3D MouseCameraDirection = new World.Vector3D(0.0, 0.0, 1.0);
        internal static World.Vector3D MouseCameraUp = new World.Vector3D(0.0, 1.0, 0.0);
        internal static World.Vector3D MouseCameraSide = new World.Vector3D(1.0, 0.0, 0.0);
        internal static byte MouseButton = 0;

        // main
        [STAThread]
        internal static void Main(string[] Args) {
            // platform and mono
            int p = (int)Environment.OSVersion.Platform;
            if (p == 4 | p == 128) {
                /// general Unix
                CurrentPlatform = Platform.Linux;
            } else if (p == 6) {
                /// Mac
                CurrentPlatform = Platform.Mac;
            } else {
                /// non-Unix
                CurrentPlatform = Platform.Windows;
            }
            CurrentlyRunOnMono = Type.GetType("Mono.Runtime") != null;
            // file hierarchy standard
            if (CurrentPlatform != Platform.Windows) {
                for (int i = 0; i < Args.Length; i++) {
                    if (Args[i].Equals("/fhs", StringComparison.OrdinalIgnoreCase)) {
                        UseFilesystemHierarchyStandard = true;
                        break;
                    }
                }
            }
            // command line arguments
            bool[] SkipArgs = new bool[Args.Length];
            if (Args.Length != 0) {
                string File = System.IO.Path.Combine(Application.StartupPath, "RouteViewer.exe");
                if (System.IO.File.Exists(File)) {
                    int Skips = 0;
                    System.Text.StringBuilder NewArgs = new System.Text.StringBuilder();
                    for (int i = 0; i < Args.Length; i++) {
                        if (System.IO.File.Exists(Args[i])) {
                            if (System.IO.Path.GetExtension(Args[i]).Equals(".csv", StringComparison.OrdinalIgnoreCase)) {
                                string Text = System.IO.File.ReadAllText(Args[i], System.Text.Encoding.UTF8);
                                if (Text.IndexOf("CreateMeshBuilder", StringComparison.OrdinalIgnoreCase) == -1 & Text.IndexOf(".Sta", StringComparison.OrdinalIgnoreCase) >= 0 & Text.IndexOf(".Stop", StringComparison.OrdinalIgnoreCase) >= 0) {
                                    if (NewArgs.Length != 0) NewArgs.Append(" ");
                                    NewArgs.Append("\"" + Args[i] + "\"");
                                    SkipArgs[i] = true;
                                    Skips++;
                                }
                            }
                        } else {
                            SkipArgs[i] = true;
                            Skips++;
                        }
                    }
                    if (NewArgs.Length != 0) {
                        System.Diagnostics.Process.Start(File, NewArgs.ToString());
                    }
                    if (Skips == Args.Length) return;
                }
            }
            // application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (Sdl.SDL_Init(Sdl.SDL_INIT_VIDEO) != 0) {
                MessageBox.Show("SDL failed to initialize the video subsystem.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            // initialize sdl window
            Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_DOUBLEBUFFER, 1);
            Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_DEPTH_SIZE, 16);
            Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_RED_SIZE, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_GREEN_SIZE, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_BLUE_SIZE, 8);
            Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_ALPHA_SIZE, 8);
            Sdl.SDL_ShowCursor(Sdl.SDL_ENABLE);
            // icon
            {
                string File = Interface.GetCombinedFileName(Interface.GetDataFolder(), "icon.bmp");
                if (System.IO.File.Exists(File)) {
                    IntPtr Bitmap = Sdl.SDL_LoadBMP(File);
                    if (Bitmap != null) {
                        Sdl.SDL_Surface Surface = (Sdl.SDL_Surface)System.Runtime.InteropServices.Marshal.PtrToStructure(Bitmap, typeof(Sdl.SDL_Surface));
                        int ColorKey = Sdl.SDL_MapRGB(Surface.format, 0, 0, 255);
                        Sdl.SDL_SetColorKey(Bitmap, Sdl.SDL_SRCCOLORKEY, ColorKey);
                        Sdl.SDL_WM_SetIcon(Bitmap, null);
                    }
                }
            }
            // initialize camera
            ResetCamera();
            // create window
            Renderer.ScreenWidth = 960;
            Renderer.ScreenHeight = 600;
            int Bits = 32;
            IntPtr video = Sdl.SDL_SetVideoMode(Renderer.ScreenWidth, Renderer.ScreenHeight, Bits, Sdl.SDL_OPENGL | Sdl.SDL_DOUBLEBUF);
            if (video != IntPtr.Zero) {
                // create window
                Sdl.SDL_WM_SetCaption(Application.ProductName, null);
                // anisotropic filtering
                string[] Extensions = Gl.glGetString(Gl.GL_EXTENSIONS).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Interface.CurrentOptions.AnisotropicFilteringMaximum = 0;
                for (int i = 0; i < Extensions.Length; i++) {
                    if (string.Compare(Extensions[i], "GL_EXT_texture_filter_anisotropic", StringComparison.OrdinalIgnoreCase) == 0) {
                        float n; Gl.glGetFloatv(Gl.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out n);
                        Interface.CurrentOptions.AnisotropicFilteringMaximum = (int)Math.Round((double)n);
                        break;
                    }
                }
                if (Interface.CurrentOptions.AnisotropicFilteringMaximum <= 0) {
                    Interface.CurrentOptions.AnisotropicFilteringMaximum = 0;
                    Interface.CurrentOptions.AnisotropicFilteringLevel = 0;
                    Interface.CurrentOptions.Interpolation = TextureManager.InterpolationMode.AnisotropicFiltering;
                } else {
                    Interface.CurrentOptions.AnisotropicFilteringLevel = Interface.CurrentOptions.AnisotropicFilteringMaximum;
                    Interface.CurrentOptions.Interpolation = TextureManager.InterpolationMode.TrilinearMipmapped;
                }
                // module initialization
                Renderer.Initialize();
                Renderer.InitializeLighting();
                Sdl.SDL_GL_SwapBuffers();
                Fonts.Initialize();
                UpdateViewport();
                // command line arguments
                for (int i = 0; i < Args.Length; i++) {
                    if (!SkipArgs[i] && System.IO.File.Exists(Args[i])) {
                        try {
                            ObjectManager.StaticObject a = ObjectManager.LoadStaticObject(Args[i], System.Text.Encoding.UTF8, ObjectManager.ObjectLoadMode.Normal, false, false);
                            ObjectManager.CreateObject(a, new World.Vector3D(0.0, 0.0, 0.0), new World.Transformation(0.0, 0.0, 0.0), new World.Transformation(0.0, 0.0, 0.0), true, 0.0, 0.0, 0.0);
                        } catch (Exception ex) {
                            Interface.AddMessage(Interface.MessageType.Critical, false, "Unhandled error (" + ex.Message + ") encountered while processing the file " + Args[i] + ".");
                        }
                        Array.Resize<string>(ref Files, Files.Length + 1);
                        Files[Files.Length - 1] = Args[i];
                    }
                }
                ObjectManager.MakeAllVisible();
                UpdateCaption();
                // loop
                while (!Quit) {
                    ProcessEvents();
                    if (ReducedMode) {
                        System.Threading.Thread.Sleep(125);
                    } else {
                        System.Threading.Thread.Sleep(1);
                        int a = Sdl.SDL_GetTicks();
                        if (ReducedModeEnteringTime == 0) {
                            ReducedModeEnteringTime = a + 1000;
                        }
                        bool updatelight = false;
                        if (a > LastTicks) {
                            bool keep = false;
                            double t = 0.001 * (double)(a - LastTicks);
                            // rotate x
                            if (RotateX == 0) {
                                double d = (1.0 + Math.Abs(RotateXSpeed)) * t;
                                if (RotateXSpeed >= -d & RotateXSpeed <= d) {
                                    RotateXSpeed = 0.0;
                                } else {
                                    RotateXSpeed -= (double)Math.Sign(RotateXSpeed) * d;
                                }
                            } else {
                                double d = (1.0 + 1.0 - 1.0 / (1.0 + RotateXSpeed * RotateXSpeed)) * t;
                                double m = 1.0;
                                RotateXSpeed += (double)RotateX * d;
                                if (RotateXSpeed < -m) {
                                    RotateXSpeed = -m;
                                } else if (RotateXSpeed > m) {
                                    RotateXSpeed = m;
                                }
                            }
                            if (RotateXSpeed != 0.0) {
                                double cosa = Math.Cos(RotateXSpeed * t);
                                double sina = Math.Sin(RotateXSpeed * t);
                                World.Rotate(ref World.AbsoluteCameraDirection.X, ref World.AbsoluteCameraDirection.Y, ref World.AbsoluteCameraDirection.Z, 0.0, 1.0, 0.0, cosa, sina);
                                World.Rotate(ref World.AbsoluteCameraUp.X, ref World.AbsoluteCameraUp.Y, ref World.AbsoluteCameraUp.Z, 0.0, 1.0, 0.0, cosa, sina);
                                World.Rotate(ref World.AbsoluteCameraSide.X, ref World.AbsoluteCameraSide.Y, ref World.AbsoluteCameraSide.Z, 0.0, 1.0, 0.0, cosa, sina);
                                keep = true;
                            }
                            // rotate y
                            if (RotateY == 0) {
                                double d = (1.0 + Math.Abs(RotateYSpeed)) * t;
                                if (RotateYSpeed >= -d & RotateYSpeed <= d) {
                                    RotateYSpeed = 0.0;
                                } else {
                                    RotateYSpeed -= (double)Math.Sign(RotateYSpeed) * d;
                                }
                            } else {
                                double d = (1.0 + 1.0 - 1.0 / (1.0 + RotateYSpeed * RotateYSpeed)) * t;
                                double m = 1.0;
                                RotateYSpeed += (double)RotateY * d;
                                if (RotateYSpeed < -m) {
                                    RotateYSpeed = -m;
                                } else if (RotateYSpeed > m) {
                                    RotateYSpeed = m;
                                }
                            }
                            if (RotateYSpeed != 0.0) {
                                double cosa = Math.Cos(RotateYSpeed * t);
                                double sina = Math.Sin(RotateYSpeed * t);
                                World.Rotate(ref World.AbsoluteCameraDirection.X, ref World.AbsoluteCameraDirection.Y, ref World.AbsoluteCameraDirection.Z, World.AbsoluteCameraSide.X, World.AbsoluteCameraSide.Y, World.AbsoluteCameraSide.Z, cosa, sina);
                                World.Rotate(ref World.AbsoluteCameraUp.X, ref World.AbsoluteCameraUp.Y, ref World.AbsoluteCameraUp.Z, World.AbsoluteCameraSide.X, World.AbsoluteCameraSide.Y, World.AbsoluteCameraSide.Z, cosa, sina);
                                keep = true;
                            }
                            // move x
                            if (MoveX == 0) {
                                double d = (2.5 + Math.Abs(MoveXSpeed)) * t;
                                if (MoveXSpeed >= -d & MoveXSpeed <= d) {
                                    MoveXSpeed = 0.0;
                                } else {
                                    MoveXSpeed -= (double)Math.Sign(MoveXSpeed) * d;
                                }
                            } else {
                                double d = (5.0 + 10.0 - 10.0 / (1.0 + MoveXSpeed * MoveXSpeed)) * t;
                                double m = 25.0;
                                MoveXSpeed += (double)MoveX * d;
                                if (MoveXSpeed < -m) {
                                    MoveXSpeed = -m;
                                } else if (MoveXSpeed > m) {
                                    MoveXSpeed = m;
                                }
                            }
                            if (MoveXSpeed != 0.0) {
                                World.AbsoluteCameraPosition.X += MoveXSpeed * t * World.AbsoluteCameraSide.X;
                                World.AbsoluteCameraPosition.Y += MoveXSpeed * t * World.AbsoluteCameraSide.Y;
                                World.AbsoluteCameraPosition.Z += MoveXSpeed * t * World.AbsoluteCameraSide.Z;
                                keep = true;
                            }
                            // move y
                            if (MoveY == 0) {
                                double d = (2.5 + Math.Abs(MoveYSpeed)) * t;
                                if (MoveYSpeed >= -d & MoveYSpeed <= d) {
                                    MoveYSpeed = 0.0;
                                } else {
                                    MoveYSpeed -= (double)Math.Sign(MoveYSpeed) * d;
                                }
                            } else {
                                double d = (5.0 + 10.0 - 10.0 / (1.0 + MoveYSpeed * MoveYSpeed)) * t;
                                double m = 25.0;
                                MoveYSpeed += (double)MoveY * d;
                                if (MoveYSpeed < -m) {
                                    MoveYSpeed = -m;
                                } else if (MoveYSpeed > m) {
                                    MoveYSpeed = m;
                                }
                            }
                            if (MoveYSpeed != 0.0) {
                                World.AbsoluteCameraPosition.X += MoveYSpeed * t * World.AbsoluteCameraUp.X;
                                World.AbsoluteCameraPosition.Y += MoveYSpeed * t * World.AbsoluteCameraUp.Y;
                                World.AbsoluteCameraPosition.Z += MoveYSpeed * t * World.AbsoluteCameraUp.Z;
                                keep = true;
                            }
                            // move z
                            if (MoveZ == 0) {
                                double d = (2.5 + Math.Abs(MoveZSpeed)) * t;
                                if (MoveZSpeed >= -d & MoveZSpeed <= d) {
                                    MoveZSpeed = 0.0;
                                } else {
                                    MoveZSpeed -= (double)Math.Sign(MoveZSpeed) * d;
                                }
                            } else {
                                double d = (5.0 + 10.0 - 10.0 / (1.0 + MoveZSpeed * MoveZSpeed)) * t;
                                double m = 25.0;
                                MoveZSpeed += (double)MoveZ * d;
                                if (MoveZSpeed < -m) {
                                    MoveZSpeed = -m;
                                } else if (MoveZSpeed > m) {
                                    MoveZSpeed = m;
                                }
                            }
                            if (MoveZSpeed != 0.0) {
                                World.AbsoluteCameraPosition.X += MoveZSpeed * t * World.AbsoluteCameraDirection.X;
                                World.AbsoluteCameraPosition.Y += MoveZSpeed * t * World.AbsoluteCameraDirection.Y;
                                World.AbsoluteCameraPosition.Z += MoveZSpeed * t * World.AbsoluteCameraDirection.Z;
                                keep = true;
                            }
                            // lighting
                            if (LightingRelative == -1) {
                                LightingRelative = (double)LightingTarget;
                                updatelight = true;
                            }
                            if (LightingTarget == 0) {
                                if (LightingRelative != 0.0) {
                                    LightingRelative -= 0.5 * t;
                                    if (LightingRelative < 0.0) LightingRelative = 0.0;
                                    updatelight = true;
                                    keep = true;
                                }
                            } else {
                                if (LightingRelative != 1.0) {
                                    LightingRelative += 0.5 * t;
                                    if (LightingRelative > 1.0) LightingRelative = 1.0;
                                    updatelight = true;
                                    keep = true;
                                }
                            }
                            // continue
                            if (keep) {
                                ReducedModeEnteringTime = a + 1000;
                            } else if (a > ReducedModeEnteringTime) {
                                ReducedMode = true;
                                World.AbsoluteCameraSide.Y = 0.0;
                                World.Normalize(ref World.AbsoluteCameraSide.X, ref World.AbsoluteCameraSide.Y, ref World.AbsoluteCameraSide.Z);
                                World.Normalize(ref World.AbsoluteCameraDirection.X, ref World.AbsoluteCameraDirection.Y, ref World.AbsoluteCameraDirection.Z);
                                World.AbsoluteCameraUp = World.Cross(World.AbsoluteCameraDirection, World.AbsoluteCameraSide);
                            }
                        } else {
                            if (LightingRelative == -1) {
                                LightingRelative = (double)LightingTarget;
                                updatelight = true;
                            }
                        }
                        if (updatelight) {
                            Renderer.OptionAmbientColor.R = (byte)Math.Round(32.0 + 128.0 * LightingRelative * (2.0 - LightingRelative));
                            Renderer.OptionAmbientColor.G = (byte)Math.Round(32.0 + 128.0 * 0.5 * (LightingRelative + LightingRelative * (2.0 - LightingRelative)));
                            Renderer.OptionAmbientColor.B = (byte)Math.Round(32.0 + 128.0 * LightingRelative);
                            Renderer.OptionDiffuseColor.R = (byte)Math.Round(32.0 + 128.0 * LightingRelative);
                            Renderer.OptionDiffuseColor.G = (byte)Math.Round(32.0 + 128.0 * LightingRelative);
                            Renderer.OptionDiffuseColor.B = (byte)Math.Round(32.0 + 128.0 * Math.Sqrt(LightingRelative));
                            Renderer.InitializeLighting();
                        }
                        if (ReducedMode) {
                            LastTicks = int.MaxValue;
                            ReducedModeEnteringTime = 0;
                        } else {
                            LastTicks = a;
                        }
                    }
                    Renderer.RenderScene();
                    Sdl.SDL_GL_SwapBuffers();
                }
                // quit
                TextureManager.UnuseAllTextures();
                Sdl.SDL_Quit();
            } else {
                MessageBox.Show("SDL failed to create the window.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        // reset camera
        private static void ResetCamera() {
            World.AbsoluteCameraPosition = new World.Vector3D(-5.0, 2.5, -25.0);
            World.AbsoluteCameraDirection = new World.Vector3D(-World.AbsoluteCameraPosition.X, -World.AbsoluteCameraPosition.Y, -World.AbsoluteCameraPosition.Z);
            World.AbsoluteCameraSide = new World.Vector3D(-World.AbsoluteCameraPosition.Z, 0.0, World.AbsoluteCameraPosition.X);
            World.Normalize(ref World.AbsoluteCameraDirection.X, ref World.AbsoluteCameraDirection.Y, ref World.AbsoluteCameraDirection.Z);
            World.Normalize(ref World.AbsoluteCameraSide.X, ref World.AbsoluteCameraSide.Y, ref World.AbsoluteCameraSide.Z);
            World.AbsoluteCameraUp = World.Cross(World.AbsoluteCameraDirection, World.AbsoluteCameraSide);
            World.VerticalViewingAngle = 45.0 * 0.0174532925199433;
            World.HorizontalViewingAngle = 2.0 * Math.Atan(Math.Tan(0.5 * World.VerticalViewingAngle) * World.AspectRatio);
            World.OriginalVerticalViewingAngle = World.VerticalViewingAngle;
        }

        // update viewport
        private static void UpdateViewport() {
            Gl.glViewport(0, 0, Renderer.ScreenWidth, Renderer.ScreenHeight);
            World.AspectRatio = (double)Renderer.ScreenWidth / (double)Renderer.ScreenHeight;
            World.HorizontalViewingAngle = 2.0 * Math.Atan(Math.Tan(0.5 * World.VerticalViewingAngle) * World.AspectRatio);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            const double invdeg = 57.295779513082320877;
            Glu.gluPerspective(World.VerticalViewingAngle * invdeg, -World.AspectRatio, 0.2, 1000.0);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
        }

        // process events
        private static void ProcessEvents() {
            Sdl.SDL_Event Event;
            while (Sdl.SDL_PollEvent(out Event) != 0) {
                switch (Event.type) {
                    // quit
                    case Sdl.SDL_QUIT:
                        Quit = true;
                        return;
                    // resize
                    case Sdl.SDL_VIDEORESIZE:
                        Renderer.ScreenWidth = Event.resize.w;
                        Renderer.ScreenHeight = Event.resize.h;
                        UpdateViewport();
                        break;
                    // mouse
                    case Sdl.SDL_MOUSEBUTTONDOWN:
                        MouseCenterX = Event.button.x;
                        MouseCenterY = Event.button.y;
                        MouseCameraPosition = World.AbsoluteCameraPosition;
                        MouseCameraDirection = World.AbsoluteCameraDirection;
                        MouseCameraUp = World.AbsoluteCameraUp;
                        MouseCameraSide = World.AbsoluteCameraSide;
                        MouseButton = Event.button.button;
                        break;
                    case Sdl.SDL_MOUSEBUTTONUP:
                        MouseButton = 0;
                        break;
                    case Sdl.SDL_MOUSEMOTION:
                        if (MouseButton == Sdl.SDL_BUTTON_LEFT) {
                            World.AbsoluteCameraDirection = MouseCameraDirection;
                            World.AbsoluteCameraUp = MouseCameraUp;
                            World.AbsoluteCameraSide = MouseCameraSide;
                            {
                                double dx = 0.0025 * (double)(MouseCenterX - Event.motion.x);
                                double cosa = Math.Cos(dx);
                                double sina = Math.Sin(dx);
                                World.Rotate(ref World.AbsoluteCameraDirection.X, ref World.AbsoluteCameraDirection.Y, ref World.AbsoluteCameraDirection.Z, 0.0, 1.0, 0.0, cosa, sina);
                                World.Rotate(ref World.AbsoluteCameraUp.X, ref World.AbsoluteCameraUp.Y, ref World.AbsoluteCameraUp.Z, 0.0, 1.0, 0.0, cosa, sina);
                                World.Rotate(ref World.AbsoluteCameraSide.X, ref World.AbsoluteCameraSide.Y, ref World.AbsoluteCameraSide.Z, 0.0, 1.0, 0.0, cosa, sina);
                            }
                            {
                                double dy = 0.0025 * (double)(MouseCenterY - Event.motion.y);
                                double cosa = Math.Cos(dy);
                                double sina = Math.Sin(dy);
                                World.Rotate(ref World.AbsoluteCameraDirection.X, ref World.AbsoluteCameraDirection.Y, ref World.AbsoluteCameraDirection.Z, World.AbsoluteCameraSide.X, World.AbsoluteCameraSide.Y, World.AbsoluteCameraSide.Z, cosa, sina);
                                World.Rotate(ref World.AbsoluteCameraUp.X, ref World.AbsoluteCameraUp.Y, ref World.AbsoluteCameraUp.Z, World.AbsoluteCameraSide.X, World.AbsoluteCameraSide.Y, World.AbsoluteCameraSide.Z, cosa, sina);
                            }
                            ReducedMode = false;
                        } else if (MouseButton == Sdl.SDL_BUTTON_RIGHT) {
                            World.AbsoluteCameraPosition = MouseCameraPosition;
                            double dx = -0.025 * (double)(Event.motion.x - MouseCenterX);
                            World.AbsoluteCameraPosition.X += dx * World.AbsoluteCameraSide.X;
                            World.AbsoluteCameraPosition.Y += dx * World.AbsoluteCameraSide.Y;
                            World.AbsoluteCameraPosition.Z += dx * World.AbsoluteCameraSide.Z;
                            double dy = 0.025 * (double)(Event.motion.y - MouseCenterY);
                            World.AbsoluteCameraPosition.X += dy * World.AbsoluteCameraUp.X;
                            World.AbsoluteCameraPosition.Y += dy * World.AbsoluteCameraUp.Y;
                            World.AbsoluteCameraPosition.Z += dy * World.AbsoluteCameraUp.Z;
                            ReducedMode = false;
                        } else if (MouseButton == Sdl.SDL_BUTTON_MIDDLE) {
                            World.AbsoluteCameraPosition = MouseCameraPosition;
                            double dx = -0.025 * (double)(Event.motion.x - MouseCenterX);
                            World.AbsoluteCameraPosition.X += dx * World.AbsoluteCameraSide.X;
                            World.AbsoluteCameraPosition.Y += dx * World.AbsoluteCameraSide.Y;
                            World.AbsoluteCameraPosition.Z += dx * World.AbsoluteCameraSide.Z;
                            double dz = -0.025 * (double)(Event.motion.y - MouseCenterY);
                            World.AbsoluteCameraPosition.X += dz * World.AbsoluteCameraDirection.X;
                            World.AbsoluteCameraPosition.Y += dz * World.AbsoluteCameraDirection.Y;
                            World.AbsoluteCameraPosition.Z += dz * World.AbsoluteCameraDirection.Z;
                            ReducedMode = false;
                        }
                        break;
                    // key down
                    case Sdl.SDL_KEYDOWN:
                        switch (Event.key.keysym.sym) {
                            case Sdl.SDLK_F5:
                                ReducedMode = false;
                                LightingRelative = -1.0;
                                Renderer.Reset();
                                TextureManager.UnuseAllTextures();
                                Fonts.Initialize();
                                ObjectManager.Objects = new ObjectManager.StaticObject[16];
                                ObjectManager.ObjectsUsed = 0;
                                Interface.ClearMessages();
                                for (int i = 0; i < Files.Length; i++) {
                                    try {
                                        ObjectManager.StaticObject a = ObjectManager.LoadStaticObject(Files[i], System.Text.Encoding.UTF8, ObjectManager.ObjectLoadMode.Normal, false, false);
                                        ObjectManager.CreateObject(a, new World.Vector3D(0.0, 0.0, 0.0), new World.Transformation(0.0, 0.0, 0.0), new World.Transformation(0.0, 0.0, 0.0), true, 0.0, 0.0, 0.0);
                                    } catch (Exception ex) {
                                        Interface.AddMessage(Interface.MessageType.Critical, false, "Unhandled error (" + ex.Message + ") encountered while processing the file " + Files[i] + ".");
                                    }
                                }
                                ObjectManager.MakeAllVisible();
                                break;
                            case Sdl.SDLK_F7: {
                                    OpenFileDialog Dialog = new OpenFileDialog();
                                    Dialog.CheckFileExists = true;
                                    Dialog.Multiselect = true;
                                    Dialog.Filter = "CSV/B3D/X files|*.csv;*.b3d;*.x|All files|*";
                                    if (Dialog.ShowDialog() == DialogResult.OK) {
                                        Interface.ClearMessages();
                                        string[] f = Dialog.FileNames;
                                        int n = Files.Length;
                                        Array.Resize<string>(ref Files, n + f.Length);
                                        for (int i = 0; i < f.Length; i++) {
                                            try {
                                                ObjectManager.StaticObject a = ObjectManager.LoadStaticObject(f[i], System.Text.Encoding.UTF8, ObjectManager.ObjectLoadMode.Normal, false, false);
                                                ObjectManager.CreateObject(a, new World.Vector3D(0.0, 0.0, 0.0), new World.Transformation(0.0, 0.0, 0.0), new World.Transformation(0.0, 0.0, 0.0), true, 0.0, 0.0, 0.0);
                                            } catch (Exception ex) {
                                                Interface.AddMessage(Interface.MessageType.Critical, false, "Unhandled error (" + ex.Message + ") encountered while processing the file " + f[i] + ".");
                                            }
                                            Files[n + i] = f[i];
                                        }
                                        UpdateCaption();
                                        ObjectManager.MakeAllVisible();
                                    }
                                } break;
                            case Sdl.SDLK_F9:
                                if (Interface.MessageCount != 0) {
                                    formMessages.ShowMessages();
                                } break;
                            case Sdl.SDLK_DELETE:
                                ReducedMode = false;
                                LightingRelative = -1.0;
                                Renderer.Reset();
                                TextureManager.UnuseAllTextures();
                                Fonts.Initialize();
                                Interface.ClearMessages();
                                ObjectManager.Objects = new ObjectManager.StaticObject[16];
                                ObjectManager.ObjectsUsed = 0;
                                Files = new string[] { };
                                UpdateCaption();
                                break;
                            case Sdl.SDLK_LEFT:
                                RotateX = -1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_RIGHT:
                                RotateX = 1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_UP:
                                RotateY = -1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_DOWN:
                                RotateY = 1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_a:
                            case Sdl.SDLK_KP4:
                                MoveX = -1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_d:
                            case Sdl.SDLK_KP6:
                                MoveX = 1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_KP8:
                                MoveY = 1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_KP2:
                                MoveY = -1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_w:
                            case Sdl.SDLK_KP9:
                                MoveZ = 1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_s:
                            case Sdl.SDLK_KP3:
                                MoveZ = -1;
                                ReducedMode = false;
                                break;
                            case Sdl.SDLK_KP5:
                                ResetCamera();
                                break;
                            case Sdl.SDLK_F1:
                                Renderer.OptionWireframe = !Renderer.OptionWireframe;
                                if (Renderer.OptionWireframe) {
                                    Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
                                } else {
                                    Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
                                } break;
                            case Sdl.SDLK_F2:
                                Renderer.OptionNormals = !Renderer.OptionNormals;
                                break;
                            case Sdl.SDLK_F3:
                                LightingTarget = 1 - LightingTarget;
                                ReducedMode = false;
                                break;
                        } break;
                    // key up
                    case Sdl.SDL_KEYUP:
                        switch (Event.key.keysym.sym) {
                            case Sdl.SDLK_LEFT:
                            case Sdl.SDLK_RIGHT:
                                RotateX = 0;
                                break;
                            case Sdl.SDLK_UP:
                            case Sdl.SDLK_DOWN:
                                RotateY = 0;
                                break;
                            case Sdl.SDLK_a:
                            case Sdl.SDLK_d:
                            case Sdl.SDLK_KP4:
                            case Sdl.SDLK_KP6:
                                MoveX = 0;
                                break;
                            case Sdl.SDLK_KP8:
                            case Sdl.SDLK_KP2:
                                MoveY = 0;
                                break;
                            case Sdl.SDLK_w:
                            case Sdl.SDLK_s:
                            case Sdl.SDLK_KP9:
                            case Sdl.SDLK_KP3:
                                MoveZ = 0;
                                break;
                        } break;
                }
            }
        }

        // update caption
        private static void UpdateCaption() {
            if (Files.Length != 0) {
                string Title = "";
                for (int i = 0; i < Files.Length; i++) {
                    if (i != 0) Title += ", ";
                    Title += System.IO.Path.GetFileName(Files[i]);
                }
                Sdl.SDL_WM_SetCaption(Title + " - " + Application.ProductName, null);
            } else {
                Sdl.SDL_WM_SetCaption(Application.ProductName, null);
            }
        }

    }

}