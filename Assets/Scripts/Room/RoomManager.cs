using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RoomManager : MonoBehaviour
{
    [Serializable]
    public class RoomDefinition
    {
        public string roomName;
        public GameObject prefab;
        public string requiredItem;
        public AudioClip roomMusic;
    }

    [Header("Rooms")]
    public List<RoomDefinition> rooms = new List<RoomDefinition>();

    [Header("Scene Refs")]
    [Tooltip("This should be your World/CubeRoot. Rooms instantiate under it.")]
    public Transform roomContainer;

    public Transform playerRoot;
    public MockInventory inventory;
    
    [SerializeField] private RoomTransitionController transition;

    public event Action<RoomDefinition> OnRoomLoading;
    public event Action<Room> OnRoomLoaded;
    public event Action<string> OnRoomBlocked;

    public Room CurrentRoom { get; private set; }
    public int CurrentIndex { get; private set; } = -1;

    private bool busy;

    private void OnEnable()
    {
        Portal.OnPortalTriggered += HandlePortal;
        ItemPickup.OnItemCollected += HandleItem;
    }

    private void OnDisable()
    {
        Portal.OnPortalTriggered -= HandlePortal;
        ItemPickup.OnItemCollected -= HandleItem;
    }

    public void LoadRoomByIndex(int index)
    {
        if (busy) return;

        if (index < 0 || index >= rooms.Count)
        {
            OnRoomBlocked?.Invoke("Invalid room index");
            return;
        }

        StartCoroutine(LoadRoutine(index, "Spawn"));
    }

    private void HandlePortal(Portal portal, int targetIndex, string spawnName, string portalRequiredItem)
    {
        if (busy) return;

        if (targetIndex < 0 || targetIndex >= rooms.Count)
        {
            OnRoomBlocked?.Invoke("Invalid portal target");
            return;
        }

        var def = rooms[targetIndex];

        string required = "";
        if (!string.IsNullOrWhiteSpace(def.requiredItem)) required = def.requiredItem;
        if (!string.IsNullOrWhiteSpace(portalRequiredItem)) required = portalRequiredItem;

        if (!string.IsNullOrWhiteSpace(required))
        {
            if (inventory == null || !inventory.Has(required))
            {
                OnRoomBlocked?.Invoke("Missing item: " + required);
                return;
            }
        }

        StartCoroutine(LoadRoutine(targetIndex, spawnName));
    }

    private IEnumerator LoadRoutine(int index, string spawnName)
    {
        busy = true;

        var def = rooms[index];
        OnRoomLoading?.Invoke(def);

        if (transition != null)
        {
            yield return transition.RunTransition(() =>
            {
                UnloadCurrent();
                Spawn(def, index, spawnName);
            });
        }
        else
        {
            UnloadCurrent();
            Spawn(def, index, spawnName);
            yield return null;
        }

        busy = false;
    }


    private void Spawn(RoomDefinition def, int index, string spawnName)
    {
        if (def.prefab == null)
        {
            OnRoomBlocked?.Invoke("Missing prefab");
            return;
        }

        if (roomContainer == null)
        {
            OnRoomBlocked?.Invoke("RoomContainer is not assigned (should be World/CubeRoot)");
            return;
        }

        var inst = Instantiate(def.prefab, roomContainer);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        var room = inst.GetComponent<Room>();
        if (room == null) room = inst.AddComponent<Room>();

        room.Initialize();
        room.Activate();

        CurrentRoom = room;
        CurrentIndex = index;

        MovePlayerToSpawn(spawnName);

        OnRoomLoaded?.Invoke(room);
    }

    private void UnloadCurrent()
    {
        if (CurrentRoom == null) return;
        Destroy(CurrentRoom.gameObject);
        CurrentRoom = null;
        CurrentIndex = -1;
    }

    private void MovePlayerToSpawn(string spawnName)
    {
        if (playerRoot == null) return;
        if (CurrentRoom == null) return;

        var anchor = CurrentRoom.GetAnchor(spawnName);
        if (anchor == null) anchor = CurrentRoom.DefaultSpawn;
        if (anchor == null) return;

        playerRoot.position = anchor.position;
        playerRoot.rotation = anchor.rotation;
    }

    private void HandleItem(string itemId)
    {
        if (inventory != null) inventory.Add(itemId);
    }

    //todo REMOVE this later. 
    private void Start()
    {
        LoadRoomByIndex(0);
    }

    private void Update()
    {
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            LoadRoomByIndex(--CurrentIndex);
        }
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            LoadRoomByIndex(++CurrentIndex);
        }
    }
}
