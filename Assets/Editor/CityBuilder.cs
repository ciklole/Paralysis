using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Populates the authored City scene with houses from the vrbn USA set.
///
/// The street tiles come from ModularLowpolyStreetsFree. Each tile carries a
/// mesh with the pavement, the border and the black driving surface. The black
/// surface is measured from the actual triangles because the imported submesh
/// bounds are unreliable, and some junction tiles reference road materials that
/// are missing from the project.
///
/// Houses are placed against the outer edge of the pavement, with their front
/// facing the street, so they line the sidewalks without ever reaching the
/// black road the car drives on. Every house gets a mesh collider so the car
/// cannot drive through it.
/// </summary>
public static class CityBuilder
{
    const string CityPath = "Assets/Scenes/City.unity";
    const string DrivingPath = "Assets/Scenes/CarDriving.unity";
    const string LogDir = "Logs";
    const string ReportPath = LogDir + "/city_report.txt";

    const string PrefabRoot =
        "Assets/vrbn_studios/2023_A/USA/buildings/prefabs/";

    const string BlackRoadMaterial = "Road-1-line";
    const string PavementMaterial = "Pavement";
    const string RoadPackage = "ModularLowpolyStreetsFree";
    const string BuildingPackage = "vrbn_studios";

    static readonly string[] BuildingPrefabs =
    {
        "B00004", "B00009", "B00024", "B00083", "B00188", "B00274",
        "B00275", "B00276", "B00286", "B00287", "B00288", "B00904",
        "B00905", "B00906",
    };

    /// <summary>Houses wider than this are block fillers, not street walls.</summary>
    const float MaxHouseWidth = 52f;

    /// <summary>Clearance between two neighbouring houses.</summary>
    const float SlotGap = 2.5f;

    /// <summary>How far a house reaches onto the pavement.</summary>
    const float SidewalkTouch = 1.0f;

    /// <summary>Short runs are left alone.</summary>
    const float MinRunLength = 20f;

    /// <summary>Props bigger than this are ignored (fake light volumes).</summary>
    const float MaxObstacleSize = 150f;

    // ------------------------------------------------------------------
    // Collected data
    // ------------------------------------------------------------------

    class RoadTile
    {
        public string prefab;
        public Transform transform;
        public Bounds road;
        public Bounds walk;
        public float groundY;
        public bool hasRoad;
        public bool hasWalk;
        public bool straight;
    }

    class Run
    {
        public int axis;              // 0 = along X, 1 = along Z
        public float alongMin;
        public float alongMax;
        public float walkMin;
        public float walkMax;
        public float groundY;
        public List<RoadTile> tiles = new List<RoadTile>();
    }

    class House
    {
        public string name;
        public string prefabPath;
        public Vector3 localMin;
        public Vector3 localMax;
        public float width;
        public float depth;
    }

    class SubInfo
    {
        public Bounds bounds;
        public string material;
        public bool ok;
    }

    // ------------------------------------------------------------------
    // Entry point
    // ------------------------------------------------------------------

    [MenuItem("Paralysis/City/Build City")]
    public static void BuildCity()
    {
        Scene scene = EditorSceneManager.OpenScene(CityPath, OpenSceneMode.Single);

        // Remove houses from previous builds before measuring obstacles. The
        // buildings root is a child of City, so it has to be searched for
        // below the scene roots as well.
        GameObject cityRoot = CleanPreviousHouses(scene);

        List<RoadTile> roads = CollectRoads(scene);
        List<Run> runs = BuildRuns(roads);

        List<Bounds> roadRects = new List<Bounds>();
        List<Bounds> walkBands = new List<Bounds>();
        List<Bounds> junctionBlocks = new List<Bounds>();

        for (int i = 0; i < roads.Count; i++)
        {
            RoadTile tile = roads[i];

            if (tile.hasRoad)
            {
                roadRects.Add(tile.road);
            }

            if (!tile.hasWalk)
            {
                continue;
            }

            if (!tile.straight)
            {
                // A junction cannot be trusted to report its driving surface,
                // so the whole tile is kept clear.
                junctionBlocks.Add(tile.walk);
                continue;
            }

            if (!tile.hasRoad)
            {
                continue;
            }

            for (int side = 0; side < 2; side++)
            {
                Bounds band = new Bounds();

                if (tile.road.size.x > tile.road.size.z)
                {
                    band.SetMinMax(
                        new Vector3(tile.road.min.x, 0f,
                            side == 0 ? tile.walk.min.z : tile.road.max.z),
                        new Vector3(tile.road.max.x, 0f,
                            side == 0 ? tile.road.min.z : tile.walk.max.z));
                }
                else
                {
                    band.SetMinMax(
                        new Vector3(side == 0 ? tile.walk.min.x : tile.road.max.x, 0f,
                            tile.road.min.z),
                        new Vector3(side == 0 ? tile.road.min.x : tile.walk.max.x, 0f,
                            tile.road.max.z));
                }

                walkBands.Add(band);
            }
        }

        List<Bounds> obstacles = new List<Bounds>();
        List<string> obstacleNames = new List<string>();
        CollectObstacles(scene, obstacles, obstacleNames);

        // Houses already placed during this build. They are avoided like any
        // other obstacle so perpendicular streets and narrow blocks do not
        // produce buildings that cut through each other.
        List<Bounds> placedHouses = new List<Bounds>();

        List<House> houses = new List<House>();
        for (int i = 0; i < BuildingPrefabs.Length; i++)
        {
            House house = LoadHouse(BuildingPrefabs[i]);
            if (house != null)
            {
                houses.Add(house);
            }
        }

        StringBuilder report = new StringBuilder();
        report.AppendLine("=== STREETS ===");
        for (int i = 0; i < runs.Count; i++)
        {
            Run run = runs[i];
            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "run {0,2} axis={1} y={6:F2} along=({2:F2},{3:F2}) pavement=({4:F2},{5:F2}) tiles={7}",
                i, run.axis, run.alongMin, run.alongMax,
                run.walkMin, run.walkMax, run.groundY, run.tiles.Count));
        }

        report.AppendLine();
        report.AppendLine("=== ROAD RECTS ===");
        for (int i = 0; i < roadRects.Count; i++)
        {
            Bounds b = roadRects[i];
            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "road ({0:F2},{1:F2})-({2:F2},{3:F2})",
                b.min.x, b.min.z, b.max.x, b.max.z));
        }

        report.AppendLine();
        report.AppendLine("=== OBSTACLES ===");
        for (int i = 0; i < obstacles.Count; i++)
        {
            Bounds b = obstacles[i];
            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-26} ({1:F1},{2:F1})-({3:F1},{4:F1})",
                obstacleNames[i], b.min.x, b.min.z, b.max.x, b.max.z));
        }

        report.AppendLine();
        report.AppendLine("=== HOUSES ===");
        for (int i = 0; i < houses.Count; i++)
        {
            House h = houses[i];
            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-8} {1:F2} x {2:F2}",
                h.name, h.width, h.depth));
        }

        if (cityRoot == null)
        {
            cityRoot = new GameObject("City");
        }

        GameObject housesRoot = new GameObject("CityBuildings");
        housesRoot.transform.SetParent(cityRoot.transform, false);

        report.AppendLine();
        report.AppendLine("=== PLACEMENTS ===");

        int placed = 0;
        int houseIndex = 0;

        for (int r = 0; r < runs.Count; r++)
        {
            Run run = runs[r];

            if (run.alongMax - run.alongMin < MinRunLength)
            {
                continue;
            }

            for (int side = 0; side < 2; side++)
            {
                bool lowSide = side == 0;
                float edge = lowSide ? run.walkMin : run.walkMax;
                Vector3 outward = lowSide ? LowNormal(run.axis) : HighNormal(run.axis);
                float cursor = run.alongMin + 1f;
                int tries = 0;

                while (cursor < run.alongMax - 1f)
                {
                    float remaining = run.alongMax - 1f - cursor;
                    House house = PickHouse(houses, ref houseIndex, remaining);

                    if (house == null)
                    {
                        break;
                    }

                    float yaw = YawFacing(run.axis, lowSide);
                    Bounds footprint = FootprintWorld(house, yaw);

                    float alongMin = run.axis == 0 ? footprint.min.x : footprint.min.z;
                    float alongMax = run.axis == 0 ? footprint.max.x : footprint.max.z;
                    float extent = alongMax - alongMin;

                    // Do not let a house run past the end of the street.
                    if (extent > remaining + 0.01f)
                    {
                        break;
                    }

                    // The front face sits just inside the outer edge of the
                    // pavement, so the house touches the sidewalk.
                    Vector3 rootPos = run.axis == 0
                        ? new Vector3(cursor - alongMin, run.groundY, edge)
                        : new Vector3(edge, run.groundY, cursor - alongMin);

                    rootPos += outward * (house.localMax.z - SidewalkTouch);

                    Bounds placed3D = OffsetBounds(footprint, rootPos);

                    if (!IsClear(placed3D, roadRects, walkBands, junctionBlocks,
                            obstacles, placedHouses))
                    {
                        tries++;

                        if (tries >= houses.Count)
                        {
                            cursor += 3f;
                            tries = 0;
                        }

                        continue;
                    }

                    GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(
                        AssetDatabase.LoadAssetAtPath<GameObject>(house.prefabPath));
                    go.name = house.name;
                    go.transform.SetParent(housesRoot.transform, true);
                    go.transform.position = rootPos;
                    go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                    placed++;
                    tries = 0;
                    cursor += extent + SlotGap;
                    placedHouses.Add(placed3D);

                    report.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "run {0,2} side {1} {2,-8} pos=({3:F2},{4:F2}) yaw={5:F0} foot=({6:F2},{7:F2})-({8:F2},{9:F2})",
                        r, side, house.name, rootPos.x, rootPos.z, yaw,
                        placed3D.min.x, placed3D.min.z,
                        placed3D.max.x, placed3D.max.z));
                }
            }
        }

        // Colliders first: the prefab assets are shared by every scene.
        EnsurePrefabColliders();
        EnsureSceneColliders(scene);

        report.AppendLine();
        report.AppendLine("PLACED: " + placed);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);

        Directory.CreateDirectory(LogDir);
        File.WriteAllText(ReportPath, report.ToString());

        Debug.Log("CityBuilder: placed " + placed + " houses (saved=" + saved + ")");

        // The driving scene has to look exactly like the city.
        bool synced = DrivingSceneSyncTool.Sync(true, CityPath, DrivingPath, false, true);
        Debug.Log("CityBuilder: driving scene synced=" + synced);
    }

    /// <summary>
    /// Destroys the houses left by an earlier build and returns the City root.
    /// The buildings live under City, so a plain top-level name search is not
    /// enough.
    /// </summary>
    static GameObject CleanPreviousHouses(Scene scene)
    {
        GameObject cityRoot = null;
        GameObject[] roots = scene.GetRootGameObjects();
        List<GameObject> stale = new List<GameObject>();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "City")
            {
                cityRoot = roots[i];
            }

            Transform[] all = roots[i].transform.GetComponentsInChildren<Transform>(true);

            for (int c = 0; c < all.Length; c++)
            {
                if (all[c].name == "CityBuildings")
                {
                    stale.Add(all[c].gameObject);
                }
            }
        }

        for (int i = 0; i < stale.Count; i++)
        {
            if (stale[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(stale[i]);
            }
        }

        return cityRoot;
    }

    // ------------------------------------------------------------------
    // Road detection
    // ------------------------------------------------------------------

    static List<RoadTile> CollectRoads(Scene scene)
    {
        List<RoadTile> roads = new List<RoadTile>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].transform.GetComponentsInChildren<Transform>(true);

            for (int c = 0; c < all.Length; c++)
            {
                Transform t = all[c];

                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                {
                    continue;
                }

                string path =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);

                if (string.IsNullOrEmpty(path) || !path.Contains(RoadPackage))
                {
                    continue;
                }

                RoadTile tile = new RoadTile();
                tile.prefab = Path.GetFileNameWithoutExtension(path);
                tile.transform = t;
                tile.groundY = t.position.y;

                MeshRenderer[] renderers = t.GetComponentsInChildren<MeshRenderer>(true);
                List<SubInfo> subs = new List<SubInfo>();

                for (int r = 0; r < renderers.Length; r++)
                {
                    MeshRenderer mr = renderers[r];
                    MeshFilter mf = mr.GetComponent<MeshFilter>();

                    if (mf == null || mf.sharedMesh == null)
                    {
                        continue;
                    }

                    Mesh mesh = mf.sharedMesh;

                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        SubInfo info = new SubInfo();
                        info.material = "<missing>";

                        if (s < mr.sharedMaterials.Length && mr.sharedMaterials[s] != null)
                        {
                            info.material = mr.sharedMaterials[s].name;
                        }

                        info.bounds = SubmeshWorldBounds(mr, s, out info.ok);
                        subs.Add(info);
                    }
                }

                SubInfo road = null;
                SubInfo walk = null;

                for (int q = 0; q < subs.Count; q++)
                {
                    if (!subs[q].ok)
                    {
                        continue;
                    }

                    if (subs[q].material == BlackRoadMaterial)
                    {
                        road = road == null ? subs[q] : Merge(road, subs[q]);
                    }
                    else if (subs[q].material == PavementMaterial)
                    {
                        walk = walk == null ? subs[q] : Merge(walk, subs[q]);
                    }
                }

                if (road == null)
                {
                    road = FindFlat(subs, false);
                }

                if (walk == null)
                {
                    walk = FindFlat(subs, true);
                }

                if (road != null)
                {
                    tile.road = road.bounds;
                    tile.hasRoad = true;
                }

                if (walk != null)
                {
                    tile.walk = walk.bounds;
                    tile.hasWalk = true;
                }

                if (tile.hasRoad)
                {
                    float w = tile.road.size.x;
                    float h = tile.road.size.z;
                    tile.straight = w > h * 1.5f || h > w * 1.5f;
                }

                roads.Add(tile);
            }
        }

        return roads;
    }

    static Bounds SubmeshWorldBounds(MeshRenderer mr, int sub, out bool ok)
    {
        ok = false;
        MeshFilter mf = mr.GetComponent<MeshFilter>();

        if (mf == null || mf.sharedMesh == null)
        {
            return new Bounds();
        }

        Mesh mesh = mf.sharedMesh;
        int[] triangles = mesh.GetTriangles(sub);

        if (triangles == null || triangles.Length == 0)
        {
            return new Bounds();
        }

        Vector3[] vertices = mesh.vertices;
        Matrix4x4 matrix = mr.transform.localToWorldMatrix;
        Bounds bounds = new Bounds();
        bool first = true;

        for (int i = 0; i < triangles.Length; i++)
        {
            int index = triangles[i];

            if (index < 0 || index >= vertices.Length)
            {
                continue;
            }

            Vector3 p = matrix.MultiplyPoint3x4(vertices[index]);

            if (first)
            {
                bounds = new Bounds(p, Vector3.zero);
                first = false;
            }
            else
            {
                bounds.Encapsulate(p);
            }
        }

        ok = !first;
        return bounds;
    }

    static SubInfo Merge(SubInfo a, SubInfo b)
    {
        a.bounds.Encapsulate(b.bounds);
        return a;
    }

    /// <summary>
    /// The pavement is the highest flat band of a tile, the driving surface is
    /// the lowest one. Used when a tile's road material is missing.
    /// </summary>
    static SubInfo FindFlat(List<SubInfo> subs, bool highest)
    {
        SubInfo best = null;

        for (int i = 0; i < subs.Count; i++)
        {
            SubInfo info = subs[i];

            if (!info.ok)
            {
                continue;
            }

            if (info.bounds.size.y > 0.1f)
            {
                continue;
            }

            if (info.bounds.size.x < 1f || info.bounds.size.z < 1f)
            {
                continue;
            }

            if (best == null ||
                (highest ? info.bounds.min.y > best.bounds.min.y
                         : info.bounds.min.y < best.bounds.min.y))
            {
                best = info;
            }
        }

        return best;
    }

    // ------------------------------------------------------------------
    // Street runs
    // ------------------------------------------------------------------

    static List<Run> BuildRuns(List<RoadTile> roads)
    {
        List<Run> runs = new List<Run>();

        for (int axis = 0; axis < 2; axis++)
        {
            List<RoadTile> list = new List<RoadTile>();

            for (int i = 0; i < roads.Count; i++)
            {
                RoadTile t = roads[i];

                if (!t.straight || !t.hasWalk)
                {
                    continue;
                }

                bool alongX = t.road.size.x > t.road.size.z;

                if ((alongX ? 0 : 1) == axis)
                {
                    list.Add(t);
                }
            }

            list.Sort(delegate (RoadTile a, RoadTile b)
            {
                return Cross(a, axis).CompareTo(Cross(b, axis));
            });

            List<List<RoadTile>> clusters = new List<List<RoadTile>>();
            List<RoadTile> cluster = null;
            float clusterCross = 0f;

            for (int i = 0; i < list.Count; i++)
            {
                float cross = Cross(list[i], axis);

                if (cluster == null || Mathf.Abs(cross - clusterCross) > 3f)
                {
                    cluster = new List<RoadTile>();
                    clusters.Add(cluster);
                    clusterCross = cross;
                }

                cluster.Add(list[i]);
                clusterCross = (clusterCross * (cluster.Count - 1) + cross) / cluster.Count;
            }

            for (int c = 0; c < clusters.Count; c++)
            {
                List<RoadTile> group = clusters[c];
                group.Sort(delegate (RoadTile a, RoadTile b)
                {
                    return Along(a, axis).CompareTo(Along(b, axis));
                });

                Run current = null;

                for (int i = 0; i < group.Count; i++)
                {
                    RoadTile t = group[i];
                    float aMin = Along(t, axis);
                    float aMax = axis == 0 ? t.road.max.x : t.road.max.z;

                    if (current == null || aMin > current.alongMax + 1.5f)
                    {
                        current = new Run();
                        current.axis = axis;
                        current.alongMin = aMin;
                        current.alongMax = aMax;
                        current.walkMin = axis == 0 ? t.walk.min.z : t.walk.min.x;
                        current.walkMax = axis == 0 ? t.walk.max.z : t.walk.max.x;
                        current.groundY = t.groundY;
                        runs.Add(current);
                    }
                    else
                    {
                        current.alongMax = Mathf.Max(current.alongMax, aMax);
                        current.walkMin = Mathf.Min(current.walkMin,
                            axis == 0 ? t.walk.min.z : t.walk.min.x);
                        current.walkMax = Mathf.Max(current.walkMax,
                            axis == 0 ? t.walk.max.z : t.walk.max.x);
                        current.groundY = (current.groundY * current.tiles.Count +
                            t.groundY) / (current.tiles.Count + 1);
                    }

                    current.tiles.Add(t);
                }
            }
        }

        return runs;
    }

    static float Cross(RoadTile t, int axis)
    {
        return axis == 0
            ? (t.road.min.z + t.road.max.z) * 0.5f
            : (t.road.min.x + t.road.max.x) * 0.5f;
    }

    static float Along(RoadTile t, int axis)
    {
        return axis == 0 ? t.road.min.x : t.road.min.z;
    }

    // ------------------------------------------------------------------
    // Obstacles
    // ------------------------------------------------------------------

    static void CollectObstacles(
        Scene scene, List<Bounds> bounds, List<string> names)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (IsUnder(roots[i].transform, "CityBuildings"))
            {
                continue;
            }

            Transform[] all = roots[i].transform.GetComponentsInChildren<Transform>(true);

            for (int c = 0; c < all.Length; c++)
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(all[c].gameObject))
                {
                    continue;
                }

                if (IsUnder(all[c].transform, "CityBuildings"))
                {
                    continue;
                }

                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    all[c].gameObject);

                if (string.IsNullOrEmpty(path) || path.Contains(RoadPackage))
                {
                    continue;
                }

                Bounds total;

                if (TryFootprint(all[c], out total))
                {
                    bounds.Add(total);
                    names.Add(Path.GetFileNameWithoutExtension(path));
                }
            }
        }
    }

    /// <summary>
    /// Footprint of a prefab instance, ignoring decals, transparent helpers,
    /// low detail LODs and the fake light volumes carried by street props.
    /// </summary>
    static bool TryFootprint(Transform root, out Bounds total)
    {
        total = new Bounds(root.position, Vector3.zero);

        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        List<MeshRenderer> usable = new List<MeshRenderer>();
        List<MeshRenderer> lodZero = new List<MeshRenderer>();

        for (int r = 0; r < renderers.Length; r++)
        {
            MeshRenderer mr = renderers[r];

            if (IsUnder(mr.transform, "decals") || IsUnder(mr.transform, "transparent"))
            {
                continue;
            }

            usable.Add(mr);

            if (IsUnder(mr.transform, "LOD_0"))
            {
                lodZero.Add(mr);
            }
        }

        List<MeshRenderer> use = lodZero.Count > 0 ? lodZero : usable;
        bool any = false;

        for (int r = 0; r < use.Count; r++)
        {
            Bounds rb = use[r].bounds;

            if (Mathf.Max(rb.size.x, rb.size.z) > 100f)
            {
                continue;
            }

            if (!any)
            {
                total = rb;
                any = true;
            }
            else
            {
                total.Encapsulate(rb);
            }
        }

        if (!any)
        {
            return false;
        }

        return Mathf.Max(total.size.x, total.size.z) <= MaxObstacleSize;
    }

    static bool IsUnder(Transform t, string name)
    {
        Transform cur = t;

        while (cur != null)
        {
            if (cur.name == name || cur.name.Contains(name))
            {
                return true;
            }

            cur = cur.parent;
        }

        return false;
    }

    // ------------------------------------------------------------------
    // Houses
    // ------------------------------------------------------------------

    static House LoadHouse(string name)
    {
        string path = PrefabRoot + name + ".prefab";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (asset == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        Bounds total;
        bool ok = TryFootprint(instance.transform, out total);
        UnityEngine.Object.DestroyImmediate(instance);

        if (!ok)
        {
            return null;
        }

        House house = new House();
        house.name = name;
        house.prefabPath = path;
        house.localMin = total.min;
        house.localMax = total.max;
        house.width = total.size.x;
        house.depth = total.size.z;
        return house;
    }

    static House PickHouse(List<House> houses, ref int index, float maxWidth)
    {
        if (houses.Count == 0)
        {
            return null;
        }

        for (int tries = 0; tries < houses.Count; tries++)
        {
            House h = houses[index % houses.Count];
            index++;

            if (h.width <= MaxHouseWidth && h.width >= 6f && h.width <= maxWidth)
            {
                return h;
            }
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Geometry helpers
    // ------------------------------------------------------------------

    static Vector3 LowNormal(int axis)
    {
        return axis == 0 ? new Vector3(0f, 0f, -1f) : new Vector3(-1f, 0f, 0f);
    }

    static Vector3 HighNormal(int axis)
    {
        return axis == 0 ? new Vector3(0f, 0f, 1f) : new Vector3(1f, 0f, 0f);
    }

    /// <summary>Yaw that turns the local +Z front towards the street.</summary>
    static float YawFacing(int axis, bool lowSide)
    {
        Vector3 toward = lowSide
            ? (axis == 0 ? new Vector3(0f, 0f, 1f) : new Vector3(1f, 0f, 0f))
            : (axis == 0 ? new Vector3(0f, 0f, -1f) : new Vector3(-1f, 0f, 0f));

        return Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
    }

    static Bounds FootprintWorld(House house, float yaw)
    {
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        Vector3[] corners =
        {
            new Vector3(house.localMin.x, 0f, house.localMin.z),
            new Vector3(house.localMax.x, 0f, house.localMin.z),
            new Vector3(house.localMax.x, 0f, house.localMax.z),
            new Vector3(house.localMin.x, 0f, house.localMax.z),
        };

        Bounds b = new Bounds(q * corners[0], Vector3.zero);

        for (int i = 1; i < corners.Length; i++)
        {
            b.Encapsulate(q * corners[i]);
        }

        return b;
    }

    static Bounds OffsetBounds(Bounds b, Vector3 offset)
    {
        Bounds result = b;
        result.center += offset;
        return result;
    }

    static bool IsClear(
        Bounds house,
        List<Bounds> roads,
        List<Bounds> walkBands,
        List<Bounds> junctions,
        List<Bounds> obstacles,
        List<Bounds> placedHouses)
    {
        for (int i = 0; i < roads.Count; i++)
        {
            if (Overlaps(house, roads[i], 0f))
            {
                return false;
            }
        }

        for (int i = 0; i < junctions.Count; i++)
        {
            if (Overlaps(house, junctions[i], 0f))
            {
                return false;
            }
        }

        // The house rests on the outer edge of the pavement, so a shallow
        // overlap with a pavement strip is expected and allowed.
        for (int i = 0; i < walkBands.Count; i++)
        {
            if (Overlaps(house, walkBands[i], 2.5f))
            {
                return false;
            }
        }

        for (int i = 0; i < obstacles.Count; i++)
        {
            if (Overlaps(house, obstacles[i], 0.5f))
            {
                return false;
            }
        }

        // Keep a small gap between buildings, including at street corners.
        for (int i = 0; i < placedHouses.Count; i++)
        {
            if (Overlaps(house, placedHouses[i], 0.5f))
            {
                return false;
            }
        }

        return true;
    }

    static bool Overlaps(Bounds a, Bounds b, float tolerance)
    {
        float ox = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        float oz = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);

        return ox > tolerance && oz > tolerance;
    }

    // ------------------------------------------------------------------
    // Colliders
    // ------------------------------------------------------------------

    /// <summary>Adds a mesh collider to the LOD_0 meshes of every house prefab.</summary>
    static void EnsurePrefabColliders()
    {
        for (int i = 0; i < BuildingPrefabs.Length; i++)
        {
            string path = PrefabRoot + BuildingPrefabs[i] + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int added = AddColliders(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            if (added > 0)
            {
                Debug.Log("CityBuilder: " + BuildingPrefabs[i] + " colliders +" + added);
            }
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>Adds mesh colliders to the building instances already in the scene.</summary>
    static void EnsureSceneColliders(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].transform.GetComponentsInChildren<Transform>(true);

            for (int c = 0; c < all.Length; c++)
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(all[c].gameObject))
                {
                    continue;
                }

                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    all[c].gameObject);

                if (string.IsNullOrEmpty(path) || !path.Contains(BuildingPackage))
                {
                    continue;
                }

                AddColliders(all[c].gameObject);
            }
        }
    }

    static int AddColliders(GameObject root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        int added = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer mr = renderers[i];

            if (IsUnder(mr.transform, "decals") || IsUnder(mr.transform, "transparent"))
            {
                continue;
            }

            // Only the highest detail mesh needs a collider.
            if (IsUnder(mr.transform, "LOD_1") ||
                IsUnder(mr.transform, "LOD_2") ||
                IsUnder(mr.transform, "LOD_3"))
            {
                continue;
            }

            MeshFilter mf = mr.GetComponent<MeshFilter>();

            if (mf == null || mf.sharedMesh == null)
            {
                continue;
            }

            if (mr.GetComponent<MeshCollider>() == null)
            {
                MeshCollider collider = mr.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mf.sharedMesh;
                added++;
            }
        }

        return added;
    }
}
