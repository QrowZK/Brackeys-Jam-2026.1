using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Winter.Input
{
    public class InputManager : MonoBehaviour
    {
        public InputActionAsset inputAsset;


        public static InputManager Instance;

        private InputAction moveAction;
        private InputAction jumpAction;
        private Vector2 moveRaw;

        public Vector2 MoveRaw { get => moveRaw; }
        private bool jumpPressedThisFrame;
        public bool JumpPressedThisFrame => jumpPressedThisFrame;

        public bool InteractionBeingPressed => Mouse.current != null ? Mouse.current.leftButton.isPressed : false;



        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            moveAction = inputAsset.FindActionMap("Player").FindAction("Move");

            moveAction.performed += (ctx) =>
            {
                moveRaw = moveAction.ReadValue<Vector2>();
            };
            moveAction.canceled += (ctx) =>
            {
                moveRaw = moveAction.ReadValue<Vector2>();
            };

            jumpAction = inputAsset.FindActionMap("Player").FindAction("Jump");

            jumpAction.performed += (ctx) =>
            {
                jumpPressedThisFrame = true;
            };
            jumpAction.canceled += (ctx) =>
            {
                jumpPressedThisFrame = false;
            };


        }



        void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
        }
    }
}

