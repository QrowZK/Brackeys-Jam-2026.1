using System.Collections;
using UnityEngine;

namespace Gram
{
    public class Room : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TextMeshProUGUI gravityText;

        public float rotateDuration = 0.25f;
        private bool isRotating;

        private Vector3 gravity = new Vector3(0, -1, 0);
        public Vector3 Gravity
        {
            get => gravity;
            set
            {
                if (value == Vector3.zero)
                    return;

                gravity = value.normalized;

                Physics.gravity = gravity * gravityStrength;

                if (gravityText == null) return;

                gravityText.text = $"Gravity {gravity:F0}";
            }
        }
        public float gravityStrength = 9.8f;
        public bool changeGravity = false;

        readonly private Vector3[] axes =
        {
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back
    };

        public void RotateBy(int index)
        {
            if (isRotating) return;

            Quaternion delta = Quaternion.AngleAxis(90, axes[index]);
            Quaternion targetRotation = delta * transform.rotation;

            if (changeGravity)
            {
                Gravity = delta * Gravity;
            }

            StartCoroutine(RotateTo(targetRotation));
        }

        IEnumerator RotateTo(Quaternion target)
        {
            isRotating = true;

            Quaternion start = transform.rotation;
            float time = 0f;

            while (time < rotateDuration)
            {
                float t = time / rotateDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                transform.rotation = Quaternion.Slerp(start, target, t);

                time += Time.deltaTime;
                yield return null;
            }

            transform.rotation = target;
            isRotating = false;
        }
    }
}