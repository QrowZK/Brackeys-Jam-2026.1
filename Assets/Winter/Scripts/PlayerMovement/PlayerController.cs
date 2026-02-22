using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Winter.Input;

namespace Winter.Player
{
    public class PlayerController : MonoBehaviour
    {

        [Header("Player Stats and Settings:")]
        public int healthPoints;
        public float walkSpeed;
        public float runSpeed;
        public float jumpHeight;
        public bool MoveAlongWorldCoords;
        public float GravityMagnitude = 9.8f;

        [Header("Transforms:")]
        public Transform graphics;
        public Transform mainColliderTransform;

        [Header("Ground Check:")]
        public LayerMask playerMask;
        public float groundCheckerOffset;
        public float groundCheckRadius;


        private Animator animator;
        private Rigidbody rb;


        [HideInInspector] public Vector3 worldSpaceGravityVector;
        public Vector3 worldSpaceMoveDirection { get; private set; }

        private bool isJumping;
        private bool isGrounded;
        private Collider[] nonAllocColQueryBuffer;
        private float jumpForce;
        private float groundValidationInterval = 0.2f;
        private float timer = 0;
        private bool timerOn;

        [Header("Audio Feedback")]
        public AudioSource audioSource;
        public List<AudioClip> stepSounds = new();
        public AudioClip jumpUpAudioClip;
        public AudioClip landAudioClip;
        public bool DisableDefaultSoundImplementation;


        public static PlayerController Instance;

        public Action OnStepSFX;
        public Action OnJumpUpSFX;
        public Action OnLandSFX;


        private bool IsPushingPulling;

        [Header("Push Pull Settings:")]
        public float pushPullSenseDistance;
        private HingeJoint joint;



        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


        void Start()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();

            nonAllocColQueryBuffer = new Collider[2];
            rb = GetComponent<Rigidbody>();
            worldSpaceGravityVector = -Vector3.up;
            jumpForce = Mathf.Sqrt(2 * 9.8f * jumpHeight);

            if (!DisableDefaultSoundImplementation)
                OnStepSFX += () => PlayAudioClip(stepSounds[UnityEngine.Random.Range(0, stepSounds.Count)]);

        }

        void Update()
        {

            CheckPushPull();

            var alongRefCoords = (MoveAlongWorldCoords ? Quaternion.identity : Camera.main.transform.rotation) * new Vector3(InputManager.Instance.MoveRaw.x, 0, InputManager.Instance.MoveRaw.y);
            worldSpaceMoveDirection = Vector3.ProjectOnPlane(alongRefCoords, mainColliderTransform.up).normalized;
            //worldSpaceMoveDirection = transform.rotation * alongCam;

            Debug.DrawLine(transform.position, transform.position + worldSpaceMoveDirection.normalized * 10f, Color.red);

            mainColliderTransform.rotation = Quaternion.Lerp(mainColliderTransform.rotation, Quaternion.LookRotation(worldSpaceMoveDirection == Vector3.zero ? Vector3.ProjectOnPlane(mainColliderTransform.forward, -worldSpaceGravityVector).normalized : worldSpaceMoveDirection.normalized, -worldSpaceGravityVector), Time.deltaTime * 8f);


            if (!IsPushingPulling)
            {
                graphics.transform.rotation = mainColliderTransform.rotation;
            }



            animator.SetFloat("Move", Mathf.Clamp01(worldSpaceMoveDirection.magnitude));

        }

        private void CheckPushPull()
        {
            if (Physics.Raycast(graphics.position, graphics.forward, out RaycastHit hitInfo, pushPullSenseDistance, LayerMask.GetMask("IO")) && InputManager.Instance.InteractionBeingPressed)
            {
                IsPushingPulling = true;
                var dir = Vector3.ProjectOnPlane(hitInfo.transform.position - mainColliderTransform.position, -worldSpaceGravityVector).normalized;
                graphics.rotation = Quaternion.LookRotation(dir, -worldSpaceGravityVector);

                if (joint == null)
                    joint = gameObject.AddComponent<HingeJoint>();

                joint.connectedBody = hitInfo.transform.GetComponent<Rigidbody>();

            }
            else
            {
                if (joint)
                {
                    var oldJoint = joint;
                    joint = null;
                    Destroy(oldJoint);
                }
                IsPushingPulling = false;
            }
        }

        private void CheckIfGrounded()
        {
            if (timerOn && timer < groundValidationInterval)
            {
                timer += Time.deltaTime;
                isGrounded = false;
                return;
            }

            timerOn = false;
            timer = 0;
            isGrounded = Physics.OverlapSphereNonAlloc(mainColliderTransform.position - mainColliderTransform.up * groundCheckerOffset, groundCheckRadius, nonAllocColQueryBuffer, playerMask, QueryTriggerInteraction.Ignore) > 0;

            animator.SetBool("Grounded", isGrounded);
        }

        void FixedUpdate()
        {
            CheckIfGrounded();

            var gravitationalForce = worldSpaceGravityVector * GravityMagnitude;
            rb.AddForce(gravitationalForce, ForceMode.Acceleration);

            var targetVel = worldSpaceMoveDirection.normalized * walkSpeed;

            targetVel = mainColliderTransform.InverseTransformVector(targetVel);
            targetVel.y = mainColliderTransform.InverseTransformVector(rb.linearVelocity).y;
            targetVel = mainColliderTransform.TransformVector(targetVel);

            rb.linearVelocity = targetVel;

            if (isGrounded && isJumping)
            {
                isJumping = false;
                OnLandSFX?.Invoke();

                if (!DisableDefaultSoundImplementation)
                    PlayAudioClip(landAudioClip);
            }

            if (InputManager.Instance.JumpPressedThisFrame && !isJumping && isGrounded)
            {
                OnJumpUpSFX?.Invoke();
                animator.SetTrigger("Jump");
                if (!DisableDefaultSoundImplementation)
                    PlayAudioClip(jumpUpAudioClip);
                var vell = mainColliderTransform.InverseTransformVector(rb.linearVelocity);
                vell.y = 0;
                rb.linearVelocity = mainColliderTransform.TransformVector(vell);
                rb.AddForce(mainColliderTransform.up * jumpForce, ForceMode.VelocityChange);
                isJumping = true;
                timerOn = true;
            }

        }

        public void SetGravityMagnitude(float val)
        {
            GravityMagnitude = val;
        }



        private void PlayAudioClip(AudioClip clip)
        {
            if (clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }



        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(mainColliderTransform.position - mainColliderTransform.up * groundCheckerOffset, groundCheckRadius);
        }


        [ContextMenu("FlipGravity")]
        public void FlipGravity()
        {
            this.worldSpaceGravityVector = -this.worldSpaceGravityVector;
            animator.SetTrigger("Flip");
        }

        [ContextMenu("Gravity along +X")]
        public void GravityAlongX()
        {
            this.worldSpaceGravityVector = Vector3.right;
            animator.SetTrigger("Flip");
        }

        [ContextMenu("Flip Along -Y")]
        public void GravityAlongY()
        {
            this.worldSpaceGravityVector = -Vector3.up;
            animator.SetTrigger("Flip");
        }
    }
}

