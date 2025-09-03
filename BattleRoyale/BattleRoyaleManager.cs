using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
#if MONO
using ScheduleOne.NPCs;
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Combat;
#else
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Combat;
#endif

namespace NPCBattleRoyale.BattleRoyale
{
    public class BattleRoyaleManager : MonoBehaviour
    {
        public static BattleRoyaleManager Instance { get; private set; }


        public ArenaDefinition[] Arenas = new ArenaDefinition[2]
        {
            new ArenaDefinition
            {
                Name = "Police Station",
                Center = new Vector3(19.265f, 1.065f, 37.653f),
				Radius = 7f,
				GateLocalPositions = new [] { new Vector3(-2.2284f, 0.0f, 8.1987f), new Vector3(7.0373f, 0.0f, -1.0304f) },
				PanelLocalOffset = new Vector3(6f, 0f, -6f)
			},
			new ArenaDefinition
			{
				Name = "Chemical Station",
				Center = new Vector3(-109.745f, -2.935f, 92.271f),
				Radius = 10f,
				GateLocalPositions = new [] { new Vector3(-2.8767f, 0.0f, -7.1238f) },
				GateLocalEulerAngles = new [] { new Vector3(0f, 20f, 0f) },
				PanelLocalOffset = new Vector3(0.4666f, 3.4786f, 10.2059f)
			}
		};

        public int ActiveArenaIndex = 1;
        public RoundState State { get; private set; } = RoundState.Idle;
		public string[] IgnoredNPCIDs = new string[]
		{
            "thomas_benzies", "uncle_nelson", "cartelgoon3", "cartelgoon1", "cartelgoon", "cartelgoon2", "cartelgoon4", "cartelgoon5", "igor_romanovich_door", "officercooper", "officerbailey"
        };

		private readonly List<NPC> _roundNPCs = new List<NPC>();
		private readonly List<GameObject> _gates = new List<GameObject>();
		private GameObject _panelRoot;
		private readonly Dictionary<NPC, UnityAction> _deathHandlers = new Dictionary<NPC, UnityAction>();
		private readonly Dictionary<NPC, UnityAction> _koHandlers = new Dictionary<NPC, UnityAction>();

		private Material CreateVisibleMaterial(Color color)
		{
			Shader s = Shader.Find("Universal Render Pipeline/Lit");
			if (s == null)
			{
				s = Shader.Find("Standard");
			}
			var mat = new Material(s);
			mat.color = color;
			return mat;
		}

		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
			DontDestroyOnLoad(gameObject);
			TrySpawnEnvironmentForActiveArena();
		}

		public void OnNPCStart(NPC npc)
		{
			// Keep a soft registry for round ops
			if (npc != null && !_roundNPCs.Contains(npc))
			{
				_roundNPCs.Add(npc);
			}
		}

		public void SetArena(int index)
		{
			index = Mathf.Clamp(index, 0, Arenas.Length - 1);
			ActiveArenaIndex = index;
			DestroyEnvironment();
			TrySpawnEnvironmentForActiveArena();
		}

		public void StartRound()
		{
			if (State != RoundState.Idle)
			{
				return;
			}
			MelonLogger.Msg($"[BR] Starting round at arena #{ActiveArenaIndex} ({Arenas[ActiveArenaIndex].Name})");
			GatherAndReviveAllToArena();
			PairAndAggroAll();
			SetGatesActive(true);
			State = RoundState.Fighting;
		}

		public void StopRound()
		{
			MelonLogger.Msg("[BR] Stopping round");
			DisableCombatForAll();
			SetGatesActive(false);
			State = RoundState.Idle;
		}

		public void GatherAndReviveAllToArena()
		{
			State = RoundState.Gathering;
			var arena = Arenas[ActiveArenaIndex];
			var all = GetAllNPCs();
			_roundNPCs.Clear();
			_roundNPCs.AddRange(all);
			UnsubscribeAll();
			for (int i = 0; i < all.Count; i++)
			{
				var npc = all[i];
				if (npc == null) continue;
				if (npc.Health.IsDead || npc.Health.IsKnockedOut)
				{
					npc.Health.Revive();
				}
				if (npc.Behaviour != null && npc.Behaviour.ScheduleManager != null)
				{
					npc.Behaviour.ScheduleManager.DisableSchedule();
				}
				TeleportToArenaGrid(npc, arena.Center, arena.Radius * 0.6f, i, all.Count);
				SubscribeElimination(npc);
			}
		}

		public void PairAndAggroAll()
		{
			var list = new List<NPC>(GetAllNPCsAlive());
			for (int i = 0; i < list.Count; i++)
			{
				var self = list[i];
				var target = list[(i + 1) % list.Count];
				ForceCombat(self, target);
			}
		}

		public void ToggleGates()
		{
			bool anyActive = _gates.Exists(g => g != null && g.activeSelf);
			SetGatesActive(!anyActive);
		}

		private List<NPC> GetAllNPCs()
		{
			// Prefer global registry for reliability — filter ignored IDs
			var list = new List<NPC>();
			foreach (var npc in NPCManager.NPCRegistry)
			{
				if (npc == null) continue;
				if (!IsIgnored(npc.ID)) list.Add(npc);
			}
			return list;
		}

		private List<NPC> GetAllNPCsAlive()
		{
			var result = new List<NPC>();
			foreach (var npc in NPCManager.NPCRegistry)
			{
				if (npc == null || npc.Health == null) continue;
				if (npc.Health.IsDead || npc.Health.IsKnockedOut) continue;
				if (!IsIgnored(npc.ID)) result.Add(npc);
			}
			return result;
		}

		private bool IsIgnored(string id)
		{
			if (string.IsNullOrEmpty(id)) return false;
			for (int i = 0; i < IgnoredNPCIDs.Length; i++)
			{
				if (string.Equals(IgnoredNPCIDs[i], id, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		private void TeleportToArenaGrid(NPC npc, Vector3 center, float radius, int index, int total)
		{
			if (npc == null || npc.Movement == null) return;
			// Arrange on concentric ring positions to minimize overlap
			int ring = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(total)));
			float ringRadius = Mathf.Clamp(radius, 3f, radius);
			float angle = (index / (float)Mathf.Max(1, total)) * Mathf.PI * 2f;
			Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
			// Snap to navmesh
			if (NavMeshUtility.SamplePosition(pos, out var hit, 2f, -1))
			{
				npc.Movement.Warp(hit.position);
				npc.Movement.FacePoint(center);
			}
			else
			{
				npc.Movement.Warp(pos);
			}
		}

		private void ForceCombat(NPC a, NPC b)
		{
			if (a == null || b == null) return;
			try
			{
				// Reduce unwanted behaviours targeting player
				if (a.Behaviour != null)
				{
					SafeDisable(a.Behaviour.FleeBehaviour);
					SafeDisable(a.Behaviour.CallPoliceBehaviour);
					SafeDisable(a.Behaviour.StationaryBehaviour);
				}
				if (b.Behaviour != null)
				{
					SafeDisable(b.Behaviour.FleeBehaviour);
					SafeDisable(b.Behaviour.CallPoliceBehaviour);
					SafeDisable(b.Behaviour.StationaryBehaviour);
				}
				// Ensure CombatBehaviour references exist (some NPCs may not have it assigned)
				EnsureCombatBehaviourAssigned(a);
				EnsureCombatBehaviourAssigned(b);
				ConfigureCombat(a);
				ConfigureCombat(b);
				if (a.Behaviour.CombatBehaviour != null)
				{
					a.Behaviour.CombatBehaviour.SetTarget(null, b.NetworkObject);
					a.Behaviour.CombatBehaviour.Enable_Networked(null);
				}
				else
				{
					MelonLogger.Warning($"[BR] CombatBehaviour missing for {a.fullName} ({a.ID})");
				}
				if (b.Behaviour.CombatBehaviour != null)
				{
					b.Behaviour.CombatBehaviour.SetTarget(null, a.NetworkObject);
					b.Behaviour.CombatBehaviour.Enable_Networked(null);
				}
				else
				{
					MelonLogger.Warning($"[BR] CombatBehaviour missing for {b.fullName} ({b.ID})");
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning($"[BR] ForceCombat error: {ex}");
			}
		}

		private void ConfigureCombat(NPC npc)
		{
			var cb = npc?.Behaviour?.CombatBehaviour;
			if (cb == null) return;
			cb.GiveUpAfterSuccessfulHits = 0;
			cb.DefaultSearchTime = 600f;
			cb.GiveUpRange = 9999f;
		}

		private void EnsureCombatBehaviourAssigned(NPC npc)
		{
			if (npc == null || npc.Behaviour == null) return;
			if (npc.Behaviour.CombatBehaviour == null)
			{
				// Attempt to find a CombatBehaviour component in children (include inactive)
				var cbs = npc.GetComponentsInChildren<ScheduleOne.Combat.CombatBehaviour>(includeInactive: true);
				if (cbs != null && cbs.Length > 0)
				{
					npc.Behaviour.CombatBehaviour = cbs[0];
				}
			}
		}

		private void DisableCombatForAll()
		{
			foreach (var npc in GetAllNPCs())
			{
				if (npc == null || npc.Behaviour == null) continue;
				npc.Behaviour.CombatBehaviour.Disable_Networked(null);
				// Optionally re-enable schedules
				if (npc.Behaviour.ScheduleManager != null)
				{
					npc.Behaviour.ScheduleManager.EnableSchedule();
				}
			}
			UnsubscribeAll();
		}

		private void SafeDisable(ScheduleOne.NPCs.Behaviour.Behaviour beh)
		{
			if (beh != null)
			{
				beh.Disable_Networked(null);
			}
		}

		private void TrySpawnEnvironmentForActiveArena()
		{
			var arena = Arenas[ActiveArenaIndex];
			SpawnGates(arena);
			SpawnControlPanel(arena);
		}

		private void SubscribeElimination(NPC npc)
		{
			if (npc == null || npc.Health == null) return;
			if (!_deathHandlers.ContainsKey(npc))
			{
				UnityAction a = () => OnNPCEliminated(npc);
				npc.Health.onDie.AddListener(a);
				_deathHandlers[npc] = a;
			}
			if (!_koHandlers.ContainsKey(npc))
			{
				UnityAction a = () => OnNPCEliminated(npc);
				npc.Health.onKnockedOut.AddListener(a);
				_koHandlers[npc] = a;
			}
		}

		private void UnsubscribeAll()
		{
			foreach (var kv in _deathHandlers)
			{
				if (kv.Key != null && kv.Key.Health != null) kv.Key.Health.onDie.RemoveListener(kv.Value);
			}
			foreach (var kv in _koHandlers)
			{
				if (kv.Key != null && kv.Key.Health != null) kv.Key.Health.onKnockedOut.RemoveListener(kv.Value);
			}
			_deathHandlers.Clear();
			_koHandlers.Clear();
		}

		private void OnNPCEliminated(NPC eliminated)
		{
			if (State != RoundState.Fighting) return;
			var alive = GetAllNPCsAlive();
			if (alive.Count <= 1)
			{
				MelonLogger.Msg("[BR] Round complete. Winner: " + (alive.Count == 1 ? alive[0].fullName : "None"));
				StopRound();
				return;
			}
			// Re-pair and re-aggro all survivors
			PairAndAggroAll();
		}

		private void DestroyEnvironment()
		{
			foreach (var g in _gates)
			{
				if (g != null) Destroy(g);
			}
			_gates.Clear();
			if (_panelRoot != null) Destroy(_panelRoot);
			_panelRoot = null;
		}

		private void SpawnGates(ArenaDefinition arena)
		{
			for (int i = 0; i < arena.GateLocalPositions.Length; i++)
			{
				var worldPos = arena.Center + arena.GateLocalPositions[i];
				var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				go.name = $"BR_Gate_{i}";
				go.transform.position = worldPos;
				go.transform.localScale = new Vector3(8f, 6f, 1.25f);
				var mr = go.GetComponent<MeshRenderer>();
				if (mr != null)
				{
					mr.material = CreateVisibleMaterial(new Color(0.15f, 0.15f, 0.15f, 1f));
				}
				var bc = go.GetComponent<BoxCollider>();
				bc.isTrigger = false;
				var obst = go.AddComponent<NavMeshObstacle>();
				obst.shape = NavMeshObstacleShape.Box;
				obst.size = go.transform.localScale;
				obst.carving = true;
				_gates.Add(go);
			}
		}

		private void SetGatesActive(bool active)
		{
			foreach (var g in _gates)
			{
				if (g == null) continue;
				g.SetActive(active);
			}
		}

		private void SpawnControlPanel(ArenaDefinition arena)
		{
			_panelRoot = new GameObject("BR_ControlPanel");
			_panelRoot.transform.position = arena.Center + arena.PanelLocalOffset;
			_panelRoot.transform.rotation = Quaternion.identity;
			DontDestroyOnLoad(_panelRoot);

			CreateButton(_panelRoot.transform, new Vector3(0f, 0.5f, 0f), "Start Round", StartRound);
			CreateButton(_panelRoot.transform, new Vector3(1.25f, 0.5f, 0f), "Aggro All", PairAndAggroAll);
			CreateButton(_panelRoot.transform, new Vector3(2.5f, 0.5f, 0f), "Toggle Gates", ToggleGates);
			CreateButton(_panelRoot.transform, new Vector3(3.75f, 0.5f, 0f), "Stop/Reset", StopRound);
		}

		private void CreateButton(Transform parent, Vector3 localPos, string label, Action onPress)
		{
			var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = $"BR_Button_{label}";
			go.transform.SetParent(parent);
			go.transform.localPosition = localPos;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
			var mr = go.GetComponent<MeshRenderer>();
			if (mr != null) mr.material = CreateVisibleMaterial(new Color(0.2f, 0.5f, 0.9f, 1f));

			var col = go.GetComponent<BoxCollider>();
			col.isTrigger = false;
			go.layer = LayerMask.NameToLayer("Default");

			var interact = go.AddComponent<InteractableObject>();
			interact.SetMessage(label);
			interact.SetInteractionType(InteractableObject.EInteractionType.Key_Press);
			// Wire up interaction
			UnityAction action = () =>
			{
				try { onPress?.Invoke(); }
				catch (Exception ex) { MelonLogger.Warning($"[BR] Control action error: {ex}"); }
			};
			interact.onInteractStart.AddListener(action);
		}
	}
}


