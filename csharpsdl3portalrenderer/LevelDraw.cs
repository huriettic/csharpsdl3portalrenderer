using SDL3;
using System.Numerics;
using System.Runtime.InteropServices;
using static LevelFunctions;

public static class LevelDraw
{
    private const int Width = 960;
    private const int Height = 540;
    private const int tileSizeX = 8;
    private const int tileSizeY = 4;
    private const int tilesX = Width / tileSizeX;
    private const int tilesY = Height / tileSizeY;

    static ulong lastCounter = SDL.GetPerformanceCounter();
    static ulong frequency = SDL.GetPerformanceFrequency();
    static float[] depthBuffer = new float[Width * Height];
    static uint[] backBuffer = new uint[Width * Height];
    static nint streamingTexture = 0;

    static int texWidth = 128;
    static int texHeight = 128;
    static int maskX = texWidth - 1;
    static int maskY = texHeight - 1;

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
    static List<Vector3> temporaryTextures = new();
    static List<int> temporaryTriangles = new();

    static List<Vector3> ceilingVertices = new();
    static List<int> ceilingTriangles = new();
    static List<Vector3> floorVertices = new();
    static List<int> floorTriangles = new();
    static List<Vector3> floorTextures = new();
    static List<Vector3> ceilingTextures = new();

    static List<Vector3> OutEdgeVertices = new();
    static Triangle[] Triangles = Array.Empty<Triangle>();

    static bool[] boolEdges = new bool[128];
    static Vector4[] processEdges = new Vector4[128];
    static Vector4[] temporaryEdges = new Vector4[128];

    static bool[] processbool = new bool[256];
    static Vector4[] processvertices = new Vector4[256];
    static Vector3[] processtextures = new Vector3[256];
    static Vector3[] processnormals = new Vector3[256];
    static Vector4[] temporaryvertices = new Vector4[256];
    static Vector3[] temporarytextures = new Vector3[256];
    static Vector3[] temporarynormals = new Vector3[256];

    static List<SectorMeta> Sectors = new();
    static List<List<SectorMeta>> ListOfSectorLists = new();
    static TileTriangle[] ArrayOfTileTriangles = Array.Empty<TileTriangle>();
    static Tile[] tiles = new Tile[tilesX * tilesY];
    static Vector4[] ArrayOfRectangles = Array.Empty<Vector4>();
    static int[] visibleSectors = Array.Empty<int>();
    static int[] contactingSectors = Array.Empty<int>();
    static int frameAlternate = 0;
    static uint[][] textures = Array.Empty<uint[]>();

    static Random rng = new();

    static MathematicalPlane LeftPlane;
    static MathematicalPlane TopPlane;

    static SectorMeta CurrentSector;
    static Vector3 playerStartPosition;

    [Serializable]
    public static class TopLevelLists
    {
        public static List<Vector3> edges = new();
        public static List<int> lines = new();
        public static List<Vector3> vertices = new();
        public static List<Vector3> textures = new();
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

    public static class SDLPlayerController
    {
        public static Vector3 Position = playerStartPosition;
        public static float Speed = 6f;
        public static float Yaw = 0f;
        public static float Pitch = 0f;
        public static float MouseSensitivity = 0.1f;
        public static bool QuitRequested = false;

        public static void HandleMovement(float dt)
        {
            ReadOnlySpan<bool> keys = SDL.GetKeyboardState(out int numKeys);

            if (keys[(int)SDL.Scancode.Escape])
            {
                QuitRequested = true;
            }

            float mx, my;
            SDL.GetRelativeMouseState(out mx, out my);

            Yaw -= mx * MouseSensitivity;
            Pitch -= my * MouseSensitivity;
            Pitch = Math.Clamp(Pitch, -89f, 89f);

            float yawRad = Yaw * (MathF.PI / 180f);
            float pitchRad = Pitch * (MathF.PI / 180f);

            Vector3 forward = new Vector3(-MathF.Sin(yawRad) * MathF.Cos(pitchRad), MathF.Sin(pitchRad), -MathF.Cos(yawRad) * MathF.Cos(pitchRad));
            forward = Vector3.Normalize(forward);

            if (keys[(int)SDL.Scancode.W])
            {
                Position += forward * Speed * dt;
            }
            if (keys[(int)SDL.Scancode.S])
            {
                Position -= forward * Speed * dt;
            }

            Vector3 right = new Vector3(MathF.Sin(yawRad + MathF.PI / 2f), 0f, MathF.Cos(yawRad + MathF.PI / 2f));

            if (keys[(int)SDL.Scancode.A])
            {
                Position -= right * Speed * dt;
            }
            if (keys[(int)SDL.Scancode.D])
            {
                Position += right * Speed * dt;
            }
        }

        public static void SetCollision(int[] contactingSectors, List<ColliderMeta> collision, List<Vector3> vertices, List<int> triangles)
        {
            float Radius = 1.25f;
            Vector3 pos = Position;

            for (int a = 0; a < contactingSectors.Length; a++)
            {
                if (contactingSectors[a] == 0)
                {
                    continue;
                }

                ColliderMeta collide = collision[a];

                for (int t = collide.indicesStartIndex; t < collide.indicesStartIndex + collide.indicesCount; t += 3)
                {
                    Vector3 A = vertices[triangles[t]];
                    Vector3 B = vertices[triangles[t + 1]];
                    Vector3 C = vertices[triangles[t + 2]];

                    Vector3 closest = Vector3.Zero;
                    Vector3 diff = Vector3.Zero;

                    float distSq = TestSphereTriangle(pos, A, B, C, Radius, ref closest, ref diff);

                    if (distSq > Radius * Radius)
                    {
                        continue;
                    }

                    float dist = MathF.Sqrt(distSq);

                    Vector3 normal;
                    float penetration;

                    if (dist > 1e-6f)
                    {
                        normal = diff / dist;
                        penetration = Radius - dist;
                    }
                    else
                    {
                        normal = Vector3.Normalize(Vector3.Cross(B - A, C - A));
                        penetration = Radius;
                    }

                    pos += normal * penetration;
                }
            }

            Position = pos;
        }

        public static void UpdateCamera()
        {
            Camera3D.Rotation = new Vector3(Pitch * (MathF.PI / 180f), Yaw * (MathF.PI / 180f), 0f);
            Camera3D.Position = new Vector3(Position.X + 0.01f, Position.Y, Position.Z);
            Camera3D.UpdateMatrices();
        }
    }

    public static float GetDeltaTime()
    {
        ulong currentCounter = SDL.GetPerformanceCounter();
        float dt = (float)(currentCounter - lastCounter) / frequency;
        lastCounter = currentCounter;
        return dt;
    }

    static void Main(string[] args)
    {
        string levelPath = Path.GetFullPath("assets/twohallways-clear.txt");

        if (!File.Exists(levelPath))
        {
            Console.WriteLine("Level file not found!");
            return;
        }

        string sdl3Path = Path.GetFullPath("SDL3.dll");

        if (!File.Exists(sdl3Path))
        {
            Console.WriteLine("SDL 3 not found!");
            return;
        }

        string csSDL3Path = Path.GetFullPath("SDL3-CS.dll");

        if (!File.Exists(csSDL3Path))
        {
            Console.WriteLine("SDL 3 Sharp not found!");
            return;
        }

        LoadFromFile(levelPath, vertices, sectors, starts);

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

        InitializeTiles(tilesX, tilesY, tileSizeX, tileSizeY, tiles);

        ListOfSectorLists = MakeLists();
        ArrayOfTileTriangles = new TileTriangle[tiles.Length * 128];
        Triangles = new Triangle[TopLevelLists.sectors.Count * 128];
        ArrayOfRectangles = new Vector4[TopLevelLists.sectors.Count * 32];
        visibleSectors = VisibleSectorsMake(TopLevelLists.sectors);
        contactingSectors = contactSectorsMake(TopLevelLists.sectors);
        textures = new uint[4][];

        for ( int i = 0; i < textures.Length; i++)
        {
            textures[i] = new uint[texWidth * texHeight];
        }

        contactingSectors[CurrentSector.sectorId] = 1;

        if (!SDL.Init(SDL.InitFlags.Video))
        {
            Console.WriteLine("Failed to init SDL: " + SDL.GetError());
            return;
        }

        nint window = SDL.CreateWindow("Software Tile Rasterizer", Width, Height, 0);
        if (window == 0)
        {
            Console.WriteLine("SDL.CreateWindow failed: " + SDL.GetError());
            SDL.Quit();
            return;
        }
        nint renderer = SDL.CreateRenderer(window, null);
        if (renderer == 0)
        {
            Console.WriteLine("SDL.CreateRenderer failed: " + SDL.GetError());
            SDL.DestroyWindow(window);
            SDL.Quit();
            return;
        }
        streamingTexture = SDL.CreateTexture(renderer, SDL.PixelFormat.ABGR8888, SDL.TextureAccess.Streaming, Width, Height);
        SDL.SetRenderVSync(renderer, 1);
        SDL.SetWindowRelativeMouseMode(window, true);
        nint white = SDL.LoadPNG("assets/White.png");
        nint green = SDL.LoadPNG("assets/Green.png");
        nint grey = SDL.LoadPNG("assets/Grey.png");
        nint red = SDL.LoadPNG("assets/Red.png");
        SDL.Surface whitetex = Marshal.PtrToStructure<SDL.Surface>(white);
        SDL.Surface greentex = Marshal.PtrToStructure<SDL.Surface>(green);
        SDL.Surface greytex = Marshal.PtrToStructure<SDL.Surface>(grey);
        SDL.Surface redtex = Marshal.PtrToStructure<SDL.Surface>(red);
        Marshal.Copy(whitetex.Pixels, (int[])(object)textures[0], 0, textures[0].Length);
        Marshal.Copy(greentex.Pixels, (int[])(object)textures[1], 0, textures[1].Length);
        Marshal.Copy(greytex.Pixels, (int[])(object)textures[2], 0, textures[2].Length);
        Marshal.Copy(redtex.Pixels, (int[])(object)textures[3], 0, textures[3].Length);
        SDL.DestroySurface(white);
        SDL.DestroySurface(green);
        SDL.DestroySurface(grey);
        SDL.DestroySurface(red);
        float sunPitch = 50.0f;
        float sunYaw = -30.0f;
        Vector3 lightDir = CalculateSunDirection(sunPitch, sunYaw);
        bool running = true;
        while (running)
        {
            while (SDL.PollEvent(out var ev))
            {
                if ((SDL.EventType)ev.Type == SDL.EventType.Quit)
                {
                    running = false;
                }
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
            CurrentSector, ListOfSectorLists, ArrayOfRectangles, visibleSectors,
            TopLevelLists.portals, planeDistance, TopLevelLists.planes,
            TopLevelLists.sectors, Sectors, OutEdgeVertices,
            Camera3D.Position, Camera3D.ViewProjectionMatrix,
            TopLevelLists.edges, TopLevelLists.lines,
            processEdges, temporaryEdges, boolEdges
            );
            SetTriangles(
            visibleSectors, TopLevelLists.meshes, ArrayOfRectangles,
            TopLevelLists.vertices, TopLevelLists.textures, TopLevelLists.normals,
            TopLevelLists.indices, Camera3D.Position,
            processbool, processvertices, temporaryvertices,
            processtextures, temporarytextures,
            processnormals, temporarynormals,
            Triangles, Camera3D.ViewProjectionMatrix, Width, Height, ArrayOfTileTriangles, tiles, tilesX, tilesY, frameAlternate, texWidth, texHeight, tileSizeX, tileSizeY, lightDir
            );
            Array.Fill(depthBuffer, float.PositiveInfinity);
            Array.Clear(backBuffer, 0, backBuffer.Length);

            DrawTiles(tiles, ArrayOfTileTriangles, Triangles, Width, Height, texWidth, texHeight, maskX, maskY, textures, backBuffer, depthBuffer);
            
            ReadOnlySpan<byte> pixelBytes = MemoryMarshal.AsBytes(backBuffer.AsSpan());
            SDL.UpdateTexture(streamingTexture, IntPtr.Zero, pixelBytes, Width * sizeof(uint));
            SDL.RenderClear(renderer);
            SDL.RenderTexture(renderer, streamingTexture, IntPtr.Zero, IntPtr.Zero);
            SDL.RenderPresent(renderer);

            frameAlternate = 1 - frameAlternate;
        }
        SDL.DestroyTexture(streamingTexture);
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }
}
