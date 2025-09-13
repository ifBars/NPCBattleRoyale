using System;
using UnityEngine;

namespace NPCBattleRoyale.BattleRoyale
{
    [Serializable]
    public class ArenaDefinition
    {
        public string Name = "Arena";
        public Vector3 Center = new Vector3(0f, 0f, 0f);
        public float Radius = 5f;
        public Vector3[] GateLocalPositions = new Vector3[0];
        // Per-gate rotation (Euler degrees). If fewer entries than gates, remaining use zero rotation.
        public Vector3[] GateLocalEulerAngles = new Vector3[0];
        public Vector3 PanelLocalOffset = new Vector3(4f, 0f, -4f);
        // Optional rotation for the control panel (Euler degrees)
        public Vector3 PanelLocalEulerAngles = Vector3.zero;
    }
}

