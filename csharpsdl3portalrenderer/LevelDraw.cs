using System.Numerics;
using System.Runtime.InteropServices;
using SDL3;
using static LevelFunctions;

public static class LevelDraw
{
    private const int Width = 640;
    private const int Height = 360;
    private const int tileSize = 8;
    private const int tilesX = Width / tileSize;
    private const int tilesY = Height / tileSize;

    static ulong lastCounter = SDL.GetPerformanceCounter();
    static ulong frequency = SDL.GetPerformanceFrequency();
    static float[] depthBuffer = new float[Width * Height];
    static uint[] backBuffer = new uint[Width * Height];
    static nint streamingTexture = 0;

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
    static List<int> temporaryColors = new();

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
    static List<List<Triangle>> ListOfTriangleLists = new();
    static Tile[] tiles = new Tile[tilesX * tilesY];
    static Vector4[][] ArrayOfRectangleArrays = Array.Empty<Vector4[]>();
    static int[] visibleSectors = Array.Empty<int>();
    static int[] contactingSectors = Array.Empty<int>();

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
        public static List<Vector2> textures = new();
        public static List<Vector3> normals = new();
        public static List<int> indices = new();
        public static List<int> colors = new();
        public static List<MeshMeta> meshes = new();
        public static List<MathematicalPlane> planes = new();
        public static List<PortalMeta> portals = new();
        public static List<ColliderMeta> collision = new();
        public static List<SectorMeta> sectors = new();
        public static List<StartPosition> positions = new();
    }

    public static void InitializeTiles()
    {
        int tilesListCounter = 0;

        for (int y = 0; y < tilesY; y++)
        {
            for (int x = 0; x < tilesX; x++)
            {
                int index = y * tilesX + x;

                tiles[index] = new Tile
                {
                    listOfTriangles = tilesListCounter,
                    minx = x * 8,
                    miny = y * 8,
                    maxx = (x + 1) * 8,
                    maxy = (y + 1) * 8
                };

                tilesListCounter += 1;
            }
        }
    }

    public struct Tile
    {
        public int listOfTriangles;
        public int minx;
        public int miny;
        public int maxx;
        public int maxy;
    };

    public static void BinTriangles(List<Triangle> allTriangles, List<List<Triangle>> listOfTriangleLists)
    {
        for (int i = 0; i < listOfTriangleLists.Count; i++)
        {
            listOfTriangleLists[i].Clear();
        }

        for (int i = 0; i < allTriangles.Count; i++)
        {
            Triangle tri = allTriangles[i];

            int startTileX = Math.Clamp(tri.xmin / 8, 0, tilesX - 1);
            int endTileX = Math.Clamp(tri.xmax / 8, 0, tilesX - 1);
            int startTileY = Math.Clamp(tri.ymin / 8, 0, tilesY - 1);
            int endTileY = Math.Clamp(tri.ymax / 8, 0, tilesY - 1);

            for (int y = startTileY; y <= endTileY; y++)
            {
                for (int x = startTileX; x <= endTileX; x++)
                {
                    int tileIndex = y * tilesX + x;

                    int targetListIndex = tiles[tileIndex].listOfTriangles;

                    listOfTriangleLists[targetListIndex].Add(tri);
                }
            }
        }
    }

    public static Vector3 CalculateSunDirection(float pitchDegrees, float yawDegrees)
    {
        float pitchRad = pitchDegrees * MathF.PI / 180.0f;
        float yawRad = yawDegrees * MathF.PI / 180.0f;

        float x = -MathF.Cos(pitchRad) * MathF.Sin(yawRad);
        float y = MathF.Sin(pitchRad);
        float z = -MathF.Cos(pitchRad) * MathF.Cos(yawRad);

        return Vector3.Normalize(new Vector3(x, y, z));
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

        InitializeTiles();

        ListOfSectorLists = MakeLists();
        ListOfTriangleLists = MakeTriangleLists(tiles.Length);
        ArrayOfRectangleArrays = ArrayOfArraysMake(TopLevelLists.sectors);
        visibleSectors = VisibleSectorsMake(TopLevelLists.sectors);
        contactingSectors = contactSectorsMake(TopLevelLists.sectors);

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
        nint texture = SDL.LoadPNG("assets/texture.png");
        SDL.Surface tex = Marshal.PtrToStructure<SDL.Surface>(texture);
        texWidth = tex.Width;
        texHeight = tex.Height;
        uint[] sampleTexture = new uint[texWidth * texHeight];
        Marshal.Copy(tex.Pixels, (int[])(object)sampleTexture, 0, sampleTexture.Length);
        SDL.DestroySurface(texture);
        float sunPitch = 50.0f;
        float sunYaw = -30.0f;
        Vector3 lightDir = CalculateSunDirection(sunPitch, sunYaw);
        bool running = true;
        while (running)
        {
            while (SDL.PollEvent(out var ev))
            {
                if ((SDL.EventType)ev.Type == SDL.EventType.Quit) running = false;
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
            Triangles, Camera3D.ViewProjectionMatrix, Width, Height
            );
            Array.Fill(depthBuffer, float.PositiveInfinity);
            Array.Clear(backBuffer, 0, backBuffer.Length);

            BinTriangles(Triangles, ListOfTriangleLists);

            Parallel.For(0, tiles.Length, tileX =>
            {
                Tile tile = tiles[tileX];

                int listBT = tile.listOfTriangles;
                List<Triangle> binnedTriangles = ListOfTriangleLists[listBT];

                int binnedCount = binnedTriangles.Count;
                if (binnedCount == 0) 
                {
                    return;
                } 

                int tileMinX = tile.minx;
                int tileMaxX = tile.maxx;
                int tileMinY = tile.miny;
                int tileMaxY = tile.maxy;

                for (int t = 0; t < binnedCount; t++)
                {
                    Triangle tri = binnedTriangles[t];

                    int startY = Math.Max(tileMinY, tri.ymin);
                    int endY = Math.Min(tileMaxY - 1, tri.ymax);
                    int startX = Math.Max(tileMinX, tri.xmin);
                    int endX = Math.Min(tileMaxX - 1, tri.xmax);

                    if (startX > endX || startY > endY)
                    {
                        continue;
                    }
                    for (int y = startY; y <= endY; y++)
                    {
                        float py = y + 0.5f;
                        int rowOffset = y * Width;

                        for (int x = startX; x <= endX; x++)
                        {
                            float px = x + 0.5f;
                            int pixelX = rowOffset + x;

                            float edge0 = tri.edgeA0 * px + tri.edgeB0 * py + tri.edgeC0;
                            float edge1 = tri.edgeA1 * px + tri.edgeB1 * py + tri.edgeC1;
                            float edge2 = tri.edgeA2 * px + tri.edgeB2 * py + tri.edgeC2;

                            if (edge0 > 0f || edge1 > 0f || edge2 > 0f) 
                            {
                                continue;
                            }

                            float weight0 = edge0 * tri.invArea;
                            float weight1 = edge1 * tri.invArea;
                            float weight2 = edge2 * tri.invArea;

                            float iweight = weight0 * tri.invw0 + weight1 * tri.invw1 + weight2 * tri.invw2;
                            float z = (weight0 * tri.invz0 + weight1 * tri.invz1 + weight2 * tri.invz2) / iweight;

                            if (z >= depthBuffer[pixelX])
                            {
                                continue;
                            }
                            depthBuffer[pixelX] = z;

                            Vector3 normal = (weight0 * tri.normal0 * tri.invw0 +
                                              weight1 * tri.normal1 * tri.invw1 +
                                              weight2 * tri.normal2 * tri.invw2) / iweight;
                            Vector3 N = Vector3.Normalize(normal);
                            float diffuse = Vector3.Dot(N, lightDir) * 0.5f + 0.5f;
                            diffuse = MathF.Min(1.0f, (diffuse * diffuse) + 0.2f);

                            Vector2 uv = (weight0 * tri.invuv0 + weight1 * tri.invuv1 + weight2 * tri.invuv2) / iweight;
                            float uWrapped = uv.X - MathF.Floor(uv.X);
                            float vWrapped = uv.Y - MathF.Floor(uv.Y);

                            int tx = Math.Clamp((int)(uWrapped * (texWidth - 1)), 0, texWidth - 1);
                            int ty = Math.Clamp((int)(vWrapped * (texHeight - 1)), 0, texHeight - 1);

                            uint col = sampleTexture[ty * texWidth + tx];

                            byte r = (byte)Math.Clamp(((col >> 0) & 255) * diffuse, 0, 255);
                            byte g = (byte)Math.Clamp(((col >> 8) & 255) * diffuse, 0, 255);
                            byte b = (byte)Math.Clamp(((col >> 16) & 255) * diffuse, 0, 255);
                            byte a = (byte)((col >> 24) & 255);

                            backBuffer[pixelX] = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                        }
                    }
                }
            });

            ReadOnlySpan<byte> pixelBytes = MemoryMarshal.AsBytes(backBuffer.AsSpan());
            SDL.UpdateTexture(streamingTexture, IntPtr.Zero, pixelBytes, Width * sizeof(uint));
            SDL.RenderClear(renderer);
            SDL.RenderTexture(renderer, streamingTexture, IntPtr.Zero, IntPtr.Zero);
            SDL.RenderPresent(renderer);
        }
        SDL.DestroyTexture(streamingTexture);
        SDL.DestroyRenderer(renderer);
        SDL.DestroyWindow(window);
        SDL.Quit();
    }
}
