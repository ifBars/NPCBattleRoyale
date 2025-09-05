using UnityEngine;

namespace NPCBattleRoyale.BattleRoyale
{
    [Serializable]
    public class ArenaDefinition
    {
        public string Name = "Arena";
        public Vector3 Center = new Vector3(0f, 0f, 0f);
        public float Radius = 5f;
        public Vector3[] GateLocalPositions = Array.Empty<Vector3>();
        // Per-gate rotation (Euler degrees). If fewer entries than gates, remaining use zero rotation.
        public Vector3[] GateLocalEulerAngles = Array.Empty<Vector3>();
        public Vector3 PanelLocalOffset = new Vector3(4f, 0f, -4f);
    }
}

