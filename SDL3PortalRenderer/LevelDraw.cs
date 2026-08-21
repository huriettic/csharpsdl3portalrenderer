using System.Numerics;
using System.Runtime.InteropServices;
using static LevelFunctions;
using static SDL3.SDL;

public static class LevelDraw
{
    [Serializable]
    public static class TopLevelLists
    {
        public static List<Vector3> edges = new();
        public static List<int> lines = new();
        public static List<Vector3> vertices = new();
        public static List<Vector2> textures = new();
        public static List<Vector3> normals = new();
        public static List<int> indices = new();
        public static List<MeshMeta> meshes = new();
        public static List<MathematicalPlane> planes = new();
        public static List<PortalMeta> portals = new();
        public static List<ColliderMeta> collision = new();
        public static List<SectorMeta> sectors = new();
        public static List<StartPosition> positions = new();
    }

    public static class Camera3D
    {
        public static Vector3 Position = Vector3.Zero;
        public static Vector3 Rotation = Vector3.Zero;

        public static float FovRadians = MathF.PI / 3f;
        public static float AspectRatio = 16f / 9f;
        public static float NearClip = 0.3f;
        public static float FarClip = 1000f;

        public static Matrix4x4 ViewMatrix;
        public static Matrix4x4 ProjectionMatrix;
        public static Matrix4x4 ViewProjectionMatrix;

        public static void UpdateMatrices()
        {
            Quaternion camRot = Quaternion.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);

            Matrix4x4 cameraWorld = Matrix4x4.CreateFromQuaternion(camRot) * Matrix4x4.CreateTranslation(Position);

            Matrix4x4.Invert(cameraWorld, out ViewMatrix);

            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FovRadians, AspectRatio, NearClip, FarClip);

            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
        }
    }

    //public static class SDLPlayerController
    //{
    //    public static Vector3 Position = playerStartPosition;
    //    public static float Speed = 6f;

    //    public static float Yaw = 180f;
    //    public static float Pitch = 0f;

    //    public static bool QuitRequested = false;

    //    public static unsafe void HandleMovement(float dt)
    //    {
    //        int numKeys;
    //        byte* keys = (byte*)SDL_GetKeyboardState(out numKeys);

    //        bool Down(SDL_Scancode sc) => keys[(int)sc] != 0;

    //        if (Down(SDL_Scancode.SDL_SCANCODE_ESCAPE))
    //        {
    //            QuitRequested = true;
    //        }

    //        if (Down(SDL_Scancode.SDL_SCANCODE_A))
    //        {
    //            Yaw += 90f * dt;
    //        }

    //        if (Down(SDL_Scancode.SDL_SCANCODE_D))
    //        {
    //            Yaw -= 90f * dt;
    //        }

    //        if (Down(SDL_Scancode.SDL_SCANCODE_Q))
    //        {
    //            Pitch += 60f * dt;
    //        }

    //        if (Down(SDL_Scancode.SDL_SCANCODE_E))
    //        {
    //            Pitch -= 60f * dt;
    //        }

    //        Pitch = Math.Clamp(Pitch, -89f, 89f);

    //        float yawRad = Yaw * (MathF.PI / 180f);
    //        float pitchRad = Pitch * (MathF.PI / 180f);

    //        Vector3 forward = new Vector3(-MathF.Sin(yawRad) * MathF.Cos(pitchRad), MathF.Sin(pitchRad), -MathF.Cos(yawRad) * MathF.Cos(pitchRad));

    //        if (Down(SDL_Scancode.SDL_SCANCODE_W))
    //        {
    //            Position += forward * Speed * dt;
    //        }

    //        if (Down(SDL_Scancode.SDL_SCANCODE_S))
    //        {
    //            Position -= forward * Speed * dt;
    //        }   
    //    }

    //    public static void UpdateCamera()
    //    {
    //        Camera3D.Rotation = new Vector3(Pitch * (MathF.PI / 180f), Yaw * (MathF.PI / 180f), 0f);

    //        Camera3D.Position = Position;
    //        Camera3D.UpdateMatrices();
    //    }
    //}

    public static class SDLPlayerController
    {
        public static Vector3 Position = playerStartPosition;
        public static float Speed = 6f;

        public static float Yaw = 180f;
        public static float Pitch = 0f;

        public static float MouseSensitivity = 0.1f;

        public static bool QuitRequested = false;

        public static unsafe void HandleMovement(float dt)
        {
            int numKeys;
            byte* keys = (byte*)SDL_GetKeyboardState(out numKeys);

            bool Down(SDL_Scancode sc) => keys[(int)sc] != 0;

            if (Down(SDL_Scancode.SDL_SCANCODE_ESCAPE))
            {
                QuitRequested = true;
            }

            float mx, my;
            SDL_GetRelativeMouseState(out mx, out my);

            Yaw -= mx * MouseSensitivity;
            Pitch -= my * MouseSensitivity;

            Pitch = Math.Clamp(Pitch, -89f, 89f);

            float yawRad = Yaw * (MathF.PI / 180f);
            float pitchRad = Pitch * (MathF.PI / 180f);

            Vector3 forward = new Vector3(-MathF.Sin(yawRad) * MathF.Cos(pitchRad), MathF.Sin(pitchRad), -MathF.Cos(yawRad) * MathF.Cos(pitchRad));

            forward = Vector3.Normalize(forward);

            if (Down(SDL_Scancode.SDL_SCANCODE_W))
            {
                Position += forward * Speed * dt;
            }

            if (Down(SDL_Scancode.SDL_SCANCODE_S))
            {
                Position -= forward * Speed * dt;
            }

            Vector3 right = new Vector3(MathF.Sin(yawRad + MathF.PI / 2f), 0f, MathF.Cos(yawRad + MathF.PI / 2f));

            if (Down(SDL_Scancode.SDL_SCANCODE_A))
            {
                Position -= right * Speed * dt;
            }

            if (Down(SDL_Scancode.SDL_SCANCODE_D))
            {
                Position += right * Speed * dt;
            }
        }

        public static void SetCollision(int[] contactingSectors, List<ColliderMeta> collision, List<Vector3> vertices, List<int> triangles)
        {
            float radius = 1.0f;

            for (int a = 0; a < contactingSectors.Length; a++)
            {
                int count = contactingSectors[a];

                if (contactingSectors[a] == 0)
                    continue;

                ColliderMeta collide = collision[a];

                for (int t = collide.indicesStartIndex; t < collide.indicesStartIndex + collide.indicesCount; t += 3)
                {
                    Vector3 A = vertices[triangles[t]];
                    Vector3 B = vertices[triangles[t + 1]];
                    Vector3 C = vertices[triangles[t + 2]];

                    // Find point P on triangle ABC closest to sphere center
                    Vector3 p = ClosestPtPointTriangle(Position, A, B, C);

                    // Sphere and triangle intersect if the (squared) distance from sphere
                    // center to point p is less than the (squared) sphere radius
                    Vector3 v = p - Position;

                    float distSq = Vector3.Dot(v, v);

                    if (distSq <= radius * radius)
                    {
                        float dist = MathF.Sqrt(distSq);
                        float penetration = radius - dist;
                        if (dist > 1e-6f)
                        {
                            v /= dist;
                        }
                        else
                        {
                            v = Vector3.Normalize(Vector3.Cross(B - A, C - A));
                        }

                        Position -= v * penetration;
                    }
                }
            }
        }

        public static void UpdateCamera()
        {
            Camera3D.Rotation = new Vector3(Pitch * (MathF.PI / 180f), Yaw * (MathF.PI / 180f), 0f);

            Camera3D.Position = Position;
            Camera3D.UpdateMatrices();
        }
    }

    public static float GetDeltaTime()
    {
        ulong currentCounter = SDL_GetPerformanceCounter();
        float dt = (float)(currentCounter - lastCounter) / frequency;
        lastCounter = currentCounter;
        return dt;
    }

    static ulong lastCounter = SDL_GetPerformanceCounter();
    static ulong frequency = SDL_GetPerformanceFrequency();

    static int w = 640;
    static int h = 360;

    static float[] depthBuffer = new float[w * h];

    static int texWidth;
    static int texHeight;

    static List<Vector2> vertices = new();
    static List<Sector> sectors = new();
    static List<StartSector> starts = new();

    static double Ceiling;
    static double Floor;
    static bool radius;
    static bool check;
    static float planeDistance;

    static List<Vector3> temporaryVertices = new();
    static List<Vector3> temporaryNormals = new();
    static List<Vector2> temporaryTextures = new();
    static List<int> temporaryTriangles = new();

    static List<Vector3> ceilingVertices = new();
    static List<int> ceilingTriangles = new();
    static List<Vector3> floorVertices = new();
    static List<int> floorTriangles = new();
    static List<Vector2> floorTextures = new();
    static List<Vector2> ceilingTextures = new();

    static List<Vector3> OutEdgeVertices = new();
    static List<Triangle> Triangles = new();

    static bool[] boolEdges = new bool[128];
    static Vector4[] processEdges = new Vector4[128];
    static Vector4[] temporaryEdges = new Vector4[128];

    static bool[] processbool = new bool[256];
    static Vector4[] processvertices = new Vector4[256];
    static Vector2[] processtextures = new Vector2[256];
    static Vector3[] processnormals = new Vector3[256];
    static Vector4[] temporaryvertices = new Vector4[256];
    static Vector2[] temporarytextures = new Vector2[256];
    static Vector3[] temporarynormals = new Vector3[256];

    static List<SectorMeta> Sectors = new();
    static List<List<SectorMeta>> ListOfSectorLists = new();

    static Vector4[][] ArrayOfRectangleArrays = Array.Empty<Vector4[]>();
    static int[] visibleSectors = Array.Empty<int>();
    static int[] contactingSectors = Array.Empty<int>();

    static Random rng = new();

    static MathematicalPlane LeftPlane;
    static MathematicalPlane TopPlane;

    static SectorMeta CurrentSector;
    static Vector3 playerStartPosition;

    static unsafe void Main(string[] args)
    {
        LoadFromFile("assets/twohallways-clear.txt", vertices, sectors, starts);

        BuildObjects(starts, sectors, TopLevelLists.positions);

        BuildGeometry(
            sectors, vertices, Ceiling, Floor,
            temporaryVertices, temporaryTextures, temporaryNormals, temporaryTriangles,
            LeftPlane, TopPlane,
            TopLevelLists.planes, TopLevelLists.edges, TopLevelLists.lines, TopLevelLists.portals,
            ceilingVertices, ceilingTextures, ceilingTriangles,
            floorVertices, floorTextures, floorTriangles,
            TopLevelLists.meshes, TopLevelLists.collision,
            TopLevelLists.vertices, TopLevelLists.textures,
            TopLevelLists.normals, TopLevelLists.indices,
            TopLevelLists.sectors
        );

        CurrentSector = PlayerStart(TopLevelLists.positions, TopLevelLists.sectors, rng, ref playerStartPosition);

        ListOfSectorLists = MakeLists();
        ArrayOfRectangleArrays = ArrayOfArraysMake(TopLevelLists.sectors);
        visibleSectors = VisibleSectorsMake(TopLevelLists.sectors);
        contactingSectors = contactSectorsMake(TopLevelLists.sectors);

        contactingSectors[CurrentSector.sectorId] = 1;

        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
        {
            Console.WriteLine("Failed to init SDL: " + SDL_GetError());
            return;
        }

        nint window = SDL_CreateWindow("SDL3 Window", 0, 0, SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

        if (window == 0)
        {
            Console.WriteLine("SDL_CreateWindow failed: " + SDL_GetError());
            SDL_Quit();
            return;
        }

        nint renderer = SDL_CreateRenderer(window, null);

        if (renderer == 0)
        {
            Console.WriteLine("SDL_CreateRenderer failed: " + SDL_GetError());
            SDL_DestroyWindow(window);
            SDL_Quit();
            return;
        }

        Console.WriteLine("SDL window and renderer created successfully!");

        if (!SDL_SetRenderVSync(renderer, 1))
        {
            Console.WriteLine("Could not set VSync! SDL_Error: " + SDL_GetError());
        }

        if (!SDL_SetRenderLogicalPresentation(renderer, w, h, SDL_RendererLogicalPresentation.SDL_LOGICAL_PRESENTATION_INTEGER_SCALE))
        {
            Console.WriteLine("Could not set logical presentation size! SDL_Error: " + SDL_GetError());
        }

        if(!SDL_SetWindowRelativeMouseMode(window, true))
        {
            Console.WriteLine("Could not set mouse mode! SDL_Error: " + SDL_GetError());
        }

        nint texture = (nint)SDL_LoadSurface("assets/texture.png");
        if (texture == 0)
        {
            Console.WriteLine("Failed to load PNG surface: " + SDL_GetError());
        }

        SDL_Surface tex = Marshal.PtrToStructure<SDL_Surface>(texture);

        if (tex.pixels == IntPtr.Zero)
        {
            Console.WriteLine("surf.pixels is NULL — no pixel data.");
            return;
        }

        texWidth = tex.w;
        texHeight = tex.h;

        uint[] sampleTexture = new uint[texWidth * texHeight];

        var texPixels = new ReadOnlySpan<uint>((uint*)tex.pixels, sampleTexture.Length);

        texPixels.CopyTo(sampleTexture);
        
        uint first = sampleTexture[0];
        Console.WriteLine($"First pixel: 0x{first:X8}");

        SDL_DestroySurface(texture);

        bool running = true;
        SDL_Event evt;

        while (running)
        {
            while (SDL_PollEvent(out evt))
            {
                if (evt.type == (uint)SDL_EventType.SDL_EVENT_QUIT)
                    running = false;
            }

            float dt = GetDeltaTime();

            SDLPlayerController.HandleMovement(dt);
            SDLPlayerController.SetCollision(contactingSectors, TopLevelLists.collision, TopLevelLists.vertices, TopLevelLists.indices);
            SDLPlayerController.UpdateCamera();

            if (SDLPlayerController.QuitRequested)
            {
                running = false;
            }

            Array.Clear(contactingSectors, 0, contactingSectors.Length);

            CurrentSector = GetSectors(
                CurrentSector, Sectors, ListOfSectorLists, contactingSectors,
                TopLevelLists.portals, TopLevelLists.sectors,
                Camera3D.Position, radius, check, TopLevelLists.planes
            );

            Array.Clear(visibleSectors, 0, visibleSectors.Length);

            GetPortals(
                CurrentSector, ListOfSectorLists, ArrayOfRectangleArrays, visibleSectors,
                TopLevelLists.portals, planeDistance, TopLevelLists.planes,
                TopLevelLists.sectors, Sectors, OutEdgeVertices,
                Camera3D.Position, Camera3D.ViewProjectionMatrix,
                TopLevelLists.edges, TopLevelLists.lines,
                processEdges, temporaryEdges, boolEdges
            );

            Triangles.Clear();

            SetTriangles(
                visibleSectors, TopLevelLists.meshes, ArrayOfRectangleArrays,
                TopLevelLists.vertices, TopLevelLists.textures, TopLevelLists.normals,
                TopLevelLists.indices, Camera3D.Position,
                processbool, processvertices, temporaryvertices,
                processtextures, temporarytextures,
                processnormals, temporarynormals,
                Triangles, Camera3D.ViewProjectionMatrix
            );

            Array.Fill(depthBuffer, float.PositiveInfinity);

            SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL_RenderClear(renderer);

            foreach (Triangle tri in Triangles)
            {
                float invw0 = 1.0f / tri.c0.W;
                float invw1 = 1.0f / tri.c1.W;
                float invw2 = 1.0f / tri.c2.W;

                float invz0 = tri.c0.Z * invw0;
                float invz1 = tri.c1.Z * invw1;
                float invz2 = tri.c2.Z * invw2;

                Vector2 invuv0 = new(tri.uv0.X * invw0, tri.uv0.Y * invw0);
                Vector2 invuv1 = new(tri.uv1.X * invw1, tri.uv1.Y * invw1);
                Vector2 invuv2 = new(tri.uv2.X * invw2, tri.uv2.Y * invw2);

                Vector2 screen0 = new((tri.c0.X * invw0 * 0.5f + 0.5f) * w, (1.0f - (tri.c0.Y * invw0 * 0.5f + 0.5f)) * h);
                Vector2 screen1 = new((tri.c1.X * invw1 * 0.5f + 0.5f) * w, (1.0f - (tri.c1.Y * invw1 * 0.5f + 0.5f)) * h);
                Vector2 screen2 = new((tri.c2.X * invw2 * 0.5f + 0.5f) * w, (1.0f - (tri.c2.Y * invw2 * 0.5f + 0.5f)) * h);

                float area = (screen0.Y - screen1.Y) * screen2.X + (screen1.X - screen0.X) * screen2.Y + (screen0.X * screen1.Y - screen0.Y * screen1.X);

                if (MathF.Abs(area) < 1e-5f || area > 0f)
                {
                    continue;
                }

                float invArea = 1.0f / area;

                int xmin = (int)MathF.Floor(MathF.Min(screen0.X, MathF.Min(screen1.X, screen2.X)));
                int ymin = (int)MathF.Floor(MathF.Min(screen0.Y, MathF.Min(screen1.Y, screen2.Y)));
                int xmax = (int)MathF.Ceiling(MathF.Max(screen0.X, MathF.Max(screen1.X, screen2.X)));
                int ymax = (int)MathF.Ceiling(MathF.Max(screen0.Y, MathF.Max(screen1.Y, screen2.Y)));

                xmin = Math.Clamp(xmin, 0, w - 1);
                ymin = Math.Clamp(ymin, 0, h - 1);
                xmax = Math.Clamp(xmax, 0, w - 1);
                ymax = Math.Clamp(ymax, 0, h - 1);

                for (int y = ymin; y <= ymax; y++)
                {
                    float py = y + 0.5f;

                    for (int x = xmin; x <= xmax; x++)
                    {
                        float px = x + 0.5f;

                        float edge0 = (screen1.Y - screen2.Y) * px + (screen2.X - screen1.X) * py + (screen1.X * screen2.Y - screen1.Y * screen2.X);
                        float edge1 = (screen2.Y - screen0.Y) * px + (screen0.X - screen2.X) * py + (screen2.X * screen0.Y - screen2.Y * screen0.X);
                        float edge2 = (screen0.Y - screen1.Y) * px + (screen1.X - screen0.X) * py + (screen0.X * screen1.Y - screen0.Y * screen1.X);

                        if (edge0 > 0f || edge1 > 0f || edge2 > 0f)
                        {
                            continue;
                        }

                        float weight0 = edge0 * invArea;
                        float weight1 = edge1 * invArea;
                        float weight2 = edge2 * invArea;

                        float iweight = weight0 * invw0 + weight1 * invw1 + weight2 * invw2;

                        float z = (weight0 * invz0 + weight1 * invz1 + weight2 * invz2) / iweight;

                        int zd = y * w + x;

                        if (z >= depthBuffer[zd])
                        {
                            continue;
                        }

                        Vector2 uv = (weight0 * invuv0 + weight1 * invuv1 + weight2 * invuv2) / iweight;

                        float uWrapped = uv.X - MathF.Floor(uv.X);
                        float vWrapped = uv.Y - MathF.Floor(uv.Y);

                        int tx = (int)(uWrapped * (texWidth - 1));
                        int ty = (int)(vWrapped * (texHeight - 1));

                        tx = Math.Clamp(tx, 0, texWidth - 1);
                        ty = Math.Clamp(ty, 0, texHeight - 1);

                        uint col = sampleTexture[ty * texWidth + tx];

                        SDL_SetRenderDrawColor(renderer,
                            (byte)((col >> 0) & 255),
                            (byte)((col >> 8) & 255),
                            (byte)((col >> 16) & 255),
                            (byte)((col >> 24) & 255));

                        SDL_RenderPoint(renderer, x, y);

                        depthBuffer[zd] = z;
                    }
                }
            }

            SDL_RenderPresent(renderer);
        }

        SDL_DestroyRenderer(renderer);
        SDL_DestroyWindow(window);
        SDL_Quit();
    }
}
