using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class RoomPrefabAuthoringTool : MonoBehaviour
{
    [Header("Room")]
    public string roomName = "Test";
    public Vector3 size = new Vector3(6f, 3f, 6f);

    [Header("Visuals")]
    public Color interiorColor = new Color(0.2f, 0.8f, 0.9f, 1f);
    public Color exteriorColor = new Color(0.9f, 0.4f, 0.2f, 1f);

    [Header("Walls")]
    [Tooltip("Adds a BoxCollider to every wall face. Colliders remain active regardless of visibility.")]
    public bool addWallColliders = true;

    [Tooltip("Local Z thickness for the BoxCollider on each face (the quad's normal axis).")]
    public float wallColliderThickness = 0.06f;

    [Tooltip("Adds WallVisual to every wall face so visibility systems can fade walls without disabling colliders.")]
    public bool addWallVisuals = true;

    [Header("Generated Content")]
    public bool createOnePuzzlePerRoom = true;
    public bool createOnePortalPerInteriorWall = true;

    [Header("Portal Defaults")]
    public int defaultTargetRoomIndex = 0;
    public string defaultTargetSpawnName = "Spawn";
    public bool portalRequireInteraction = false;

    [Header("Options")]
    public bool clearExisting = true;

#if UNITY_EDITOR
    [ContextMenu("Create Room In Scene")]
    public void CreateRoomInScene()
    {
        RoomPrefabBuilder.Create(roomName, size, interiorColor, exteriorColor,
            createOnePuzzlePerRoom,
            createOnePortalPerInteriorWall,
            defaultTargetRoomIndex,
            defaultTargetSpawnName,
            portalRequireInteraction,
            addWallColliders,
            wallColliderThickness,
            addWallVisuals,
            clearExisting);
    }

    [ContextMenu("Rebuild This Room")]
    public void RebuildThisRoom()
    {
        RoomPrefabBuilder.Rebuild(gameObject, size, interiorColor, exteriorColor,
            createOnePuzzlePerRoom,
            createOnePortalPerInteriorWall,
            defaultTargetRoomIndex,
            defaultTargetSpawnName,
            portalRequireInteraction,
            addWallColliders,
            wallColliderThickness,
            addWallVisuals);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(RoomPrefabAuthoringTool))]
public sealed class RoomPrefabAuthoringToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var t = (RoomPrefabAuthoringTool)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Create Room In Scene", GUILayout.Height(28)))
        {
            t.CreateRoomInScene();
        }

        if (GUILayout.Button("Rebuild This Room", GUILayout.Height(28)))
        {
            t.RebuildThisRoom();
        }
    }
}

internal static class RoomPrefabBuilder
{
    public static void Create(
        string roomName,
        Vector3 size,
        Color interiorColor,
        Color exteriorColor,
        bool createOnePuzzlePerRoom,
        bool createOnePortalPerInteriorWall,
        int defaultTargetRoomIndex,
        string defaultTargetSpawnName,
        bool portalRequireInteraction,
        bool addWallColliders,
        float wallColliderThickness,
        bool addWallVisuals,
        bool clearExisting)
    {
        string rootName = "Room_" + roomName;

        var existing = GameObject.Find(rootName);
        if (existing != null && clearExisting)
        {
            Object.DestroyImmediate(existing);
        }

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Room");

        EnsureComponent<Room>(root);

        // Ensure tool is on root for quick rebuild tweaks
        var tool = root.GetComponent<RoomPrefabAuthoringTool>();
        if (tool == null) tool = root.AddComponent<RoomPrefabAuthoringTool>();
        tool.roomName = roomName;
        tool.size = size;
        tool.interiorColor = interiorColor;
        tool.exteriorColor = exteriorColor;
        tool.createOnePuzzlePerRoom = createOnePuzzlePerRoom;
        tool.createOnePortalPerInteriorWall = createOnePortalPerInteriorWall;
        tool.defaultTargetRoomIndex = defaultTargetRoomIndex;
        tool.defaultTargetSpawnName = defaultTargetSpawnName;
        tool.portalRequireInteraction = portalRequireInteraction;
        tool.addWallColliders = addWallColliders;
        tool.wallColliderThickness = wallColliderThickness;
        tool.addWallVisuals = addWallVisuals;
        tool.clearExisting = false;

        BuildOrRebuild(root, size, interiorColor, exteriorColor,
            createOnePuzzlePerRoom,
            createOnePortalPerInteriorWall,
            defaultTargetRoomIndex,
            defaultTargetSpawnName,
            portalRequireInteraction,
            addWallColliders,
            wallColliderThickness,
            addWallVisuals,
            destroyGeneratedFirst: false);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    public static void Rebuild(
        GameObject roomRoot,
        Vector3 size,
        Color interiorColor,
        Color exteriorColor,
        bool createOnePuzzlePerRoom,
        bool createOnePortalPerInteriorWall,
        int defaultTargetRoomIndex,
        string defaultTargetSpawnName,
        bool portalRequireInteraction,
        bool addWallColliders,
        float wallColliderThickness,
        bool addWallVisuals)
    {
        if (roomRoot == null) return;

        Undo.RegisterFullObjectHierarchyUndo(roomRoot, "Rebuild Room");

        BuildOrRebuild(roomRoot, size, interiorColor, exteriorColor,
            createOnePuzzlePerRoom,
            createOnePortalPerInteriorWall,
            defaultTargetRoomIndex,
            defaultTargetSpawnName,
            portalRequireInteraction,
            addWallColliders,
            wallColliderThickness,
            addWallVisuals,
            destroyGeneratedFirst: true);

        EditorUtility.SetDirty(roomRoot);
    }

    private static void BuildOrRebuild(
        GameObject root,
        Vector3 size,
        Color interiorColor,
        Color exteriorColor,
        bool createOnePuzzlePerRoom,
        bool createOnePortalPerInteriorWall,
        int defaultTargetRoomIndex,
        string defaultTargetSpawnName,
        bool portalRequireInteraction,
        bool addWallColliders,
        float wallColliderThickness,
        bool addWallVisuals,
        bool destroyGeneratedFirst)
    {
        // Roots
        var geometry = EnsureChild(root.transform, "Geometry");
        var interior = EnsureChild(geometry, "Interior");
        var exterior = EnsureChild(geometry, "Exterior");

        var anchors = EnsureChild(root.transform, "Anchors");
        var spawn = EnsureChild(anchors, "Spawn");
        spawn.localPosition = new Vector3(0f, 0.1f, 0f);

        var portalsRoot = EnsureChild(root.transform, "Portals");
        var puzzlesRoot = EnsureChild(root.transform, "Puzzles");
        EnsureChild(root.transform, "Items");

        var audioRoot = EnsureChild(root.transform, "Audio");
        var roomAudio = EnsureChild(audioRoot, "RoomAudio");
        EnsureAudioSource(roomAudio);

        EnsureChild(root.transform, "Runtime");

        // Clear generated children if rebuild
        if (destroyGeneratedFirst)
        {
            DestroyChildren(interior);
            DestroyChildren(exterior);
            DestroyGeneratedPortals(portalsRoot);
            DestroyGeneratedPuzzle(puzzlesRoot);
        }

        // Materials
        var interiorMat = MakeMat($"{root.name}_InteriorMat", interiorColor);
        var exteriorMat = MakeMat($"{root.name}_ExteriorMat", exteriorColor);

        // Build faces
        BuildFaceSet(interior, size, true, interiorMat, addWallColliders, wallColliderThickness, addWallVisuals);
        BuildFaceSet(exterior, size, false, exteriorMat, addWallColliders, wallColliderThickness, addWallVisuals);

        // Generate portals (one per interior face)
        if (createOnePortalPerInteriorWall)
        {
            CreatePortalsForInteriorFaces(portalsRoot, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
        }

        // Generate one room puzzle
        if (createOnePuzzlePerRoom)
        {
            CreateRoomPuzzle(puzzlesRoot);
        }

        // Assign Room serialized refs
        AssignRoomReferences(root.GetComponent<Room>(), geometry, anchors, puzzlesRoot, spawn);
    }

    private static void BuildFaceSet(Transform parent, Vector3 size, bool isInterior, Material mat,
        bool addWallColliders, float wallColliderThickness, bool addWallVisuals)
    {
        CreateFace(parent, "Face_Front", FaceName.Front, size, isInterior, mat, addWallColliders, wallColliderThickness, addWallVisuals);
        CreateFace(parent, "Face_Back", FaceName.Back, size, isInterior, mat, addWallColliders, wallColliderThickness, addWallVisuals);
        CreateFace(parent, "Face_Left", FaceName.Left, size, isInterior, mat, addWallColliders, wallColliderThickness, addWallVisuals);
        CreateFace(parent, "Face_Right", FaceName.Right, size, isInterior, mat, addWallColliders, wallColliderThickness, addWallVisuals);
        CreateFace(parent, "Face_Top", FaceName.Top, size, isInterior, mat, addWallColliders, wallColliderThickness, addWallVisuals);
        CreateFace(parent, "Face_Bottom", FaceName.Bottom, size, isInterior, mat, addWallColliders, wallColliderThickness, addWallVisuals);
    }

    private static void CreateFace(Transform parent, string name, FaceName face, Vector3 size, bool isInterior, Material mat,
        bool addWallColliders, float wallColliderThickness, bool addWallVisuals)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(go, "Create Face");

        go.name = name;
        go.transform.SetParent(parent, false);

        // Replace Unity's default collider with a BoxCollider (cheaper, predictable thickness)
        var existingCol = go.GetComponent<Collider>();
        if (existingCol != null) Object.DestroyImmediate(existingCol);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;

        var wall = EnsureComponent<Wall>(go);
        wall.faceName = face;
        wall.isInterior = isInterior;

        if (addWallVisuals)
        {
            // WallVisual should fade renderers only, colliders stay active.
            EnsureComponent<WallVisual>(go);
        }

        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float hz = size.z * 0.5f;

        Vector3 pos;
        Quaternion rot;
        Vector3 scale;

        if (face == FaceName.Front)
        {
            pos = new Vector3(0f, hy, hz);
            rot = Quaternion.Euler(0f, 180f, 0f);
            scale = new Vector3(size.x, size.y, 1f);
        }
        else if (face == FaceName.Back)
        {
            pos = new Vector3(0f, hy, -hz);
            rot = Quaternion.Euler(0f, 0f, 0f);
            scale = new Vector3(size.x, size.y, 1f);
        }
        else if (face == FaceName.Left)
        {
            pos = new Vector3(-hx, hy, 0f);
            rot = Quaternion.Euler(0f, 90f, 0f);
            scale = new Vector3(size.z, size.y, 1f);
        }
        else if (face == FaceName.Right)
        {
            pos = new Vector3(hx, hy, 0f);
            rot = Quaternion.Euler(0f, 270f, 0f);
            scale = new Vector3(size.z, size.y, 1f);
        }
        else if (face == FaceName.Top)
        {
            pos = new Vector3(0f, size.y, 0f);
            rot = Quaternion.Euler(90f, 0f, 0f);
            scale = new Vector3(size.x, size.z, 1f);
        }
        else
        {
            pos = new Vector3(0f, 0f, 0f);
            rot = Quaternion.Euler(270f, 0f, 0f);
            scale = new Vector3(size.x, size.z, 1f);
        }

        if (isInterior)
        {
            rot = rot * Quaternion.Euler(0f, 180f, 0f);
        }

        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;

        if (addWallColliders)
        {
            var box = go.AddComponent<BoxCollider>();
            // Quad is in XY plane, normal is +Z. Thickness goes in local Z.
            box.center = Vector3.zero;
            box.size = new Vector3(1f, 1f, Mathf.Max(0.001f, wallColliderThickness));
        }

        wall.isWalkable = isInterior && face == FaceName.Bottom;
        wall.RebuildCache();
    }

    private static void CreatePortalsForInteriorFaces(
        Transform portalsRoot,
        Vector3 size,
        int defaultTargetRoomIndex,
        string defaultTargetSpawnName,
        bool portalRequireInteraction)
    {
        CreatePortal(portalsRoot, "Portal_Front", FaceName.Front, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
        CreatePortal(portalsRoot, "Portal_Back", FaceName.Back, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
        CreatePortal(portalsRoot, "Portal_Left", FaceName.Left, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
        CreatePortal(portalsRoot, "Portal_Right", FaceName.Right, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
        CreatePortal(portalsRoot, "Portal_Top", FaceName.Top, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
        CreatePortal(portalsRoot, "Portal_Bottom", FaceName.Bottom, size, defaultTargetRoomIndex, defaultTargetSpawnName, portalRequireInteraction);
    }

    private static void CreatePortal(
        Transform portalsRoot,
        string name,
        FaceName face,
        Vector3 size,
        int defaultTargetRoomIndex,
        string defaultTargetSpawnName,
        bool portalRequireInteraction)
    {
        // Remove if exists
        var existing = portalsRoot.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var portalRoot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(portalRoot, "Create Portal");
        portalRoot.transform.SetParent(portalsRoot, false);

        // Mesh (optional)
        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(mesh, "Create Portal Mesh");
        mesh.name = "Mesh";
        mesh.transform.SetParent(portalRoot.transform, false);
        mesh.transform.localScale = new Vector3(0.6f, 1.2f, 0.12f);
        var meshCol = mesh.GetComponent<Collider>();
        if (meshCol != null) Object.DestroyImmediate(meshCol);

        // Trigger
        var trig = new GameObject("Trigger");
        Undo.RegisterCreatedObjectUndo(trig, "Create Portal Trigger");
        trig.transform.SetParent(portalRoot.transform, false);
        var box = trig.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.9f, 1.5f, 0.9f);

        // Portal behaviour
        var portal = EnsureComponent<Portal>(portalRoot);
        portal.targetRoomIndex = defaultTargetRoomIndex;
        portal.spawnName = defaultTargetSpawnName;
        portal.requireInteraction = portalRequireInteraction;

        // Place it near the corresponding interior wall
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float hz = size.z * 0.5f;

        Vector3 pos;
        Quaternion rot;

        if (face == FaceName.Front)
        {
            pos = new Vector3(0f, hy - 0.2f, hz - 0.25f);
            rot = Quaternion.Euler(0f, 180f, 0f);
        }
        else if (face == FaceName.Back)
        {
            pos = new Vector3(0f, hy - 0.2f, -hz + 0.25f);
            rot = Quaternion.Euler(0f, 0f, 0f);
        }
        else if (face == FaceName.Left)
        {
            pos = new Vector3(-hx + 0.25f, hy - 0.2f, 0f);
            rot = Quaternion.Euler(0f, 90f, 0f);
        }
        else if (face == FaceName.Right)
        {
            pos = new Vector3(hx - 0.25f, hy - 0.2f, 0f);
            rot = Quaternion.Euler(0f, 270f, 0f);
        }
        else if (face == FaceName.Top)
        {
            pos = new Vector3(0f, size.y - 0.25f, 0f);
            rot = Quaternion.Euler(270f, 0f, 0f);
        }
        else
        {
            pos = new Vector3(0f, 0.25f, 0f);
            rot = Quaternion.Euler(90f, 0f, 0f);
        }

        portalRoot.transform.localPosition = pos;
        portalRoot.transform.localRotation = rot;
    }

    private static void CreateRoomPuzzle(Transform puzzlesRoot)
    {
        if (puzzlesRoot == null) return;

        var existing = puzzlesRoot.Find("GeneratedRoomPuzzle");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var puzzleGo = new GameObject("GeneratedRoomPuzzle");
        Undo.RegisterCreatedObjectUndo(puzzleGo, "Create Room Puzzle");
        puzzleGo.transform.SetParent(puzzlesRoot, false);
        puzzleGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Requires a concrete puzzle component in your project.
        // Example: SimplePuzzle : PuzzleBase
        puzzleGo.AddComponent<SimplePuzzle>();
    }

    private static void EnsureAudioSource(Transform roomAudio)
    {
        var src = roomAudio.GetComponent<AudioSource>();
        if (src == null) src = roomAudio.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
    }

    private static Material MakeMat(string name, Color color)
    {
        string folderPath = "Assets/Generated/Rooms";

        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Generated", "Rooms");

        string assetPath = $"{folderPath}/{name}.mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var shader = Shader.Find("Custom/URP/ToonDitherOutline");
        var mat = new Material(shader);
        mat.name = name;
        mat.color = color;

        AssetDatabase.CreateAsset(mat, assetPath);
        AssetDatabase.SaveAssets();

        return mat;
    }


    private static Transform EnsureChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Node");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static void DestroyChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void DestroyGeneratedPortals(Transform portalsRoot)
    {
        if (portalsRoot == null) return;

        // Destroy only the ones we generate (Portal_*)
        for (int i = portalsRoot.childCount - 1; i >= 0; i--)
        {
            var c = portalsRoot.GetChild(i);
            if (c.name.StartsWith("Portal_"))
            {
                Object.DestroyImmediate(c.gameObject);
            }
        }
    }

    private static void DestroyGeneratedPuzzle(Transform puzzlesRoot)
    {
        if (puzzlesRoot == null) return;

        var t = puzzlesRoot.Find("GeneratedRoomPuzzle");
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static void AssignRoomReferences(Room room, Transform geometry, Transform anchors, Transform puzzles, Transform spawn)
    {
        if (room == null) return;

        var so = new SerializedObject(room);

        var pGeometry = so.FindProperty("geometryRoot");
        var pAnchors = so.FindProperty("anchorsRoot");
        var pPuzzles = so.FindProperty("puzzlesRoot");
        var pSpawn = so.FindProperty("defaultSpawn");

        if (pGeometry != null) pGeometry.objectReferenceValue = geometry;
        if (pAnchors != null) pAnchors.objectReferenceValue = anchors;
        if (pPuzzles != null) pPuzzles.objectReferenceValue = puzzles;
        if (pSpawn != null) pSpawn.objectReferenceValue = spawn;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(room);
    }
}
#endif
