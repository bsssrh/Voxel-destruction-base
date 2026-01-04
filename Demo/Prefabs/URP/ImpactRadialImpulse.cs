using UnityEngine;

public class ImpactRadialImpulse : MonoBehaviour
{
    [Header("Impulse")]
    [Min(0f)] public float radius = 2.5f;

    [Tooltip("Force for all objects EXCEPT HoldObj layer.")]
    [Min(0f)] public float force = 12f;

    [Header("HoldObj Override")]
    [Tooltip("Only objects on this layer get holdObjForce instead of force.")]
    public string holdObjLayerName = "HoldObj";

    [Min(0f)] public float holdObjForce = 10f;

    [Header("Falloff")]
    [Tooltip("Higher = faster falloff (more concentrated near center).")]
    [Min(0.01f)] public float falloff = 3f;

    [Header("Filtering")]
    [Min(1)] public int maxHits = 64;
    public LayerMask layers = ~0;
    public bool affectTriggers = false;
    public bool ignoreKinematic = true;

    [Header("Force Mode")]
    public ForceMode forceMode = ForceMode.Impulse;

    [Header("Cleanup")]
    public bool destroyComponentAfter = true;
    public bool destroyGameObjectAfter = false;

    private static Collider[] _cols = new Collider[256];
    private int _holdObjLayer = -1;
    private int _holdObjMaskBit = 0;

    private void Awake()
    {
        _holdObjLayer = LayerMask.NameToLayer(holdObjLayerName);
        _holdObjMaskBit = (_holdObjLayer >= 0) ? (1 << _holdObjLayer) : 0;
    }

    private void Start()
    {
        ApplyOnce();
    }

    private void ApplyOnce()
    {
        if (radius <= 0f) { Cleanup(); return; }
        if (force <= 0f && holdObjForce <= 0f) { Cleanup(); return; }

        Vector3 center = transform.position;

        if (_cols.Length < maxHits)
            _cols = new Collider[Mathf.NextPowerOfTwo(maxHits)];

        var q = affectTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        int count = Physics.OverlapSphereNonAlloc(center, radius, _cols, layers, q);
        if (count <= 0) { Cleanup(); return; }
        if (count > maxHits) count = maxHits;

        float invR = 1f / radius;

        for (int i = 0; i < count; i++)
        {
            Collider c = _cols[i];
            if (!c) continue;

            Rigidbody rb = c.attachedRigidbody;
            if (!rb) continue;
            if (ignoreKinematic && rb.isKinematic) continue;

            // Choose force based on layer (HoldObj override)
            float baseForce = force;
            if (_holdObjLayer >= 0)
            {
                int otherLayerBit = (1 << c.gameObject.layer);
                if ((otherLayerBit & _holdObjMaskBit) != 0)
                    baseForce = holdObjForce;
            }

            if (baseForce <= 0f) continue;

            Vector3 p = c.ClosestPoint(center);
            Vector3 dir = (p - center);
            float d = dir.magnitude;

            if (d < 0.0001f)
            {
                dir = (rb.worldCenterOfMass - center);
                d = dir.magnitude;
                if (d < 0.0001f) continue;
            }

            dir /= d;

            float t = Mathf.Clamp01(d * invR);
            float k = Mathf.Exp(-falloff * t); // cheap exponential falloff
            float f = baseForce * k;

            rb.AddForce(dir * f, forceMode);
        }

        Cleanup();
    }

    private void Cleanup()
    {
        if (destroyComponentAfter) Destroy(this);
        if (destroyGameObjectAfter) Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
