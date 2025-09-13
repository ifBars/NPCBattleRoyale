using MelonLoader;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.UI;
#if MONO
using ScheduleOne.NPCs;
using ScheduleOne.DevUtilities;
using ScheduleOne.Interaction;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
#else
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Police;
#endif

namespace NPCBattleRoyale.BattleRoyale
{
    public class BattleRoyaleManager : MonoBehaviour
    {
        public static BattleRoyaleManager Instance { get; private set; }


        public ArenaDefinition[] Arenas = new ArenaDefinition[2]
        {
            new()
            {
                Name = "Police Station",
                Center = new Vector3(19.265f, 1.065f, 37.653f),
                Radius = 7f,
                GateLocalPositions = new [] { new Vector3(-2.2284f, 0.0f, 8.1987f), new Vector3(7.0373f, 0.0f, -1.0304f) },
                GateLocalEulerAngles = new [] { new Vector3(0f, 0f, 0f), new Vector3(0f, 90f, 0f) },
                PanelLocalOffset = new Vector3(8f, 0f, -6f),
                PanelLocalEulerAngles = new Vector3(0f, 90f, 0f),
            },
            new()
            {
                Name = "Chemical Station",
                Center = new Vector3(-109.745f, -2.935f, 92.271f),
                Radius = 10f,
                GateLocalPositions = new [] { new Vector3(-2.8767f, 0.0f, -7.1238f), new Vector3(7.0f, 0.0f, 4.0f) },
                GateLocalEulerAngles = new [] { new Vector3(0f, 20f, 0f), new Vector3(0f, 90f, 0f) },
                PanelLocalOffset = new Vector3(0.4666f, 3.4786f, 10.2059f)
            }
        };

        public int ActiveArenaIndex = 1;
        public RoundState State { get; set; } = RoundState.Idle;
        public string[] IgnoredNPCIDs = new string[]
        {
            "shirley_watts", "thomas_benzies", "uncle_nelson", "cartelgoon3", "cartelgoon1", "cartelgoon", "cartelgoon2", "cartelgoon4", "cartelgoon5", "igor_romanovich_door", "salvador_moreno"
        };

        private readonly List<NPC> _roundNPCs = new();
        private readonly List<GameObject> _gates = new();
        private GameObject _panelRoot;
        private readonly Dictionary<NPC, UnityAction> _deathHandlers = new();
        private readonly Dictionary<NPC, UnityAction> _koHandlers = new();
        private readonly List<NPC> _stagedWinners = new();
        private readonly HashSet<NPC> _activeParticipants = new();
        public bool ExternalControlActive { get; private set; }

        // Winner toast UI
        private Canvas _toastCanvas;
        private Text _toastText;
        private Coroutine _toastRoutine;

        /// <summary>
        /// Teleport the player near the arena center with a safe offset.
        /// </summary>
        public void TeleportPlayerToArena()
        {
            try
            {
                var pm = PlayerSingleton<PlayerMovement>.Instance;
                if (pm == null) return;
                var arena = Arenas[ActiveArenaIndex];
                pm.Teleport(arena.Center);
            }
            catch { }
        }

        /// <summary>
        /// Teleport the player to the in-world control panel.
        /// </summary>
        public void TeleportPlayerToControlPanel()
        {
            try
            {
                var pm = PlayerSingleton<PlayerMovement>.Instance;
                if (pm == null) return;
                if (_panelRoot == null)
                {
                    TrySpawnEnvironmentForActiveArena();
                }
                if (_panelRoot == null) return;
                var target = _panelRoot.transform.position + new Vector3(0f, 0.5f, -1.2f);
                pm.Teleport(target);
                Vector3 lookDir = _panelRoot.transform.position - target;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    var rot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    pm.LerpPlayerRotation(rot, 0.1f);
                }
            }
            catch { }
        }

        private static Transform GetPlayerRootTransform()
        {
            // Best-effort: try tagged player, then camera root
            var go = GameObject.FindWithTag("Player");
            if (go != null) return go.transform;
            if (Camera.main != null)
            {
                var tr = Camera.main.transform;
                while (tr.parent != null) tr = tr.parent;
                return tr;
            }
            // Fallback: any character controller in scene
            var cc = GameObject.FindObjectOfType<CharacterController>();
            return cc != null ? cc.transform : null;
        }

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
            EnsureToastUI();
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
                if (!ShouldIncludePolice() && IsPolice(npc)) continue;
                if (npc.Health.IsDead || npc.Health.IsKnockedOut)
                {
                    npc.Health.Revive();
                }
                if (npc.Behaviour != null && npc.Behaviour.ScheduleManager != null)
                {
                    npc.Behaviour.ScheduleManager.DisableSchedule();
                    // Also disable any ScheduleBehaviour so it cannot re-enable schedules during the round
                    var schedBehaviours = npc.GetComponentsInChildren<ScheduleOne.NPCs.Behaviour.ScheduleBehaviour>(includeInactive: true);
                    for (int s = 0; s < schedBehaviours.Length; s++)
                    {
                        schedBehaviours[s].Disable_Networked(null);
                    }
                    
                    // Clear any existing combat targets to prevent chasing non-participants
                    if (npc.Behaviour.CombatBehaviour != null && npc.Behaviour.CombatBehaviour.Active)
                    {
                        npc.Behaviour.CombatBehaviour.Disable_Networked(null);
                    }
                    // Ensure NPCs leave buildings/vehicles before warping to arena
                    if (npc.isInBuilding)
                    {
                        npc.ExitBuilding();
                    }
                    if (npc.IsInVehicle)
                    {
                        npc.ExitVehicle();
                    }
                }
                TeleportToArenaGrid(npc, arena.Center, arena.Radius * 0.6f, i, all.Count);
                SubscribeElimination(npc);
            }
        }

        public void PairAndAggroAll()
        {
            var list = new List<NPC>(GetAllNPCsAlive());
            try
            {
                string ids = "";
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;
                    if (ids.Length > 0) ids += ", ";
                    ids += list[i].ID;
                }
                MelonLogger.Msg($"[BR] PairAndAggroAll participants ({list.Count}): {ids}");
            }
            catch { }
            for (int i = 0; i < list.Count; i++)
            {
                var self = list[i];
                var target = list[(i + 1) % list.Count];
                if (IsAllowedCombatant(self) && IsAllowedCombatTarget(target))
                {
                    ForceCombat(self, target);
                }
            }
        }

        /// <summary>
        /// Set the explicit list of active participants for pairing/aggression.
        /// </summary>
        public void SetActiveParticipants(IEnumerable<NPC> participants)
        {
            _activeParticipants.Clear();
            foreach (var p in participants)
            {
                if (p != null) _activeParticipants.Add(p);
            }
        }

        public void ClearActiveParticipants() => _activeParticipants.Clear();

        public void SetExternalControl(bool active) => ExternalControlActive = active;

        public List<NPC> GetActiveParticipantsAlive()
        {
            var alive = new List<NPC>();
            foreach (var p in _activeParticipants)
            {
                if (p == null || p.Health == null) continue;
                if (p.Health.IsDead || p.Health.IsKnockedOut) continue;
                alive.Add(p);
            }
            return alive;
        }

        /// <summary>
        /// Pair and aggro only within the explicitly set active participants.
        /// </summary>
        public void PairAndAggroActiveParticipants()
        {
            var list = GetActiveParticipantsAlive();
            try
            {
                string ids = "";
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null) continue;
                    if (ids.Length > 0) ids += ", ";
                    ids += list[i].ID;
                }
                MelonLogger.Msg($"[BR] PairAndAggroActiveParticipants ({list.Count}): {ids}");
            }
            catch { }
            for (int i = 0; i < list.Count; i++)
            {
                var self = list[i];
                var target = list[(i + 1) % Mathf.Max(1, list.Count)];
                if (IsAllowedCombatant(self) && IsAllowedCombatTarget(target))
                {
                    ForceCombat(self, target);
                }
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
                if (!ShouldIncludePolice() && IsPolice(npc)) continue;
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
                if (!ShouldIncludePolice() && IsPolice(npc)) continue;
                if (!IsIgnored(npc.ID)) result.Add(npc);
            }
            return result;
        }

        public bool IsIgnored(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < IgnoredNPCIDs.Length; i++)
            {
                // Use both exact match and substring match for better detection
                if (string.Equals(IgnoredNPCIDs[i], id, StringComparison.OrdinalIgnoreCase) ||
                    id.IndexOf(IgnoredNPCIDs[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsPolice(NPC npc)
        {
            try { return npc is PoliceOfficer; } catch { return false; }
        }

        public bool IsAllowedCombatant(NPC npc)
        {
            if (npc == null || npc.Health == null) return false;
            if (npc.Health.IsDead || npc.Health.IsKnockedOut) return false;
            if (!ShouldIncludePolice() && IsPolice(npc)) return false;
            if (IsIgnored(npc.ID)) return false;
            if (ExternalControlActive) return _activeParticipants.Contains(npc);
            return true;
        }
        private bool ShouldIncludePolice()
        {
            try { return ConfigPanel.Instance != null && ConfigPanel.Instance.CurrentSettings.IncludePolice; } catch { return false; }
        }

        public bool IsAllowedCombatTarget(NPC npc) => IsAllowedCombatant(npc);

        public void TeleportToArenaGrid(NPC npc, Vector3 center, float radius, int index, int total)
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

        public void ForceCombat(NPC a, NPC b)
        {
            if (a == null || b == null) return;
            try
            {
                // If both are already targeting each other in combat, do nothing to avoid stutters
                if (IsAlreadyTargeting(a, b) && IsAlreadyTargeting(b, a))
                {
                    return;
                }

                // Comprehensive behavior management for both NPCs
                PrepareNPCForCombat(a, b);
                PrepareNPCForCombat(b, a);
                
                // Ensure CombatBehaviour references exist and are properly configured
                EnsureCombatBehaviourAssigned(a);
                EnsureCombatBehaviourAssigned(b);
                ConfigureCombat(a);
                ConfigureCombat(b);
                
                // Set up mutual targeting (guard against invalid targets like police or non-participants)
                if (a.Behaviour.CombatBehaviour != null)
                {
                    // Only retarget if not already targeting and target is allowed
                    if (!IsAlreadyTargeting(a, b) && IsAllowedCombatTarget(b))
                    {
                        a.Behaviour.CombatBehaviour.SetTarget(null, b.NetworkObject);
                    }
                    if (!a.Behaviour.CombatBehaviour.Enabled)
                    {
                        a.Behaviour.CombatBehaviour.Enable_Networked(null);
                    }
                }
                else
                {
                    MelonLogger.Warning($"[BR] CombatBehaviour missing for {a.fullName} ({a.ID})");
                }
                if (b.Behaviour.CombatBehaviour != null)
                {
                    if (!IsAlreadyTargeting(b, a) && IsAllowedCombatTarget(a))
                    {
                        b.Behaviour.CombatBehaviour.SetTarget(null, a.NetworkObject);
                    }
                    if (!b.Behaviour.CombatBehaviour.Enabled)
                    {
                        b.Behaviour.CombatBehaviour.Enable_Networked(null);
                    }
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

        private static bool IsAlreadyTargeting(NPC self, NPC target)
        {
            try
            {
                var cb = self?.Behaviour?.CombatBehaviour;
                if (cb == null) return false;
                if (!cb.Enabled && !cb.Active) return false;
                var current = cb.Target;
                if (current == null) return false;
                return current.NetworkObject == target?.NetworkObject;
            }
            catch { return false; }
        }

        /// <summary>
        /// Move winner to a safe staging area around the arena and freeze them for later rounds.
        /// </summary>
        public void StageWinner(NPC npc, int slotIndex)
        {
            if (npc == null) return;
            try
            {
                // Ensure no combat and no schedule for staged winners
                if (npc.Behaviour?.CombatBehaviour != null)
                {
                    npc.Behaviour.CombatBehaviour.Disable_Networked(null);
                }
                if (npc.Behaviour?.ScheduleManager != null)
                {
                    npc.Behaviour.ScheduleManager.DisableSchedule();
                }
                // Stop movement
                npc.Movement?.Stop();
                // Place near arena but outside ring
                var arena = Arenas[ActiveArenaIndex];
                var pos = GetStagingPosition(arena, slotIndex);
                npc.Movement?.Warp(pos);
                npc.Movement?.FacePoint(arena.Center);
                if (!_stagedWinners.Contains(npc)) _stagedWinners.Add(npc);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[BR] StageWinner error: {ex}");
            }
        }

        /// <summary>
        /// Compute a staging position around the arena for winners to wait safely.
        /// </summary>
        public Vector3 GetStagingPosition(ArenaDefinition arena, int index)
        {
            float radius = Mathf.Max(3f, arena.Radius * 1.8f);
            float angle = (index % 12) / 12f * Mathf.PI * 2f; // 12 evenly spaced slots
            Vector3 pos = arena.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (NavMeshUtility.SamplePosition(pos, out var hit, 3f, -1))
            {
                return hit.position;
            }
            return pos;
        }

        /// <summary>
        /// Returns true if an NPC is currently staged.
        /// </summary>
        public bool IsStaged(NPC npc) => npc != null && _stagedWinners.Contains(npc);

        /// <summary>
        /// Clears staged winners list (used when tournament completes).
        /// </summary>
        public void ClearStagedWinners() => _stagedWinners.Clear();

        private void PrepareNPCForCombat(NPC npc, NPC desiredTarget = null)
        {
            if (npc == null || npc.Behaviour == null) return;
            
            // Avoid forcibly disabling combat if already active and aimed at the desired target
            if (npc.Behaviour.CombatBehaviour != null && npc.Behaviour.CombatBehaviour.Active)
            {
                if (desiredTarget != null)
                {
                    if (!IsAlreadyTargeting(npc, desiredTarget))
                    {
                        npc.Behaviour.CombatBehaviour.Disable_Networked(null);
                    }
                }
            }
            
            // Disable problematic behaviors that could interfere with combat
            SafeDisable(npc.Behaviour.FleeBehaviour);
            SafeDisable(npc.Behaviour.CallPoliceBehaviour);
            SafeDisable(npc.Behaviour.StationaryBehaviour);
            npc.Behaviour.ScheduleManager.ScheduleEnabled = false;
            npc.Behaviour.ScheduleManager.DisableSchedule();
            // Explicitly disable any ScheduleBehaviour components so they cannot re-enable schedules
            var scheduleBehaviours = npc.GetComponentsInChildren<ScheduleOne.NPCs.Behaviour.ScheduleBehaviour>(includeInactive: true);
            for (int i = 0; i < scheduleBehaviours.Length; i++)
            {
                SafeDisable(scheduleBehaviours[i]);
            }
            SafeDisable(npc.Behaviour.DeadBehaviour);
            SafeDisable(npc.Behaviour.UnconsciousBehaviour);
            SafeDisable(npc.Behaviour.RagdollBehaviour);
            SafeDisable(npc.Behaviour.CoweringBehaviour);
            SafeDisable(npc.Behaviour.FaceTargetBehaviour);
            SafeDisable(npc.Behaviour.SummonBehaviour);
            SafeDisable(npc.Behaviour.GenericDialogueBehaviour);
            SafeDisable(npc.Behaviour.RequestProductBehaviour);
            SafeDisable(npc.Behaviour.ConsumeProductBehaviour);
            
            // Disable schedule to prevent interruptions
            if (npc.Behaviour.ScheduleManager != null)
            {
                npc.Behaviour.ScheduleManager.DisableSchedule();
            }
            // Increase aggression to ensure focus on fighting
            npc.OverrideAggression(1f);
            // Reduce awareness-based distractions
            npc.Awareness?.SetAwarenessActive(false);
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
                
                // Safely disable combat behavior
                try
                {
                    if (npc.Behaviour.CombatBehaviour != null)
                    {
                        npc.Behaviour.CombatBehaviour.Disable_Networked(null);
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[BR] Error disabling combat for {npc.fullName}: {ex}");
                }
                
                // Re-enable schedules to restore normal behavior
                try
                {
                    if (npc.Behaviour.ScheduleManager != null)
                    {
                        npc.Behaviour.ScheduleManager.EnableSchedule();
                        // Re-enable ScheduleBehaviour components that were disabled at round start
                        var schedBehaviours = npc.GetComponentsInChildren<ScheduleOne.NPCs.Behaviour.ScheduleBehaviour>(includeInactive: true);
                        for (int s = 0; s < schedBehaviours.Length; s++)
                        {
                            schedBehaviours[s].Enable_Networked(null);
                        }
                    }
                    npc.ResetAggression();
                    npc.Awareness?.SetAwarenessActive(true);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[BR] Error re-enabling schedule for {npc.fullName}: {ex}");
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

        public void SubscribeElimination(NPC npc)
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

        public void UnsubscribeAll()
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
            // When a tournament controls the flow, do not auto re-pair here
            if (ExternalControlActive) return;
            var alive = GetAllNPCsAlive();
            if (alive.Count <= 1)
            {
                var winnerName = (alive.Count == 1 ? alive[0].fullName : "None");
                MelonLogger.Msg("[BR] Round complete. Winner: " + winnerName);
                StopRound();
                if (alive.Count == 1) ShowWinnerToast(winnerName);
                return;
            }
            
            // Only re-pair survivors if needed, not on every elimination
            // This reduces performance impact significantly
            if (alive.Count > 2 && alive.Count % 3 == 0) // Re-pair every 3rd elimination or when few remain
            {
                MelonLogger.Msg($"[BR] Re-pairing {alive.Count} survivors");
                PairAndAggroAll();
            }
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

        private void EnsureToastUI()
        {
            if (_toastCanvas != null && _toastText != null) return;

            var canvasGO = new GameObject("BR_WinnerToast_Canvas");
            DontDestroyOnLoad(canvasGO);
            _toastCanvas = canvasGO.AddComponent<Canvas>();
            _toastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var textGO = new GameObject("BR_WinnerToast_Text");
            textGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            _toastText = textGO.AddComponent<Text>();
            _toastText.alignment = TextAnchor.MiddleCenter;
            _toastText.color = new Color(1f, 0.95f, 0.2f, 1f);
            _toastText.fontSize = 36;
            _toastText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _toastText.text = string.Empty;
            var rt = (RectTransform)textGO.transform;
            rt.anchorMin = new Vector2(0.5f, 0.9f);
            rt.anchorMax = new Vector2(0.5f, 0.9f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 80f);

            _toastCanvas.gameObject.SetActive(false);
        }

        public void ShowWinnerToast(string winnerName, float seconds = 5f)
        {
            EnsureToastUI();
            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
                _toastRoutine = null;
            }
            _toastText.text = string.IsNullOrEmpty(winnerName) ? "Winner: None" : $"Winner: {winnerName}";
            _toastCanvas.gameObject.SetActive(true);
            _toastRoutine = StartCoroutine(HideToastAfter(seconds));
        }

        private System.Collections.IEnumerator HideToastAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (_toastCanvas != null)
            {
                _toastCanvas.gameObject.SetActive(false);
            }
            _toastRoutine = null;
        }

        private void SpawnGates(ArenaDefinition arena)
        {
            for (int i = 0; i < arena.GateLocalPositions.Length; i++)
            {
                var worldPos = arena.Center + arena.GateLocalPositions[i];
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"BR_Gate_{i}";
                go.transform.position = worldPos;
                // Apply optional rotation from arena definition
                if (i < arena.GateLocalEulerAngles.Length)
                {
                    go.transform.rotation = Quaternion.Euler(arena.GateLocalEulerAngles[i]);
                }
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

        public void SetGatesActive(bool active)
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
            _panelRoot.transform.rotation = Quaternion.Euler(arena.PanelLocalEulerAngles);
            DontDestroyOnLoad(_panelRoot);

            // Trimmed controls: remove direct Start Round. Control flows from GUI now.
            CreateButton(_panelRoot.transform, new Vector3(0f, 0.5f, 0f), "Aggro All", PairAndAggroAll);
            CreateButton(_panelRoot.transform, new Vector3(1.25f, 0.5f, 0f), "Toggle Gates", ToggleGates);
            CreateButton(_panelRoot.transform, new Vector3(2.5f, 0.5f, 0f), "Stop/Reset", StopRound);
            CreateButton(_panelRoot.transform, new Vector3(3.75f, 0.5f, 0f), "Config", () =>
            {
                try { ConfigPanel.Toggle(); }
                catch (Exception ex) { MelonLogger.Warning($"[BR] Control action error (Config): {ex}"); }
            });
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


