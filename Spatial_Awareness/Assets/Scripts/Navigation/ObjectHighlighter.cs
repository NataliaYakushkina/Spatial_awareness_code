using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpatialAwareness.SceneGraph;

namespace SpatialAwareness.Navigation
{
    public class ObjectHighlighter : MonoBehaviour
    {
        public static ObjectHighlighter Instance { get; private set; }

        [Header("General")]
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private float highlightDuration = 10f;

        [Header("Edge outline")]
        [Tooltip("Thickness of the outline in world units.")]
        [SerializeField] private float outlineWidth = 0.03f;
        [Tooltip("Push the outline off the picture surface (toward the viewer) so it does not z-fight.")]
        [SerializeField] private float outlineOffset = 0.01f;
        [Tooltip("Rounded corner smoothness of the outline.")]
        [SerializeField] private int cornerVertices = 2;

        [Header("Emissive pulse")]
        [Tooltip("Pulse cycles per second.")]
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float minEmission = 1.5f;
        [SerializeField] private float maxEmission = 6f;

        private readonly Dictionary<string, Coroutine> activeHighlights = new Dictionary<string, Coroutine>();
        private readonly Dictionary<string, GameObject> activeOutlines = new Dictionary<string, GameObject>();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public bool HighlightObject(string objectId)
        {
            var node = SceneGraphManager.Instance?.GetNodeById(objectId);
            if (node == null)
            {
                Debug.LogWarning($"[Highlighter] Object '{objectId}' not found!");
                return false;
            }

            if (activeHighlights.ContainsKey(objectId))
                ClearHighlight(objectId);

            var renderers = node.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[Highlighter] Object '{objectId}' has no renderers to highlight.");
                return false;
            }

            // Outline the largest flat face (the picture itself, not a thin frame/prop).
            var target = PickLargestFace(renderers);
            var outline = CreateOutline(target);
            if (outline == null)
            {
                Debug.LogWarning($"[Highlighter] Could not build an outline for '{objectId}'.");
                return false;
            }
            activeOutlines[objectId] = outline;

            Debug.Log($"[Highlighter] Highlighted (edge): {objectId}");
            activeHighlights[objectId] = StartCoroutine(PulseRoutine(objectId, highlightDuration));
            return true;
        }

        public void ClearHighlight(string objectId)
        {
            if (activeHighlights.TryGetValue(objectId, out var coroutine))
            {
                if (coroutine != null) StopCoroutine(coroutine);
                activeHighlights.Remove(objectId);
            }

            if (activeOutlines.TryGetValue(objectId, out var go))
            {
                if (go != null)
                {
                    var lr = go.GetComponent<LineRenderer>();
                    if (lr != null && lr.material != null) Destroy(lr.material);
                    Destroy(go);
                }
                activeOutlines.Remove(objectId);
            }
        }

        public void ClearAllHighlights()
        {
            foreach (var id in new List<string>(activeHighlights.Keys))
                ClearHighlight(id);
        }

        // Pick the renderer whose local bounds have the largest face area (its two biggest extents).
        private Renderer PickLargestFace(Renderer[] renderers)
        {
            Renderer best = renderers[0];
            float bestArea = -1f;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                Vector3 e = r.localBounds.extents;
                float[] s = { e.x, e.y, e.z };
                System.Array.Sort(s); // ascending -> two largest are s[1], s[2]
                float area = s[1] * s[2];
                if (area > bestArea) { bestArea = area; best = r; }
            }
            return best;
        }

        // Build a rectangular outline that traces the four edges of the renderer's front face.
        private GameObject CreateOutline(Renderer r)
        {
            Bounds lb = r.localBounds;
            Vector3 c = lb.center, e = lb.extents;

            // The thinnest local axis is the picture's normal; the other two span its face.
            int thin = 0; float min = e.x;
            if (e.y < min) { min = e.y; thin = 1; }
            if (e.z < min) { min = e.z; thin = 2; }
            int a = (thin + 1) % 3, b = (thin + 2) % 3;

            var m = r.localToWorldMatrix;

            // Push the outline toward the viewer so it sits in front of the surface, not inside the wall.
            float sign = 1f;
            if (Camera.main != null)
            {
                Vector3 worldCenter = m.MultiplyPoint3x4(c);
                Vector3 worldNormal = m.MultiplyVector(AxisVec(thin));
                if (Vector3.Dot(worldNormal, Camera.main.transform.position - worldCenter) < 0f)
                    sign = -1f;
            }

            Vector3 ua = AxisVec(a) * e[a];
            Vector3 ub = AxisVec(b) * e[b];
            Vector3 vn = AxisVec(thin) * sign * (e[thin] + outlineOffset);

            Vector3[] local =
            {
                c + vn + ua + ub,
                c + vn + ua - ub,
                c + vn - ua - ub,
                c + vn - ua + ub,
            };
            Vector3[] world = local.Select(p => m.MultiplyPoint3x4(p)).ToArray();

            var go = new GameObject("HighlightOutline");
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = world.Length;
            lr.SetPositions(world);
            lr.widthMultiplier = outlineWidth;
            lr.numCornerVertices = cornerVertices;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            lr.material = new Material(shader);
            return go;
        }

        private static Vector3 AxisVec(int i) => i == 0 ? Vector3.right : (i == 1 ? Vector3.up : Vector3.forward);

        private IEnumerator PulseRoutine(string objectId, float duration)
        {
            activeOutlines.TryGetValue(objectId, out var go);
            var lr = go != null ? go.GetComponent<LineRenderer>() : null;
            var mat = lr != null ? lr.material : null;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float wave = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f; // 0..1
                float intensity = Mathf.Lerp(minEmission, maxEmission, wave);
                Color col = highlightColor * intensity; // HDR -> feeds Bloom

                if (mat != null) mat.SetColor(BaseColorId, col);
                if (lr != null) { lr.startColor = col; lr.endColor = col; }

                elapsed += Time.deltaTime;
                yield return null;
            }

            ClearHighlight(objectId);
        }
    }
}
