using UnityEngine;

public static class FloorFaceResolver
{
    //Might just be useless at this point lol
    
    public static FaceName GetFloorFace(Transform cubeRoot)
    {
        if (cubeRoot == null) return FaceName.Bottom;


        Vector3 localDown = cubeRoot.InverseTransformDirection(Vector3.down);

        // Pick the dominant axis
        float ax = Mathf.Abs(localDown.x);
        float ay = Mathf.Abs(localDown.y);
        float az = Mathf.Abs(localDown.z);

        if (ay >= ax && ay >= az)
        {
            return localDown.y > 0f ? FaceName.Top : FaceName.Bottom;
        }

        if (ax >= ay && ax >= az)
        {
            return localDown.x > 0f ? FaceName.Right : FaceName.Left;
        }

        return localDown.z > 0f ? FaceName.Front : FaceName.Back;
    }

    public static FaceName GetCeilingFace(Transform cubeRoot)
    {
        // Ceiling is opposite of floor
        FaceName floor = GetFloorFace(cubeRoot);
        return GetOpposite(floor);
    }

    public static FaceName GetOpposite(FaceName face)
    {
        switch (face)
        {
            case FaceName.Front: return FaceName.Back;
            case FaceName.Back: return FaceName.Front;
            case FaceName.Left: return FaceName.Right;
            case FaceName.Right: return FaceName.Left;
            case FaceName.Top: return FaceName.Bottom;
            case FaceName.Bottom: return FaceName.Top;
            default: return FaceName.Bottom;
        }
    }

    public static Vector3 GetOutwardNormalLocal(FaceName face)
    {
        switch (face)
        {
            case FaceName.Front: return Vector3.forward;
            case FaceName.Back: return Vector3.back;
            case FaceName.Left: return Vector3.left;
            case FaceName.Right: return Vector3.right;
            case FaceName.Top: return Vector3.up;
            case FaceName.Bottom: return Vector3.down;
            default: return Vector3.down;
        }
    }
}