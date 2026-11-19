using System.Numerics;

public struct Triangle
{
    public Vector2 screen0, screen1, screen2;
    public Vector2 invuv0, invuv1, invuv2;
    public float invz0, invz1, invz2;
    public float invw0, invw1, invw2;
    public float edgeA0, edgeB0, edgeC0;
    public float edgeA1, edgeB1, edgeC1;
    public float edgeA2, edgeB2, edgeC2;
    public float invwx, invwy;
    public float invzx, invzy;
    public float invux, invuy;
    public float invvx, invvy;
    public float diffuse;
    public float invArea;
    public int texture;
};

[Serializable]
public struct Sector
{
    public float floorHeight;
    public float ceilingHeight;
    public int vertexStartIndex;
    public int vertexCount;
    public int wallTypesIndexStart;
    public int wallTypesCount;
};

[Serializable]
public struct StartSector
{
    public Vector2 location;
    public float angle;
    public int sector;
};

[Serializable]
public struct MathematicalPlane
{
    public Vector3 normal;
    public float distance;
};

[Serializable]
public struct StartPosition
{
    public Vector3 playerStart;
    public int sectorId;
};

[Serializable]
public struct PortalMeta
{
    public int edgeStartIndex;
    public int edgeCount;

    public int connectedSectorId;
    public int sectorId;

    public int plane;
    public int portalId;
};

[Serializable]
public struct SectorMeta
{
    public int portalStartIndex;
    public int portalCount;

    public int planeStartIndex;
    public int planeCount;

    public int rectangle;
    public int sectorId;
};

public struct EdgeVertex
{
    public Vector4 c;
    public bool b;
};

[Serializable]
public struct ClipVertex
{
    public Vector4 c;
    public Vector3 t;
    public Vector3 n;
    public bool b;
};

[Serializable]
public struct Vertex
{
    public Vector3 v;
    public Vector3 t;
    public Vector3 n;
};

[Serializable]
public struct MeshMeta
{
    public int verticesStartIndex;
    public int verticesCount;

    public int indicesStartIndex;
    public int indicesCount;

    public int triangles;
    public int sectorId;
};

[Serializable]
public struct TileTriangle
{
    public int triangleIndex;
    public int minx;
    public int miny;
    public int maxx;
    public int maxy;
};

public static class LevelFunctions
{
    public static Vector3 CalculateSunDirection(float pitchDegrees, float yawDegrees)
    {
        float pitchRad = pitchDegrees * MathF.PI / 180.0f;
        float yawRad = yawDegrees * MathF.PI / 180.0f;

        float x = -MathF.Cos(pitchRad) * MathF.Sin(yawRad);
        float y = MathF.Sin(pitchRad);
        float z = MathF.Cos(pitchRad) * MathF.Cos(yawRad);

        return Vector3.Normalize(new Vector3(x, y, z));
    }

    public static List<List<SectorMeta>> MakeLists()
    {
        List<List<SectorMeta>> ListOfSectorLists = new List<List<SectorMeta>>();

        for (int i = 0; i < 2; i++)
        {
            ListOfSectorLists.Add(new List<SectorMeta>());
        }

        return ListOfSectorLists;
    }

    public static int[] contactSectorsMake(List<SectorMeta> sectors)
    {
        int[] contactingSectors = new int[sectors.Count];

        return contactingSectors;
    }

    public static int[] VisibleSectorsMake(List<SectorMeta> sectors)
    {
        int[] visibleSectors = new int[sectors.Count];

        return visibleSectors;
    }

    public static void GetPortals(SectorMeta ASector, List<List<SectorMeta>> ListOfSectorLists, Vector4[] ArrayOfRectangles, int[] visibleSectors, List<PortalMeta> portals, float planeDistance, List<MathematicalPlane> planes, List<SectorMeta> sectors, List<SectorMeta> contact, List<Vector3> OutEdgeVertices, Vector3 CamPosition, Matrix4x4 CamViewProj, List<Vector3> edges, List<int> lines, EdgeVertex[] processBuffer, EdgeVertex[] temporaryBuffer)
    {
        int input = 0;
        int output = 1;

        ListOfSectorLists[input].Clear();
        ListOfSectorLists[output].Clear();

        ArrayOfRectangles[(ASector.sectorId * 32) + ASector.rectangle] = new Vector4(-1.0f, -1.0f, 1.0f, 1.0f);

        visibleSectors[ASector.sectorId] += 1;

        ListOfSectorLists[input].Add(ASector);

        for (int a = 0; a < 4096; a++)
        {
            if (a % 2 == 0)
            {
                input = 0;
                output = 1;
            }
            else
            {
                input = 1;
                output = 0;
            }

            ListOfSectorLists[output].Clear();

            if (ListOfSectorLists[input].Count == 0)
            {
                break;
            }

            for (int b = 0; b < ListOfSectorLists[input].Count; b++)
            {
                SectorMeta sector = ListOfSectorLists[input][b];

                Vector4 rectangleIn = ArrayOfRectangles[(sector.sectorId * 32) + sector.rectangle];

                for (int c = sector.portalStartIndex; c < sector.portalStartIndex + sector.portalCount; c++)
                {
                    PortalMeta polygon = portals[c];

                    planeDistance = GetPlaneSignedDistanceToPoint(planes[polygon.plane], CamPosition);

                    if (planeDistance <= 0)
                    {
                        continue;
                    }

                    int connectedsector = polygon.connectedSectorId;

                    SectorMeta sectorpolygon = sectors[connectedsector];

                    int nextcount = visibleSectors[connectedsector];

                    int connectedstart = sectorpolygon.portalStartIndex;

                    int connectedcount = sectorpolygon.portalCount;

                    if (nextcount >= 32)
                    {
                        continue;
                    }

                    if (sector.sectorId == ASector.sectorId)
                    {
                        if (SectorsContains(sectorpolygon.sectorId, contact))
                        {
                            ArrayOfRectangles[(connectedsector * 32) + nextcount] = rectangleIn;

                            visibleSectors[connectedsector] = nextcount + 1;

                            SectorMeta ContactSector = new SectorMeta
                            {
                                portalStartIndex = connectedstart,
                                portalCount = connectedcount,
                                rectangle = nextcount,
                                sectorId = connectedsector
                            };

                            ListOfSectorLists[output].Add(ContactSector);

                            continue;
                        }
                    }

                    Vector4 rectangleOut = MakeRectangleWithEdges(rectangleIn, polygon, OutEdgeVertices, CamViewProj, edges, lines, processBuffer, temporaryBuffer);

                    if (OutEdgeVertices.Count < 6 || OutEdgeVertices.Count % 2 == 1)
                    {
                        continue;
                    }

                    if (DegenerateRectangle(rectangleOut))
                    {
                        continue;
                    }

                    bool identical = false;

                    for (int i = 0; i < nextcount; i++)
                    {
                        if (RectanglesIdentical(ArrayOfRectangles[(connectedsector * 32) + i], rectangleOut))
                        {
                            identical = true;
                            break;
                        }
                    }

                    if (identical)
                    {
                        continue;
                    }

                    ArrayOfRectangles[(connectedsector * 32) + nextcount] = rectangleOut;

                    visibleSectors[connectedsector] = nextcount + 1;

                    SectorMeta VisibleSector = new SectorMeta
                    {
                        portalStartIndex = connectedstart,
                        portalCount = connectedcount,
                        rectangle = nextcount,
                        sectorId = connectedsector
                    };

                    ListOfSectorLists[output].Add(VisibleSector);
                }
            }
        }
    }

    public static void SetTriangles(int[] visibleSectors, List<MeshMeta> meshes, ClipVertex[] processPipeline, ClipVertex[] temporaryPipeline, Vector4[] ArrayOfRectangles, List<Vertex> vertices, List<int> indices, Vector3 CamPosition, Triangle[] ArrayOfTriangles, Matrix4x4 CamViewProj, int Width, int Height, TileTriangle[] ArrayOfTileTriangles, int tilesX, int tilesY, int texWidth, int texHeight, int tileSizeX, int tileSizeY, Vector3 lightDir, int[] triangleCount)
    {
        for (int a = 0; a < visibleSectors.Length; a++)
        {
            int triangles = 0;
            int count = visibleSectors[a];

            if (count == 0)
            {
                continue;
            }

            MeshMeta mesh = meshes[a];

            for (int b = mesh.indicesStartIndex; b < mesh.indicesStartIndex + mesh.indicesCount; b += 3)
            {
                Vertex vert0 = vertices[indices[b]];
                Vertex vert1 = vertices[indices[b + 1]];
                Vertex vert2 = vertices[indices[b + 2]];

                Vector3 edge1 = vert1.v - vert0.v;
                Vector3 edge2 = vert2.v - vert0.v;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                Vector3 camDir = Vector3.Normalize(CamPosition - vert0.v);
                float triangleDir = Vector3.Dot(normal, camDir);

                if (triangleDir < 0)
                {
                    continue;
                }

                int processCount = 3;
                processPipeline[0] = new ClipVertex { c = ConvertWorldToClip(CamViewProj, vert0.v), t = vert0.t, n = vert0.n, b = true };
                processPipeline[1] = new ClipVertex { c = ConvertWorldToClip(CamViewProj, vert1.v), t = vert1.t, n = vert1.n, b = true };
                processPipeline[2] = new ClipVertex { c = ConvertWorldToClip(CamViewProj, vert2.v), t = vert2.t, n = vert2.n, b = true };

                for (int c = 0; c < 2; c++)
                {
                    int temporaryCount = 0;
                    int addedTriangles = 0;

                    for (int d = 0; d < processCount; d += 3)
                    {
                        if (!processPipeline[d].b && !processPipeline[d + 1].b && !processPipeline[d + 2].b)
                        {
                            continue;
                        }

                        ClipVertex cv0 = processPipeline[d];
                        ClipVertex cv1 = processPipeline[d + 1];
                        ClipVertex cv2 = processPipeline[d + 2];

                        float d0, d1, d2;
                        switch (c)
                        {
                            case 0: // Near Plane Clipping
                                d0 = cv0.c.Z;
                                d1 = cv1.c.Z;
                                d2 = cv2.c.Z;
                                break;
                            case 1: // Far Plane Clipping
                                d0 = cv0.c.W - cv0.c.Z;
                                d1 = cv1.c.W - cv1.c.Z;
                                d2 = cv2.c.W - cv2.c.Z;
                                break;
                            default:
                                d0 = 0; d1 = 0; d2 = 0;
                                break;
                        }

                        bool b0 = d0 >= 0;
                        bool b1 = d1 >= 0;
                        bool b2 = d2 >= 0;

                        if (b0 && b1 && b2)
                        {
                            continue;
                        }
                        else if ((b0 && !b1 && !b2) || (!b0 && b1 && !b2) || (!b0 && !b1 && b2))
                        {
                            ClipVertex inV, outV1, outV2;
                            float inD, outD1, outD2;

                            if (b0) { inV = cv0; outV1 = cv1; outV2 = cv2; inD = d0; outD1 = d1; outD2 = d2; }
                            else if (b1) { inV = cv1; outV1 = cv2; outV2 = cv0; inD = d1; outD1 = d2; outD2 = d0; }
                            else { inV = cv2; outV1 = cv0; outV2 = cv1; inD = d2; outD1 = d0; outD2 = d1; }

                            float t1 = inD / (inD - outD1);
                            float t2 = inD / (inD - outD2);

                            temporaryPipeline[temporaryCount] = inV;

                            temporaryPipeline[temporaryCount + 1] = new ClipVertex
                            {
                                c = Vector4.Lerp(inV.c, outV1.c, t1),
                                t = Vector3.Lerp(inV.t, outV1.t, t1),
                                n = Vector3.Normalize(Vector3.Lerp(inV.n, outV1.n, t1)),
                                b = true
                            };

                            temporaryPipeline[temporaryCount + 2] = new ClipVertex
                            {
                                c = Vector4.Lerp(inV.c, outV2.c, t2),
                                t = Vector3.Lerp(inV.t, outV2.t, t2),
                                n = Vector3.Normalize(Vector3.Lerp(inV.n, outV2.n, t2)),
                                b = true
                            };

                            temporaryCount += 3;

                            processPipeline[d].b = false;
                            processPipeline[d + 1].b = false;
                            processPipeline[d + 2].b = false;
                            addedTriangles += 1;
                        }
                        else if ((!b0 && b1 && b2) || (b0 && !b1 && b2) || (b0 && b1 && !b2))
                        {
                            ClipVertex inV1, inV2, outV;
                            float inD1, inD2, outD;

                            if (!b0) { outV = cv0; inV1 = cv1; inV2 = cv2; outD = d0; inD1 = d1; inD2 = d2; }
                            else if (!b1) { outV = cv1; inV1 = cv2; inV2 = cv0; outD = d1; inD1 = d2; inD2 = d0; }
                            else { outV = cv2; inV1 = cv0; inV2 = cv1; outD = d2; inD1 = d0; inD2 = d1; }

                            float t1 = inD1 / (inD1 - outD);
                            float t2 = inD2 / (inD2 - outD);

                            ClipVertex vA = new ClipVertex
                            {
                                c = Vector4.Lerp(inV1.c, outV.c, t1),
                                t = Vector3.Lerp(inV1.t, outV.t, t1),
                                n = Vector3.Normalize(Vector3.Lerp(inV1.n, outV.n, t1)),
                                b = true
                            };

                            ClipVertex vB = new ClipVertex
                            {
                                c = Vector4.Lerp(inV2.c, outV.c, t2),
                                t = Vector3.Lerp(inV2.t, outV.t, t2),
                                n = Vector3.Normalize(Vector3.Lerp(inV2.n, outV.n, t2)),
                                b = true
                            };

                            temporaryPipeline[temporaryCount] = inV1;
                            temporaryPipeline[temporaryCount + 1] = inV2;
                            temporaryPipeline[temporaryCount + 2] = vA;

                            temporaryPipeline[temporaryCount + 3] = vA;
                            temporaryPipeline[temporaryCount + 4] = inV2;
                            temporaryPipeline[temporaryCount + 5] = vB;
                            temporaryCount += 6;

                            processPipeline[d].b = false;
                            processPipeline[d + 1].b = false;
                            processPipeline[d + 2].b = false;
                            addedTriangles += 2;
                        }
                        else
                        {
                            processPipeline[d].b = false;
                            processPipeline[d + 1].b = false;
                            processPipeline[d + 2].b = false;
                        }
                    }

                    if (addedTriangles > 0)
                    {
                        for (int e = 0; e < temporaryCount; e++)
                        {
                            processPipeline[processCount + e] = temporaryPipeline[e];
                        }
                        processCount += temporaryCount;
                    }
                }

                for (int f = 0; f < processCount; f += 3)
                {
                    if (processPipeline[f].b && processPipeline[f + 1].b && processPipeline[f + 2].b)
                    {
                        if (triangles >= 128)
                        {
                            continue;
                        }

                        Triangle tri = new Triangle();

                        Vector4 c0 = processPipeline[f].c;
                        Vector4 c1 = processPipeline[f + 1].c;
                        Vector4 c2 = processPipeline[f + 2].c;

                        Vector3 t0 = processPipeline[f].t;
                        Vector3 t1 = processPipeline[f + 1].t;
                        Vector3 t2 = processPipeline[f + 2].t;

                        Vector3 n = processPipeline[f].n;

                        float invw0 = 1.0f / c0.W;
                        float invw1 = 1.0f / c1.W;
                        float invw2 = 1.0f / c2.W;

                        Vector2 screen0 = new((c0.X * invw0 * 0.5f + 0.5f) * Width, (1.0f - (c0.Y * invw0 * 0.5f + 0.5f)) * Height);
                        Vector2 screen1 = new((c1.X * invw1 * 0.5f + 0.5f) * Width, (1.0f - (c1.Y * invw1 * 0.5f + 0.5f)) * Height);
                        Vector2 screen2 = new((c2.X * invw2 * 0.5f + 0.5f) * Width, (1.0f - (c2.Y * invw2 * 0.5f + 0.5f)) * Height);

                        float area = (screen0.Y - screen1.Y) * screen2.X +
                                     (screen1.X - screen0.X) * screen2.Y +
                                     (screen0.X * screen1.Y - screen0.Y * screen1.X);

                        if (MathF.Abs(area) < 1e-5f || area > 0f)
                        {
                            continue;
                        }

                        tri.invz0 = c0.Z * invw0;
                        tri.invz1 = c1.Z * invw1;
                        tri.invz2 = c2.Z * invw2;

                        tri.invuv0 = new(t0.X * invw0, t0.Y * invw0);
                        tri.invuv1 = new(t1.X * invw1, t1.Y * invw1);
                        tri.invuv2 = new(t2.X * invw2, t2.Y * invw2);

                        tri.invw0 = invw0; tri.invw1 = invw1; tri.invw2 = invw2;
                        tri.screen0 = screen0; tri.screen1 = screen1; tri.screen2 = screen2;
                        tri.invArea = 1.0f / area;

                        tri.edgeA0 = tri.screen1.Y - tri.screen2.Y;
                        tri.edgeB0 = tri.screen2.X - tri.screen1.X;
                        tri.edgeC0 = tri.screen1.X * tri.screen2.Y - tri.screen1.Y * tri.screen2.X;

                        tri.edgeA1 = tri.screen2.Y - tri.screen0.Y;
                        tri.edgeB1 = tri.screen0.X - tri.screen2.X;
                        tri.edgeC1 = tri.screen2.X * tri.screen0.Y - tri.screen2.Y * tri.screen0.X;
                        tri.edgeA2 = tri.screen0.Y - tri.screen1.Y;
                        tri.edgeB2 = tri.screen1.X - tri.screen0.X;
                        tri.edgeC2 = tri.screen0.X * tri.screen1.Y - tri.screen0.Y * tri.screen1.X;
                        float a0 = tri.edgeA0 * tri.invArea; float a1 = tri.edgeA1 * tri.invArea; float a2 = tri.edgeA2 * tri.invArea;
                        float b0 = tri.edgeB0 * tri.invArea; float b1 = tri.edgeB1 * tri.invArea; float b2 = tri.edgeB2 * tri.invArea;
                        tri.invuv0.X *= texWidth; tri.invuv0.Y *= texHeight;
                        tri.invuv1.X *= texWidth; tri.invuv1.Y *= texHeight;
                        tri.invuv2.X *= texWidth; tri.invuv2.Y *= texHeight;
                        tri.invwx = a0 * tri.invw0 + a1 * tri.invw1 + a2 * tri.invw2;
                        tri.invwy = b0 * tri.invw0 + b1 * tri.invw1 + b2 * tri.invw2;
                        tri.invzx = a0 * tri.invz0 + a1 * tri.invz1 + a2 * tri.invz2;
                        tri.invzy = b0 * tri.invz0 + b1 * tri.invz1 + b2 * tri.invz2;
                        tri.invux = a0 * tri.invuv0.X + a1 * tri.invuv1.X + a2 * tri.invuv2.X;
                        tri.invuy = b0 * tri.invuv0.X + b1 * tri.invuv1.X + b2 * tri.invuv2.X;
                        tri.invvx = a0 * tri.invuv0.Y + a1 * tri.invuv1.Y + a2 * tri.invuv2.Y;
                        tri.invvy = b0 * tri.invuv0.Y + b1 * tri.invuv1.Y + b2 * tri.invuv2.Y;
                        float rawDiffuse = Vector3.Dot(n, lightDir) * 0.5f + 0.5f;
                        rawDiffuse = MathF.Min(1.0f, (rawDiffuse * rawDiffuse) + 0.2f);
                        tri.diffuse = Math.Clamp(rawDiffuse, 0.0f, 1.0f);
                        tri.texture = (int)t0.Z;
                        ArrayOfTriangles[(a * 128) + triangles] = tri;
                        triangles += 1;
                    }
                }
            }

            for (int g = 0; g < count; g++)
            {
                Vector4 rectangle = ArrayOfRectangles[(a * 32) + g];

                int portalXmin = (int)MathF.Floor((rectangle.X * 0.5f + 0.5f) * Width);
                int portalYmin = (int)MathF.Floor((1.0f - (rectangle.W * 0.5f + 0.5f)) * Height);
                int portalXmax = (int)MathF.Ceiling((rectangle.Z * 0.5f + 0.5f) * Width);
                int portalYmax = (int)MathF.Ceiling((1.0f - (rectangle.Y * 0.5f + 0.5f)) * Height);

                portalXmin = Math.Clamp(portalXmin, 0, Width - 1);
                portalYmin = Math.Clamp(portalYmin, 0, Height - 1);
                portalXmax = Math.Clamp(portalXmax, 0, Width - 1);
                portalYmax = Math.Clamp(portalYmax, 0, Height - 1);

                if (portalXmin >= portalXmax || portalYmin >= portalYmax)
                {
                    continue;
                }

                for (int h = 0; h < triangles; h++)
                {
                    Triangle tri = ArrayOfTriangles[(a * 128) + h];

                    int triXmin = (int)MathF.Floor(MathF.Min(tri.screen0.X, MathF.Min(tri.screen1.X, tri.screen2.X)));
                    int triYmin = (int)MathF.Floor(MathF.Min(tri.screen0.Y, MathF.Min(tri.screen1.Y, tri.screen2.Y)));
                    int triXmax = (int)MathF.Ceiling(MathF.Max(tri.screen0.X, MathF.Max(tri.screen1.X, tri.screen2.X)));
                    int triYmax = (int)MathF.Ceiling(MathF.Max(tri.screen0.Y, MathF.Max(tri.screen1.Y, tri.screen2.Y)));

                    int combinedXmin = Math.Max(triXmin, portalXmin);
                    int combinedYmin = Math.Max(triYmin, portalYmin);
                    int combinedXmax = Math.Min(triXmax, portalXmax);
                    int combinedYmax = Math.Min(triYmax, portalYmax);

                    if (combinedXmin >= combinedXmax || combinedYmin >= combinedYmax)
                    {
                        continue;
                    }

                    int startTileX = Math.Clamp(combinedXmin / tileSizeX, 0, tilesX - 1);
                    int endTileX = Math.Clamp(combinedXmax / tileSizeX, 0, tilesX - 1);
                    int startTileY = Math.Clamp(combinedYmin / tileSizeY, 0, tilesY - 1);
                    int endTileY = Math.Clamp(combinedYmax / tileSizeY, 0, tilesY - 1);

                    float n0X = tri.edgeA0; float n0Y = tri.edgeB0;
                    float n1X = tri.edgeA1; float n1Y = tri.edgeB1;
                    float n2X = tri.edgeA2; float n2Y = tri.edgeB2;

                    float triMin0 = MathF.Min(n0X * tri.screen0.X + n0Y * tri.screen0.Y, n0X * tri.screen2.X + n0Y * tri.screen2.Y);
                    float triMax0 = MathF.Max(n0X * tri.screen0.X + n0Y * tri.screen0.Y, n0X * tri.screen2.X + n0Y * tri.screen2.Y);

                    float triMin1 = MathF.Min(n1X * tri.screen1.X + n1Y * tri.screen1.Y, n1X * tri.screen0.X + n1Y * tri.screen0.Y);
                    float triMax1 = MathF.Max(n1X * tri.screen1.X + n1Y * tri.screen1.Y, n1X * tri.screen0.X + n1Y * tri.screen0.Y);

                    float triMin2 = MathF.Min(n2X * tri.screen2.X + n2Y * tri.screen2.Y, n2X * tri.screen1.X + n2Y * tri.screen1.Y);
                    float triMax2 = MathF.Max(n2X * tri.screen2.X + n2Y * tri.screen2.Y, n2X * tri.screen1.X + n2Y * tri.screen1.Y);

                    for (int y = startTileY; y <= endTileY; y++)
                    {
                        for (int x = startTileX; x <= endTileX; x++)
                        {
                            int tileIndex = y * tilesX + x;
                            int tileCount = tilesX * tilesY;

                            if (triangleCount[tileIndex] >= 128)
                            {
                                continue;
                            }

                            int tileMinX = Math.Max(x * tileSizeX, combinedXmin);
                            int tileMinY = Math.Max(y * tileSizeY, combinedYmin);
                            int tileMaxX = Math.Min((x + 1) * tileSizeX, combinedXmax);
                            int tileMaxY = Math.Min((y + 1) * tileSizeY, combinedYmax);

                            if (tileMinX >= tileMaxX || tileMinY >= tileMaxY)
                            {
                                continue;
                            }

                            float e0c0 = n0X * tileMinX + n0Y * tileMinY + tri.edgeC0;
                            float e0c1 = n0X * tileMaxX + n0Y * tileMinY + tri.edgeC0;
                            float e0c2 = n0X * tileMinX + n0Y * tileMaxY + tri.edgeC0;
                            float e0c3 = n0X * tileMaxX + n0Y * tileMaxY + tri.edgeC0;

                            if (e0c0 > 0f && e0c1 > 0f && e0c2 > 0f && e0c3 > 0f)
                            {
                                continue;
                            }

                            float e1c0 = n1X * tileMinX + n1Y * tileMinY + tri.edgeC1;
                            float e1c1 = n1X * tileMaxX + n1Y * tileMinY + tri.edgeC1;
                            float e1c2 = n1X * tileMinX + n1Y * tileMaxY + tri.edgeC1;
                            float e1c3 = n1X * tileMaxX + n1Y * tileMaxY + tri.edgeC1;

                            if (e1c0 > 0f && e1c1 > 0f && e1c2 > 0f && e1c3 > 0f)
                            {
                                continue;
                            }

                            float e2c0 = n2X * tileMinX + n2Y * tileMinY + tri.edgeC2;
                            float e2c1 = n2X * tileMaxX + n2Y * tileMinY + tri.edgeC2;
                            float e2c2 = n2X * tileMinX + n2Y * tileMaxY + tri.edgeC2;
                            float e2c3 = n2X * tileMaxX + n2Y * tileMaxY + tri.edgeC2;

                            if (e2c0 > 0f && e2c1 > 0f && e2c2 > 0f && e2c3 > 0f)
                            {
                                continue;
                            }

                            if (e0c0 <= 0f && e0c1 <= 0f && e0c2 <= 0f && e0c3 <= 0f &&
                                e1c0 <= 0f && e1c1 <= 0f && e1c2 <= 0f && e1c3 <= 0f &&
                                e2c0 <= 0f && e2c1 <= 0f && e2c2 <= 0f && e2c3 <= 0f)
                            {
                                int fullDestIndex = (tileIndex * 128) + triangleCount[tileIndex];

                                TileTriangle fullTileTri = new TileTriangle
                                {
                                    triangleIndex = (a * 128) + h,

                                    minx = tileMinX,
                                    miny = tileMinY,
                                    maxx = tileMaxX,
                                    maxy = tileMaxY
                                };

                                ArrayOfTileTriangles[fullDestIndex] = fullTileTri;
                                triangleCount[tileIndex] += 1;

                                continue;
                            }

                            float p00 = e0c0 - tri.edgeC0; float p01 = e0c1 - tri.edgeC0;
                            float p02 = e0c2 - tri.edgeC0; float p03 = e0c3 - tri.edgeC0;
                            float tileMinProj0 = MathF.Min(MathF.Min(p00, p01), MathF.Min(p02, p03));
                            float tileMaxProj0 = MathF.Max(MathF.Max(p00, p01), MathF.Max(p02, p03));

                            if (tileMaxProj0 < triMin0 || tileMinProj0 > triMax0)
                            {
                                continue;
                            }

                            float p10 = e1c0 - tri.edgeC1; float p11 = e1c1 - tri.edgeC1;
                            float p12 = e1c2 - tri.edgeC1; float p13 = e1c3 - tri.edgeC1;
                            float tileMinProj1 = MathF.Min(MathF.Min(p10, p11), MathF.Min(p12, p13));
                            float tileMaxProj1 = MathF.Max(MathF.Max(p10, p11), MathF.Max(p12, p13));

                            if (tileMaxProj1 < triMin1 || tileMinProj1 > triMax1)
                            {
                                continue;
                            }

                            float p20 = e2c0 - tri.edgeC2; float p21 = e2c1 - tri.edgeC2;
                            float p22 = e2c2 - tri.edgeC2; float p23 = e2c3 - tri.edgeC2;
                            float tileMinProj2 = MathF.Min(MathF.Min(p20, p21), MathF.Min(p22, p23));
                            float tileMaxProj2 = MathF.Max(MathF.Max(p20, p21), MathF.Max(p22, p23));

                            if (tileMaxProj2 < triMin2 || tileMinProj2 > triMax2)
                            {
                                continue;
                            }

                            int partialDestIndex = (tileIndex * 128) + triangleCount[tileIndex];

                            TileTriangle partialTileTri = new TileTriangle
                            {
                                triangleIndex = (a * 128) + h,

                                minx = tileMinX,
                                miny = tileMinY,
                                maxx = tileMaxX,
                                maxy = tileMaxY
                            };

                            ArrayOfTileTriangles[partialDestIndex] = partialTileTri;
                            triangleCount[tileIndex] += 1;
                        }
                    }
                }
            }
        }
    }

    public static bool RectanglesIdentical(Vector4 a, Vector4 b)
    {
        const float epsilon = 0.001f;

        return MathF.Abs(a.X - b.X) < epsilon && MathF.Abs(a.Y - b.Y) < epsilon && MathF.Abs(a.Z - b.Z) < epsilon && MathF.Abs(a.W - b.W) < epsilon;
    }

    public static bool DegenerateRectangle(Vector4 r)
    {
        return r.X >= r.Z || r.Y >= r.W || (r.Z - r.X) < 0.001f || (r.W - r.Y) < 0.001f;
    }

    public static bool CheckRadius(SectorMeta asector, Vector3 campoint, List<MathematicalPlane> planes)
    {
        for (int i = asector.planeStartIndex; i < asector.planeStartIndex + asector.planeCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(planes[i], campoint) < -0.6f)
            {
                return false;
            }
        }
        return true;
    }

    public static bool CheckSector(SectorMeta asector, Vector3 campoint, List<MathematicalPlane> planes)
    {
        for (int i = asector.planeStartIndex; i < asector.planeStartIndex + asector.planeCount; i++)
        {
            if (GetPlaneSignedDistanceToPoint(planes[i], campoint) < 0)
            {
                return false;
            }
        }
        return true;
    }

    public static bool SectorsContains(int sectorID, List<SectorMeta> Sectors)
    {
        for (int i = 0; i < Sectors.Count; i++)
        {
            if (Sectors[i].sectorId == sectorID)
            {
                return true;
            }
        }
        return false;
    }

    public static SectorMeta GetSectors(SectorMeta ASector, List<SectorMeta> Sectors, List<List<SectorMeta>> ListOfSectorLists, int[] contactingSectors, List<PortalMeta> portals, List<SectorMeta> sectors, Vector3 CamPosition, bool radius, bool check, List<MathematicalPlane> planes)
    {
        SectorMeta CurrentSector = ASector;

        int input = 0;
        int output = 1;

        Sectors.Clear();

        ListOfSectorLists[input].Clear();
        ListOfSectorLists[output].Clear();

        ListOfSectorLists[input].Add(ASector);

        for (int b = 0; b < 4096; b++)
        {
            if (b % 2 == 0)
            {
                input = 0;
                output = 1;
            }
            else
            {
                input = 1;
                output = 0;
            }

            ListOfSectorLists[output].Clear();

            if (ListOfSectorLists[input].Count == 0)
            {
                break;
            }

            for (int c = 0; c < ListOfSectorLists[input].Count; c++)
            {
                SectorMeta sector = ListOfSectorLists[input][c];

                Sectors.Add(sector);

                contactingSectors[sector.sectorId] += 1;

                for (int d = sector.portalStartIndex; d < sector.portalStartIndex + sector.portalCount; d++)
                {
                    int connectedsector = portals[d].connectedSectorId;

                    SectorMeta portalsector = sectors[connectedsector];

                    if (SectorsContains(portalsector.sectorId, Sectors))
                    {
                        continue;
                    }

                    radius = CheckRadius(portalsector, CamPosition, planes);

                    if (radius)
                    {
                        ListOfSectorLists[output].Add(portalsector);
                    }
                }

                check = CheckSector(sector, CamPosition, planes);

                if (check)
                {
                    CurrentSector = sector;
                }
            }
        }

        return CurrentSector;
    }

    //“from Real-Time Collision Detection by Christer Ericson, published by Morgan Kaufmann Publishers, © 2005 Elsevier Inc”.

    public static float TestSphereTriangle(Vector3 s, Vector3 a, Vector3 b, Vector3 c, float radius, ref Vector3 p, ref Vector3 v)
    {
        // Find point P on triangle ABC closest to sphere center
        p = ClosestPtPointTriangle(s, a, b, c);

        // Sphere and triangle intersect if the (squared) distance from sphere
        // center to point p is less than the (squared) sphere radius
        v = s - p;
        return Vector3.Dot(v, v);
        //return Vector3.Dot(v, v) <= radius * radius;
    }

    public static Vector3 ClosestPtPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        // Check if P in vertex region outside A
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;
        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0.0f && d2 <= 0.0f) return a; // barycentric coordinates (1,0,0)

        // Check if P in vertex region outside B
        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0.0f && d4 <= d3) return b; // barycentric coordinates (0,1,0)

        // Check if P in edge region of AB, if so return projection of P onto AB
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
        {
            float v = d1 / (d1 - d3);
            return a + v * ab; // barycentric coordinates (1-v,v,0)
        }

        // Check if P in vertex region outside C
        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0.0f && d5 <= d6) return c; // barycentric coordinates (0,0,1)

        // Check if P in edge region of AC, if so return projection of P onto AC
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
        {
            float w = d2 / (d2 - d6);
            return a + w * ac; // barycentric coordinates (1-w,0,w)
        }

        // Check if P in edge region of BC, if so return projection of P onto BC
        float va = d3 * d6 - d5 * d4;
        if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + w * (c - b); // barycentric coordinates (0,1-w,w)
        }

        // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
        float denom = 1.0f / (va + vb + vc);
        float v2 = vb * denom;
        float w2 = vc * denom;
        return a + ab * v2 + ac * w2; // = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
    }

    public static SectorMeta PlayerStart(List<StartPosition> positions, List<SectorMeta> sectors, Random rng, ref Vector3 playerStartPosition)
    {
        int randomIndex = rng.Next(0, positions.Count);

        StartPosition selectedPosition = positions[randomIndex];

        playerStartPosition = new Vector3(selectedPosition.playerStart.Z, selectedPosition.playerStart.Y + 1.25f, -selectedPosition.playerStart.X);

        SectorMeta CurrentSector = sectors[selectedPosition.sectorId];

        Console.WriteLine("Player start position created!");

        return CurrentSector;
    }

    public static void BuildObjects(List<StartSector> starts, List<Sector> sectors, List<StartPosition> positions)
    {
        for (int i = 0; i < starts.Count; i++)
        {
            StartPosition start = new StartPosition
            {
                playerStart = new Vector3(starts[i].location.X / 2 * 2.5f, sectors[starts[i].sector].floorHeight / 8 * 2.5f, starts[i].location.Y / 2 * 2.5f),

                sectorId = starts[i].sector
            };

            positions.Add(start);
        }

        Console.WriteLine("Objects built successfully!");
    }

    public static void LoadFromFile(string read, List<Vector2> vertices, List<int> vertexIndices, List<int> wallTypes, List<Sector> sectors, List<StartSector> starts)
    {
        string text = File.ReadAllText(read);

        string[] lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("vertex"))
            {
                string[] parts = lines[i].Split('\t');

                if (parts.Length == 3)
                {
                    float y = float.Parse(parts[1]);

                    string[] xValues = parts[2].Split(' ');

                    for (int e = 0; e < xValues.Length; e++)
                    {
                        if (float.TryParse(xValues[e], out float x))
                        {
                            vertices.Add(new Vector2(x, y));
                        }
                    }
                }
            }

            if (lines[i].StartsWith("sector"))
            {
                Sector sector = new Sector();

                string[] parts = lines[i].Split('\t');

                if (parts.Length == 3)
                {
                    string[] heightParts = parts[1].Split(' ');

                    if (heightParts.Length == 2)
                    {
                        sector.floorHeight = float.Parse(heightParts[0]);
                        sector.ceilingHeight = float.Parse(heightParts[1]);
                    }

                    string[] values = parts[2].Split(' ');

                    int half = values.Length / 2;

                    sector.vertexStartIndex = vertexIndices.Count;
                    sector.wallTypesIndexStart = wallTypes.Count;

                    for (int e = 0; e < values.Length; e++)
                    {
                        if (int.TryParse(values[e], out int val))
                        {
                            if (e < half)
                            {
                                vertexIndices.Add(val);
                                sector.vertexCount += 1;
                            }
                            else
                            {
                                wallTypes.Add(val);
                                sector.wallTypesCount += 1;
                            }
                        }
                    }
                }

                sectors.Add(sector);
            }

            if (lines[i].StartsWith("player"))
            {
                StartSector start = new StartSector();

                string[] parts = lines[i].Split('\t');

                if (parts.Length == 4)
                {
                    string[] locationParts = parts[1].Split(' ');

                    if (locationParts.Length == 2)
                    {
                        float x = float.Parse(locationParts[0]);

                        float y = float.Parse(locationParts[1]);

                        start.location = new Vector2(x, y);
                    }

                    start.angle = float.Parse(parts[2]);

                    start.sector = int.Parse(parts[3]);
                }

                starts.Add(start);
            }
        }

        Console.WriteLine($"Loaded {vertices.Count} vertices.");
        Console.WriteLine($"Loaded {sectors.Count} sectors.");
        Console.WriteLine($"Loaded {starts.Count} player starts.");
    }

    public static float GetPlaneSignedDistanceToPoint(MathematicalPlane plane, Vector3 point)
    {
        return Vector3.Dot(plane.normal, point) + plane.distance;
    }

    public static Vector4 ConvertWorldToClip(Matrix4x4 viewProj, Vector3 vertex)
    {
        Vector4 v = new Vector4(vertex.X, vertex.Y, vertex.Z, 1.0f);
        return Vector4.Transform(v, viewProj);
    }

    public static Vector3 ConvertClipToNDC(Vector4 vertex)
    {
        float invw = 1.0f / vertex.W;

        return new Vector3(vertex.X * invw, vertex.Y * invw, vertex.Z * invw);
    }

    public static void BuildGeometry(List<Sector> sectors, List<int> vertexIndices, List<int> wallTypes, List<Vector2> vertices, double Ceiling, double Floor, List<Vertex> temporaryVertices, List<int> temporaryTriangles, MathematicalPlane LeftPlane, MathematicalPlane TopPlane, List<MathematicalPlane> planes, List<Vector3> edges, List<int> lines, List<PortalMeta> portals, List<Vector3> ceilingVertices, List<Vector3> ceilingTextures, List<int> ceilingTriangles, List<Vector3> floorVertices, List<Vector3> floorTextures, List<int> floorTriangles, List<MeshMeta> meshes, List<Vertex> Vertices, List<int> Indices, List<SectorMeta> Sectors)
    {
        int portalStart = 0;

        int planeStart = 0;

        int verticesStart = 0;

        int indicesStart = 0;

        int portalNumber = 0;

        for (int i = 0; i < sectors.Count; i++)
        {
            temporaryVertices.Clear();

            temporaryTriangles.Clear();

            int portalCount = 0;

            int planeCount = 0;

            Sector sector = sectors[i];

            for (int e = sector.vertexStartIndex; e < sector.vertexStartIndex + sector.vertexCount; e++)
            {
                int localIndex = e - sector.vertexStartIndex;

                int current = vertexIndices[e];

                int next = vertexIndices[sector.vertexStartIndex + ((localIndex + 1) % sector.vertexCount)];

                int wall = wallTypes[sector.wallTypesIndexStart + ((localIndex + 1) % sector.wallTypesCount)];

                double X1 = -vertices[current].X / 2 * 2.5f;
                double Z1 = vertices[current].Y / 2 * 2.5f;

                double X0 = -vertices[next].X / 2 * 2.5f;
                double Z0 = vertices[next].Y / 2 * 2.5f;

                if (wall == -1)
                {
                    double V0 = sector.floorHeight / 8 * 2.5f;
                    double V1 = sector.ceilingHeight / 8 * 2.5f;

                    int baseVert = temporaryVertices.Count;

                    Vector3 pos0 = new Vector3((float)Z0, (float)V0, (float)X0);
                    Vector3 pos1 = new Vector3((float)Z0, (float)V1, (float)X0);
                    Vector3 pos2 = new Vector3((float)Z1, (float)V1, (float)X1);
                    Vector3 pos3 = new Vector3((float)Z1, (float)V0, (float)X1);

                    Vector3 n = Vector3.Normalize(Vector3.Cross(pos1 - pos0, pos2 - pos0));

                    Vector3 leftPlaneNormal = Vector3.Normalize(pos2 - pos1);
                    float leftPlaneDistance = -Vector3.Dot(leftPlaneNormal, pos1);

                    Vector3 topPlaneNormal = Vector3.Normalize(pos1 - pos0);
                    float topPlaneDistance = -Vector3.Dot(topPlaneNormal, pos1);

                    LeftPlane = new MathematicalPlane { normal = leftPlaneNormal, distance = leftPlaneDistance };
                    TopPlane = new MathematicalPlane { normal = topPlaneNormal, distance = topPlaneDistance };

                    temporaryVertices.Add(new Vertex { v = pos0, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos0) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos0) / 2.5f, 0) });
                    temporaryVertices.Add(new Vertex { v = pos1, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos1) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos1) / 2.5f, 0) });
                    temporaryVertices.Add(new Vertex { v = pos2, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos2) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos2) / 2.5f, 0) });
                    temporaryVertices.Add(new Vertex { v = pos3, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos3) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos3) / 2.5f, 0) });

                    temporaryTriangles.Add(baseVert);
                    temporaryTriangles.Add(baseVert + 1);
                    temporaryTriangles.Add(baseVert + 2);
                    temporaryTriangles.Add(baseVert);
                    temporaryTriangles.Add(baseVert + 2);
                    temporaryTriangles.Add(baseVert + 3);

                    MathematicalPlane plane = new MathematicalPlane { normal = n, distance = -Vector3.Dot(n, pos0) };
                    planes.Add(plane);
                    planeCount += 1;
                }
                else
                {
                    if (sector.ceilingHeight > sectors[wall].ceilingHeight)
                    {
                        if (sector.floorHeight < sectors[wall].ceilingHeight)
                        {
                            double C0 = sector.ceilingHeight / 8 * 2.5f;
                            Ceiling = sector.ceilingHeight > sectors[wall].ceilingHeight ? sectors[wall].ceilingHeight / 8 * 2.5f : sector.ceilingHeight / 8 * 2.5f;

                            int baseVert = temporaryVertices.Count;

                            Vector3 pos0 = new Vector3((float)Z0, (float)Ceiling, (float)X0);
                            Vector3 pos1 = new Vector3((float)Z0, (float)C0, (float)X0);
                            Vector3 pos2 = new Vector3((float)Z1, (float)C0, (float)X1);
                            Vector3 pos3 = new Vector3((float)Z1, (float)Ceiling, (float)X1);

                            Vector3 n = Vector3.Normalize(Vector3.Cross(pos1 - pos0, pos2 - pos0));

                            Vector3 leftPlaneNormal = Vector3.Normalize(pos2 - pos1);
                            float leftPlaneDistance = -Vector3.Dot(leftPlaneNormal, pos1);

                            Vector3 topPlaneNormal = Vector3.Normalize(pos1 - pos0);
                            float topPlaneDistance = -Vector3.Dot(topPlaneNormal, pos1);

                            LeftPlane = new MathematicalPlane { normal = leftPlaneNormal, distance = leftPlaneDistance };
                            TopPlane = new MathematicalPlane { normal = topPlaneNormal, distance = topPlaneDistance };

                            temporaryVertices.Add(new Vertex { v = pos0, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos0) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos0) / 2.5f, 0) });
                            temporaryVertices.Add(new Vertex { v = pos1, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos1) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos1) / 2.5f, 0) });
                            temporaryVertices.Add(new Vertex { v = pos2, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos2) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos2) / 2.5f, 0) });
                            temporaryVertices.Add(new Vertex { v = pos3, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos3) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos3) / 2.5f, 0) });

                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 1);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert + 3);

                            MathematicalPlane plane = new MathematicalPlane { normal = n, distance = -Vector3.Dot(n, pos0) };
                            planes.Add(plane);
                            planeCount += 1;
                        }
                        else
                        {
                            double C0 = sector.ceilingHeight / 8 * 2.5f;
                            double C1 = sector.floorHeight / 8 * 2.5f;

                            int baseVert = temporaryVertices.Count;

                            Vector3 pos0 = new Vector3((float)Z0, (float)C1, (float)X0);
                            Vector3 pos1 = new Vector3((float)Z0, (float)C0, (float)X0);
                            Vector3 pos2 = new Vector3((float)Z1, (float)C0, (float)X1);
                            Vector3 pos3 = new Vector3((float)Z1, (float)C1, (float)X1);

                            Vector3 n = Vector3.Normalize(Vector3.Cross(pos1 - pos0, pos2 - pos0));

                            Vector3 leftPlaneNormal = Vector3.Normalize(pos2 - pos1);
                            float leftPlaneDistance = -Vector3.Dot(leftPlaneNormal, pos1);

                            Vector3 topPlaneNormal = Vector3.Normalize(pos1 - pos0);
                            float topPlaneDistance = -Vector3.Dot(topPlaneNormal, pos1);

                            LeftPlane = new MathematicalPlane { normal = leftPlaneNormal, distance = leftPlaneDistance };
                            TopPlane = new MathematicalPlane { normal = topPlaneNormal, distance = topPlaneDistance };

                            temporaryVertices.Add(new Vertex { v = pos0, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos0) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos0) / 2.5f, 0) });
                            temporaryVertices.Add(new Vertex { v = pos1, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos1) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos1) / 2.5f, 0) });
                            temporaryVertices.Add(new Vertex { v = pos2, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos2) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos2) / 2.5f, 0) });
                            temporaryVertices.Add(new Vertex { v = pos3, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos3) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos3) / 2.5f, 0) });

                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 1);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert + 3);

                            MathematicalPlane plane = new MathematicalPlane { normal = n, distance = -Vector3.Dot(n, pos0) };

                            planes.Add(plane);

                            planeCount += 1;
                        }
                    }
                    if (sectors[wall].ceilingHeight != sectors[wall].floorHeight)
                    {
                        if (sector.ceilingHeight > sectors[wall].ceilingHeight)
                        {
                            Ceiling = sectors[wall].ceilingHeight / 8 * 2.5f;
                        }
                        else
                        {
                            Ceiling = sector.ceilingHeight / 8 * 2.5f;
                        }
                        if (sector.floorHeight > sectors[wall].floorHeight)
                        {
                            Floor = sector.floorHeight / 8 * 2.5f;
                        }
                        else
                        {
                            Floor = sectors[wall].floorHeight / 8 * 2.5f;
                        }

                        int baseVert = edges.Count;

                        int baseStartIndex = lines.Count;

                        edges.Add(new Vector3((float)Z0, (float)Floor, (float)X0));
                        edges.Add(new Vector3((float)Z0, (float)Ceiling, (float)X0));
                        edges.Add(new Vector3((float)Z1, (float)Ceiling, (float)X1));
                        edges.Add(new Vector3((float)Z1, (float)Floor, (float)X1));

                        lines.Add(baseVert);
                        lines.Add(baseVert + 1);
                        lines.Add(baseVert + 1);
                        lines.Add(baseVert + 2);
                        lines.Add(baseVert + 2);
                        lines.Add(baseVert + 3);
                        lines.Add(baseVert + 3);
                        lines.Add(baseVert);

                        Vector3 v0 = edges[baseVert];
                        Vector3 v1 = edges[baseVert + 1];
                        Vector3 v2 = edges[baseVert + 2];

                        Vector3 n = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

                        PortalMeta transformedportal = new PortalMeta
                        {
                            plane = planes.Count,

                            sectorId = i,

                            connectedSectorId = wall,

                            edgeStartIndex = baseStartIndex,

                            edgeCount = 8,

                            portalId = portalNumber
                        };

                        portals.Add(transformedportal);

                        MathematicalPlane plane = new MathematicalPlane
                        {
                            normal = n,
                            distance = -Vector3.Dot(n, v0)
                        };

                        planes.Add(plane);

                        portalCount += 1;

                        planeCount += 1;

                        portalNumber += 1;
                    }

                    if (sector.floorHeight < sectors[wall].floorHeight)
                    {
                        if (sector.ceilingHeight > sectors[wall].floorHeight)
                        {
                            double F0 = sector.floorHeight / 8 * 2.5f;
                            Floor = sector.floorHeight > sectors[wall].floorHeight ? sector.floorHeight / 8 * 2.5f : sectors[wall].floorHeight / 8 * 2.5f;
                            int baseVert = temporaryVertices.Count;
                            Vector3 pos0 = new Vector3((float)Z0, (float)F0, (float)X0);
                            Vector3 pos1 = new Vector3((float)Z0, (float)Floor, (float)X0);
                            Vector3 pos2 = new Vector3((float)Z1, (float)Floor, (float)X1);
                            Vector3 pos3 = new Vector3((float)Z1, (float)F0, (float)X1);
                            Vector3 n = Vector3.Normalize(Vector3.Cross(pos1 - pos0, pos2 - pos0));
                            Vector3 leftPlaneNormal = Vector3.Normalize(pos2 - pos1);
                            float leftPlaneDistance = -Vector3.Dot(leftPlaneNormal, pos1);
                            Vector3 topPlaneNormal = Vector3.Normalize(pos1 - pos0);
                            float topPlaneDistance = -Vector3.Dot(topPlaneNormal, pos1);
                            LeftPlane = new MathematicalPlane { normal = leftPlaneNormal, distance = leftPlaneDistance };
                            TopPlane = new MathematicalPlane { normal = topPlaneNormal, distance = topPlaneDistance };
                            temporaryVertices.Add(new Vertex { v = pos0, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos0) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos0) / 2.5f, 3) });
                            temporaryVertices.Add(new Vertex { v = pos1, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos1) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos1) / 2.5f, 3) });
                            temporaryVertices.Add(new Vertex { v = pos2, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos2) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos2) / 2.5f, 3) });
                            temporaryVertices.Add(new Vertex { v = pos3, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos3) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos3) / 2.5f, 3) });
                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 1);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert + 3);
                            MathematicalPlane plane = new MathematicalPlane { normal = n, distance = -Vector3.Dot(n, pos0) };
                            planes.Add(plane);
                            planeCount += 1;
                        }
                        else
                        {
                            double F0 = sector.floorHeight / 8 * 2.5f;
                            double F1 = sector.ceilingHeight / 8 * 2.5f;
                            int baseVert = temporaryVertices.Count;
                            Vector3 pos0 = new Vector3((float)Z0, (float)F0, (float)X0);
                            Vector3 pos1 = new Vector3((float)Z0, (float)F1, (float)X0);
                            Vector3 pos2 = new Vector3((float)Z1, (float)F1, (float)X1);
                            Vector3 pos3 = new Vector3((float)Z1, (float)F0, (float)X1);
                            Vector3 n = Vector3.Normalize(Vector3.Cross(pos1 - pos0, pos2 - pos0));
                            Vector3 leftPlaneNormal = Vector3.Normalize(pos2 - pos1);
                            float leftPlaneDistance = -Vector3.Dot(leftPlaneNormal, pos1);
                            Vector3 topPlaneNormal = Vector3.Normalize(pos1 - pos0);
                            float topPlaneDistance = -Vector3.Dot(topPlaneNormal, pos1);
                            LeftPlane = new MathematicalPlane { normal = leftPlaneNormal, distance = leftPlaneDistance };
                            TopPlane = new MathematicalPlane { normal = topPlaneNormal, distance = topPlaneDistance };
                            temporaryVertices.Add(new Vertex { v = pos0, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos0) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos0) / 2.5f, 3) });
                            temporaryVertices.Add(new Vertex { v = pos1, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos1) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos1) / 2.5f, 3) });
                            temporaryVertices.Add(new Vertex { v = pos2, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos2) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos2) / 2.5f, 3) });
                            temporaryVertices.Add(new Vertex { v = pos3, n = n, t = new Vector3(GetPlaneSignedDistanceToPoint(LeftPlane, pos3) / 2.5f, GetPlaneSignedDistanceToPoint(TopPlane, pos3) / 2.5f, 3) });
                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 1);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert);
                            temporaryTriangles.Add(baseVert + 2);
                            temporaryTriangles.Add(baseVert + 3);
                            MathematicalPlane plane = new MathematicalPlane { normal = n, distance = -Vector3.Dot(n, pos0) };
                            planes.Add(plane);
                            planeCount += 1;
                        }
                    }
                }
            }

            if (sector.floorHeight != sector.ceilingHeight)
            {
                floorVertices.Clear();
                ceilingVertices.Clear();
                floorTextures.Clear();
                ceilingTextures.Clear();

                float tinyNumber = 1e-6f;

                for (int e = sector.vertexStartIndex; e < sector.vertexStartIndex + sector.vertexCount; ++e)
                {
                    double YF = sector.floorHeight / 8 * 2.5f;
                    double YC = sector.ceilingHeight / 8 * 2.5f;
                    double X = -vertices[vertexIndices[e]].X / 2 * 2.5f;
                    double Z = vertices[vertexIndices[e]].Y / 2 * 2.5f;

                    float OX = -(float)X / 2.5f;
                    float OY = (float)Z / 2.5f;

                    floorVertices.Add(new Vector3((float)Z, (float)YF, (float)X));
                    ceilingVertices.Add(new Vector3((float)Z, (float)YC, (float)X));
                    floorTextures.Add(new Vector3(OY, OX, 1));
                    ceilingTextures.Add(new Vector3(OY, OX, 2));
                }

                floorVertices.Reverse();
                floorTextures.Reverse();

                floorTriangles.Clear();

                for (int e = 0; e < floorVertices.Count - 2; e++)
                {
                    Vector3 v0 = floorVertices[0];
                    Vector3 v1 = floorVertices[e + 1];
                    Vector3 v2 = floorVertices[e + 2];

                    Vector3 e0 = v1 - v0;
                    Vector3 e1 = v2 - v1;
                    Vector3 e2 = v2 - v0;

                    if (e0.LengthSquared() < tinyNumber || e1.LengthSquared() < tinyNumber || e2.LengthSquared() < tinyNumber)
                    {
                        continue;
                    }

                    Vector3 ef = Vector3.Cross(e0, e2);

                    if (ef.LengthSquared() < tinyNumber)
                    {
                        continue;
                    }

                    floorTriangles.Add(0);
                    floorTriangles.Add(e + 1);
                    floorTriangles.Add(e + 2);
                }

                ceilingTriangles.Clear();

                for (int e = 0; e < ceilingVertices.Count - 2; e++)
                {
                    Vector3 v0 = ceilingVertices[0];
                    Vector3 v1 = ceilingVertices[e + 1];
                    Vector3 v2 = ceilingVertices[e + 2];

                    Vector3 e0 = v1 - v0;
                    Vector3 e1 = v2 - v1;
                    Vector3 e2 = v2 - v0;

                    if (e0.LengthSquared() < tinyNumber || e1.LengthSquared() < tinyNumber || e2.LengthSquared() < tinyNumber)
                    {
                        continue;
                    }

                    Vector3 ec = Vector3.Cross(e0, e2);

                    if (ec.LengthSquared() < tinyNumber)
                    {
                        continue;
                    }

                    ceilingTriangles.Add(0);
                    ceilingTriangles.Add(e + 1);
                    ceilingTriangles.Add(e + 2);
                }
                int baseFloor = temporaryVertices.Count;
                Vector3 f0 = floorVertices[floorTriangles[0]];
                Vector3 f1 = floorVertices[floorTriangles[1]];
                Vector3 f2 = floorVertices[floorTriangles[2]];
                Vector3 f = Vector3.Normalize(Vector3.Cross(f1 - f0, f2 - f0));
                for (int e = 0; e < floorVertices.Count; e++)
                {
                    temporaryVertices.Add(new Vertex { v = floorVertices[e], t = floorTextures[e], n = f });
                }
                for (int e = 0; e < floorTriangles.Count; e++)
                {
                    temporaryTriangles.Add(baseFloor + floorTriangles[e]);
                }
                MathematicalPlane floorPlane = new MathematicalPlane { normal = f, distance = -Vector3.Dot(f, f0) };
                planes.Add(floorPlane);
                planeCount += 1;
                int baseCeiling = temporaryVertices.Count;
                Vector3 ceil0 = ceilingVertices[ceilingTriangles[0]];
                Vector3 ceil1 = ceilingVertices[ceilingTriangles[1]];
                Vector3 ceil2 = ceilingVertices[ceilingTriangles[2]];
                Vector3 c = Vector3.Normalize(Vector3.Cross(ceil1 - ceil0, ceil2 - ceil0));
                for (int e = 0; e < ceilingVertices.Count; e++)
                {
                    temporaryVertices.Add(new Vertex { v = ceilingVertices[e], t = ceilingTextures[e], n = c });
                }
                for (int e = 0; e < ceilingTriangles.Count; e++)
                {
                    temporaryTriangles.Add(baseCeiling + ceilingTriangles[e]);
                }
                MathematicalPlane ceilingPlane = new MathematicalPlane { normal = c, distance = -Vector3.Dot(c, ceil0) };
                planes.Add(ceilingPlane);
                planeCount += 1;
            }

            SectorMeta sectorMeta = new SectorMeta
            {
                sectorId = i,
                rectangle = 0,
                portalStartIndex = portalStart,
                portalCount = portalCount,
                planeStartIndex = planeStart,
                planeCount = planeCount
            };

            Sectors.Add(sectorMeta);

            Vertices.AddRange(temporaryVertices);

            for (int j = 0; j < temporaryTriangles.Count; j++)
            {
                int globalIndex = temporaryTriangles[j] + verticesStart;
                Indices.Add(globalIndex);
            }

            MeshMeta mesh = new MeshMeta
            {
                verticesStartIndex = verticesStart,
                verticesCount = temporaryVertices.Count,
                indicesStartIndex = indicesStart,
                indicesCount = temporaryTriangles.Count,
                triangles = temporaryTriangles.Count / 3,
                sectorId = i
            };

            meshes.Add(mesh);

            verticesStart += temporaryVertices.Count;

            indicesStart += temporaryTriangles.Count;

            portalStart += portalCount;

            planeStart += planeCount;
        }

        Console.WriteLine("Level built successfully!");
    }

    public static Vector4 MakeRectangleWithEdges(
     Vector4 rectangle,
     PortalMeta portal,
     List<Vector3> OutEdgeVertices,
     Matrix4x4 CamViewProj,
     List<Vector3> edges,
     List<int> lines,
     EdgeVertex[] processBuffer,
     EdgeVertex[] temporaryBuffer)
    {
        OutEdgeVertices.Clear();

        int processCount = 0;

        for (int a = portal.edgeStartIndex; a < portal.edgeStartIndex + portal.edgeCount; a += 2)
        {
            Vector4 v0clip = ConvertWorldToClip(CamViewProj, edges[lines[a]]);
            Vector4 v1clip = ConvertWorldToClip(CamViewProj, edges[lines[a + 1]]);

            processBuffer[processCount] = new EdgeVertex { c = v0clip, b = true };
            processBuffer[processCount + 1] = new EdgeVertex { c = v1clip, b = true };
            processCount += 2;
        }

        for (int b = 0; b < 6; b++)
        {
            int intersection = 0;
            int temporaryCount = 0;

            Vector4 intersectionPoint0 = Vector4.Zero;
            Vector4 intersectionPoint1 = Vector4.Zero;

            for (int c = 0; c < processCount; c += 2)
            {
                if (!processBuffer[c].b && !processBuffer[c + 1].b)
                {
                    continue;
                }

                Vector4 v0 = processBuffer[c].c;
                Vector4 v1 = processBuffer[c + 1].c;

                float minX = rectangle.X;
                float minY = rectangle.Y;
                float maxX = rectangle.Z;
                float maxY = rectangle.W;

                float d0, d1;
                switch (b)
                {
                    case 0: d0 = v0.X - minX * v0.W; d1 = v1.X - minX * v1.W; break; // Left
                    case 1: d0 = maxX * v0.W - v0.X; d1 = maxX * v1.W - v1.X; break; // Right
                    case 2: d0 = v0.Y - minY * v0.W; d1 = v1.Y - minY * v1.W; break; // Bottom
                    case 3: d0 = maxY * v0.W - v0.Y; d1 = maxY * v1.W - v1.Y; break; // Top
                    case 4: d0 = v0.Z; d1 = v1.Z; break; // Near
                    case 5: d0 = v0.W - v0.Z; d1 = v1.W - v1.Z; break; // Far
                    default: d0 = 0; d1 = 0; break;
                }

                bool b0 = d0 >= 0;
                bool b1 = d1 >= 0;

                if (b0 && b1)
                {
                    continue;
                }
                else if (b0 != b1)
                {
                    Vector4 point0, point1;
                    float t = d0 / (d0 - d1);
                    Vector4 intersectionPoint = Vector4.Lerp(v0, v1, t);

                    if (b0)
                    {
                        point0 = v0;
                        point1 = intersectionPoint;
                        intersectionPoint0 = intersectionPoint;
                    }
                    else
                    {
                        point0 = intersectionPoint;
                        point1 = v1;
                        intersectionPoint1 = intersectionPoint;
                    }

                    temporaryBuffer[temporaryCount] = new EdgeVertex { c = point0, b = true };
                    temporaryBuffer[temporaryCount + 1] = new EdgeVertex { c = point1, b = true };
                    temporaryCount += 2;

                    processBuffer[c].b = false;
                    processBuffer[c + 1].b = false;

                    intersection += 1;
                }
                else
                {
                    processBuffer[c].b = false;
                    processBuffer[c + 1].b = false;
                }
            }

            if (intersection == 2)
            {
                for (int d = 0; d < temporaryCount; d += 2)
                {
                    processBuffer[processCount] = temporaryBuffer[d];
                    processBuffer[processCount + 1] = temporaryBuffer[d + 1];
                    processCount += 2;
                }

                processBuffer[processCount] = new EdgeVertex { c = intersectionPoint0, b = true };
                processBuffer[processCount + 1] = new EdgeVertex { c = intersectionPoint1, b = true };
                processCount += 2;
            }
        }

        for (int e = 0; e < processCount; e += 2)
        {
            if (processBuffer[e].b && processBuffer[e + 1].b)
            {
                OutEdgeVertices.Add(ConvertClipToNDC(processBuffer[e].c));
                OutEdgeVertices.Add(ConvertClipToNDC(processBuffer[e + 1].c));
            }
        }

        if (OutEdgeVertices.Count < 6 || OutEdgeVertices.Count % 2 == 1)
        {
            return Vector4.Zero;
        }

        float xmin = float.PositiveInfinity, ymin = float.PositiveInfinity;
        float xmax = float.NegativeInfinity, ymax = float.NegativeInfinity;

        for (int i = 0; i < OutEdgeVertices.Count; i++)
        {
            Vector3 ndc = OutEdgeVertices[i];
            xmin = MathF.Min(xmin, ndc.X);
            xmax = MathF.Max(xmax, ndc.X);
            ymin = MathF.Min(ymin, ndc.Y);
            ymax = MathF.Max(ymax, ndc.Y);
        }

        return new Vector4(xmin, ymin, xmax, ymax);
    }

    public static void DrawTiles(int tilesX, int tilesY, int[] triangleCount, TileTriangle[] ArrayOfTileTriangles, Triangle[] ArrayOfTriangles, int Width, int Height, int texWidth, int texHeight, int maskX, int maskY, uint[][] sampleTexture, uint[] backBuffer, float[] depthBuffer)
    {

        int totalTiles = tilesX * tilesY;
        for (int t = 0; t < totalTiles; t++)
        {
            if (triangleCount[t] == 0)
            {
                continue;
            }

            int startOffset = t * 128;

            for (int bt = 0; bt < triangleCount[t]; bt++)
            {
                var tt = ArrayOfTileTriangles[startOffset + bt];

                int triId = tt.triangleIndex;
                Triangle tri = ArrayOfTriangles[triId];

                int startY = tt.miny;
                int endY = tt.maxy;
                int startX = tt.minx;
                int endX = tt.maxx;

                if (startX >= endX || startY >= endY)
                {
                    continue;
                }

                float startPx = startX + 0.5f;
                float startPy = startY + 0.5f;

                float edgeRow0 = tri.edgeA0 * startPx + tri.edgeB0 * startPy + tri.edgeC0;
                float edgeRow1 = tri.edgeA1 * startPx + tri.edgeB1 * startPy + tri.edgeC1;
                float edgeRow2 = tri.edgeA2 * startPx + tri.edgeB2 * startPy + tri.edgeC2;

                float startW0 = edgeRow0 * tri.invArea;
                float startW1 = edgeRow1 * tri.invArea;
                float startW2 = edgeRow2 * tri.invArea;

                float rowW = startW0 * tri.invw0 + startW1 * tri.invw1 + startW2 * tri.invw2;
                float rowZ = startW0 * tri.invz0 + startW1 * tri.invz1 + startW2 * tri.invz2;
                float rowU = startW0 * tri.invuv0.X + startW1 * tri.invuv1.X + startW2 * tri.invuv2.X;
                float rowV = startW0 * tri.invuv0.Y + startW1 * tri.invuv1.Y + startW2 * tri.invuv2.Y;

                float diffuse = tri.diffuse;

                for (int y = startY; y < endY; y++)
                {
                    int p = y * Width + startX;

                    float edge0 = edgeRow0;
                    float edge1 = edgeRow1;
                    float edge2 = edgeRow2;

                    float incW = rowW;
                    float incZ = rowZ;
                    float incU = rowU;
                    float incV = rowV;

                    for (int x = startX; x < endX; x++)
                    {
                        if (edge0 <= 0f && edge1 <= 0f && edge2 <= 0f)
                        {
                            if (incW != 0f)
                            {
                                float invWeight = 1.0f / incW;
                                float z = incZ * invWeight;

                                if (z < depthBuffer[p])
                                {
                                    depthBuffer[p] = z;

                                    float texX = incU * invWeight;
                                    float texY = incV * invWeight;

                                    int tx = ((int)texX - (texX < 0 ? 1 : 0)) & maskX;
                                    int ty = ((int)texY - (texY < 0 ? 1 : 0)) & maskY;

                                    uint col = sampleTexture[tri.texture][ty * texWidth + tx];

                                    byte r = (byte)(((col >> 0) & 255) * diffuse);
                                    byte g = (byte)(((col >> 8) & 255) * diffuse);
                                    byte b = (byte)(((col >> 16) & 255) * diffuse);
                                    byte a = (byte)((col >> 24) & 255);

                                    backBuffer[p] = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                                }
                            }
                        }

                        edge0 += tri.edgeA0;
                        edge1 += tri.edgeA1;
                        edge2 += tri.edgeA2;

                        incW += tri.invwx;
                        incZ += tri.invzx;
                        incU += tri.invux;
                        incV += tri.invvx;
                        p += 1;
                    }

                    edgeRow0 += tri.edgeB0;
                    edgeRow1 += tri.edgeB1;
                    edgeRow2 += tri.edgeB2;

                    rowW += tri.invwy;
                    rowZ += tri.invzy;
                    rowU += tri.invuy;
                    rowV += tri.invvy;
                }
            }
        }
    }

    public static void DrawTilesParallel(int tilesX, int tilesY, int[] triangleCount, TileTriangle[] ArrayOfTileTriangles, Triangle[] ArrayOfTriangles, int Width, int Height, int texWidth, int texHeight, int maskX, int maskY, uint[][] sampleTexture, uint[] backBuffer, float[] depthBuffer)
    {
        int totalTiles = tilesX * tilesY;
        Parallel.For(0, totalTiles, t =>
        {
            if (triangleCount[t] == 0)
            {
                return;
            }

            int startOffset = t * 128;

            for (int bt = 0; bt < triangleCount[t]; bt++)
            {
                var tt = ArrayOfTileTriangles[startOffset + bt];

                int triId = tt.triangleIndex;
                Triangle tri = ArrayOfTriangles[triId];

                int startY = tt.miny;
                int endY = tt.maxy;
                int startX = tt.minx;
                int endX = tt.maxx;

                if (startX >= endX || startY >= endY)
                {
                    continue;
                }

                float startPx = startX + 0.5f;
                float startPy = startY + 0.5f;

                float edgeRow0 = tri.edgeA0 * startPx + tri.edgeB0 * startPy + tri.edgeC0;
                float edgeRow1 = tri.edgeA1 * startPx + tri.edgeB1 * startPy + tri.edgeC1;
                float edgeRow2 = tri.edgeA2 * startPx + tri.edgeB2 * startPy + tri.edgeC2;

                float startW0 = edgeRow0 * tri.invArea;
                float startW1 = edgeRow1 * tri.invArea;
                float startW2 = edgeRow2 * tri.invArea;

                float rowW = startW0 * tri.invw0 + startW1 * tri.invw1 + startW2 * tri.invw2;
                float rowZ = startW0 * tri.invz0 + startW1 * tri.invz1 + startW2 * tri.invz2;
                float rowU = startW0 * tri.invuv0.X + startW1 * tri.invuv1.X + startW2 * tri.invuv2.X;
                float rowV = startW0 * tri.invuv0.Y + startW1 * tri.invuv1.Y + startW2 * tri.invuv2.Y;

                float diffuse = tri.diffuse;

                for (int y = startY; y < endY; y++)
                {
                    int p = y * Width + startX;

                    float edge0 = edgeRow0;
                    float edge1 = edgeRow1;
                    float edge2 = edgeRow2;

                    float incW = rowW;
                    float incZ = rowZ;
                    float incU = rowU;
                    float incV = rowV;

                    for (int x = startX; x < endX; x++)
                    {
                        if (edge0 <= 0f && edge1 <= 0f && edge2 <= 0f)
                        {
                            if (incW != 0f)
                            {
                                float invWeight = 1.0f / incW;
                                float z = incZ * invWeight;

                                if (z < depthBuffer[p])
                                {
                                    depthBuffer[p] = z;

                                    float texX = incU * invWeight;
                                    float texY = incV * invWeight;

                                    int tx = ((int)texX - (texX < 0 ? 1 : 0)) & maskX;
                                    int ty = ((int)texY - (texY < 0 ? 1 : 0)) & maskY;

                                    uint col = sampleTexture[tri.texture][ty * texWidth + tx];

                                    byte r = (byte)(((col >> 0) & 255) * diffuse);
                                    byte g = (byte)(((col >> 8) & 255) * diffuse);
                                    byte b = (byte)(((col >> 16) & 255) * diffuse);
                                    byte a = (byte)((col >> 24) & 255);

                                    backBuffer[p] = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                                }
                            }
                        }

                        edge0 += tri.edgeA0;
                        edge1 += tri.edgeA1;
                        edge2 += tri.edgeA2;

                        incW += tri.invwx;
                        incZ += tri.invzx;
                        incU += tri.invux;
                        incV += tri.invvx;
                        p += 1;
                    }

                    edgeRow0 += tri.edgeB0;
                    edgeRow1 += tri.edgeB1;
                    edgeRow2 += tri.edgeB2;

                    rowW += tri.invwy;
                    rowZ += tri.invzy;
                    rowU += tri.invuy;
                    rowV += tri.invvy;
                }
            }
        });
    }

    public static void DrawTilesOther(int tilesX, int tilesY, int[] triangleCount, TileTriangle[] ArrayOfTileTriangles, Triangle[] ArrayOfTriangles, int Width, int Height, int texWidth, int texHeight, int maskX, int maskY, uint[][] sampleTexture, uint[] backBuffer, float[] depthBuffer)
    {

        int totalTiles = tilesX * tilesY;
        for (int t = 0; t < totalTiles; t++)
        {
            if (triangleCount[t] == 0)
            {
                continue;
            }

            int startOffset = t * 128;

            for (int bt = 0; bt < triangleCount[t]; bt++)
            {
                var tt = ArrayOfTileTriangles[startOffset + bt];

                int triId = tt.triangleIndex;
                Triangle tri = ArrayOfTriangles[triId];

                int startY = tt.miny;
                int endY = tt.maxy;
                int startX = tt.minx;
                int endX = tt.maxx;

                if (startX >= endX || startY >= endY)
                {
                    continue;
                }

                float diffuse = tri.diffuse;

                for (int y = startY; y < endY; y++)
                {
                    int rowOffset = y * Width;
                    float py = y + 0.5f;

                    for (int x = startX; x < endX; x++)
                    {
                        float px = x + 0.5f;

                        float edge0 = tri.edgeA0 * px + tri.edgeB0 * py + tri.edgeC0;
                        float edge1 = tri.edgeA1 * px + tri.edgeB1 * py + tri.edgeC1;
                        float edge2 = tri.edgeA2 * px + tri.edgeB2 * py + tri.edgeC2;

                        if (edge0 <= 0f && edge1 <= 0f && edge2 <= 0f)
                        {
                            float weight0 = edge0 * tri.invArea;
                            float weight1 = edge1 * tri.invArea;
                            float weight2 = edge2 * tri.invArea;

                            float combinedWeight = weight0 * tri.invw0 + weight1 * tri.invw1 + weight2 * tri.invw2;

                            if (combinedWeight != 0f)
                            {
                                float invWeight = 1.0f / combinedWeight;
                                float z = (weight0 * tri.invz0 + weight1 * tri.invz1 + weight2 * tri.invz2) * invWeight;

                                int p = rowOffset + x;

                                if (z < depthBuffer[p])
                                {
                                    depthBuffer[p] = z;

                                    float texX = (weight0 * tri.invuv0.X + weight1 * tri.invuv1.X + weight2 * tri.invuv2.X) * invWeight;
                                    float texY = (weight0 * tri.invuv0.Y + weight1 * tri.invuv1.Y + weight2 * tri.invuv2.Y) * invWeight;

                                    int tx = ((int)texX - (texX < 0 ? 1 : 0)) & maskX;
                                    int ty = ((int)texY - (texY < 0 ? 1 : 0)) & maskY;

                                    uint col = sampleTexture[tri.texture][ty * texWidth + tx];

                                    byte r = (byte)(((col >> 0) & 255) * diffuse);
                                    byte g = (byte)(((col >> 8) & 255) * diffuse);
                                    byte b = (byte)(((col >> 16) & 255) * diffuse);
                                    byte a = (byte)((col >> 24) & 255);

                                    backBuffer[p] = (uint)(r | (g << 8) | (b << 16) | (a << 24));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
