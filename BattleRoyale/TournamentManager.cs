using MelonLoader;
using UnityEngine;
using System.Collections;
#if MONO
using ScheduleOne.NPCs;
using S1Behaviour = ScheduleOne.NPCs.Behaviour;
#else
using Il2CppScheduleOne.NPCs;
using S1Behaviour = Il2CppScheduleOne.NPCs.Behaviour;
#endif

namespace NPCBattleRoyale.BattleRoyale
{
    /// <summary>
    /// Orchestrates group-stage FFAs and finals bracket using BattleRoyaleManager's combat helpers.
    /// </summary>
    public class TournamentManager
    {
        private readonly BattleRoyaleManager _manager;
        private readonly List<GroupDefinition> _groups;
        private readonly RoundSettings _settings;

        private readonly List<NPC> _currentParticipants = new();
        private readonly List<(string group, NPC winner)> _groupWinners = new();
        private readonly System.Random _rng = new();

        public TournamentManager(BattleRoyaleManager manager, List<GroupDefinition> groups, RoundSettings settings)
        {
            _manager = manager;
            _groups = groups;
            _settings = settings;
        }

        public void RunTournament()
        {
            MelonCoroutines.Start(RunTournamentRoutine());
        }

        /// <summary>
        /// Public helper to run a single free-for-all round with the provided participants.
        /// </summary>
        public void RunSingleFFARoutine(List<NPC> participants)
        {
            if (participants == null || participants.Count < 2)
            {
                MelonLogger.Warning("[BR] Need at least 2 participants to run an FFA round.");
                return;
            }
            MelonCoroutines.Start(RunFFARoutine(participants, w =>
            {
                if (w != null)
                {
                    _manager.ShowWinnerToast(w.fullName);
                }
            }));
        }

        private static void LogParticipants(string label, IList<NPC> list)
        {
            try
            {
                string ids = "";
                for (int i = 0; i < list.Count; i++)
                {
                    var n = list[i];
                    if (n == null) continue;
                    if (ids.Length > 0) ids += ", ";
                    ids += n.ID;
                }
                MelonLogger.Msg($"[BR] {label} ({list.Count}): {ids}");
            }
            catch { }
        }

        private IEnumerator RunTournamentRoutine()
        {
            _groupWinners.Clear();
            _manager.ClearStagedWinners();

            if (_settings.TournamentMode == ETournamentMode.GroupsThenFinals)
            {
                // Group stage → collect winners (or advance top N)
                for (int gi = 0; gi < _settings.SelectedGroups.Count; gi++)
                {
                    var groupName = _settings.SelectedGroups[gi];
                    var def = _groups.Find(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
                    if (def == null) continue;

                    var candidates = FilterNPCsByGroup(def);
                    if (candidates.Count == 0) continue;

                    var picked = PickParticipants(candidates, _settings.ParticipantsPerGroup, _settings.ShuffleParticipants);
                    if (picked.Count < 2)
                    {
                        MelonLogger.Msg($"[BR] Group {groupName} has fewer than 2 participants; skipping.");
                        continue;
                    }

                    LogParticipants($"Group {groupName} picked", picked);

                    // If MaxFFASize set, split into chunks and run sequential FFAs to find a local qualifier pool
                    var localQualifiers = new List<NPC>();
                    if (_settings.MaxFFASize > 0 && picked.Count > _settings.MaxFFASize)
                    {
                        int idx = 0;
                        while (idx < picked.Count)
                        {
                            var chunk = picked.GetRange(idx, Mathf.Min(_settings.MaxFFASize, picked.Count - idx));
                            NPC chunkWinner = null;
                            yield return RunFFARoutine(chunk, w => chunkWinner = w);
                            if (chunkWinner != null) localQualifiers.Add(chunkWinner);
                            idx += _settings.MaxFFASize;
                            if (_settings.InterRoundDelaySeconds > 0f) yield return new WaitForSeconds(_settings.InterRoundDelaySeconds);
                            if (_settings.HealBetweenRounds && chunkWinner != null) HealNPC(chunkWinner);
                        }
                    }
                    else
                    {
                        localQualifiers.AddRange(picked);
                    }

                    var adv = Mathf.Max(1, _settings.AdvancePerGroup);

                    // If advancing one per group: run exactly one FFA among qualifiers to pick the group winner
                    if (adv == 1)
                    {
                        NPC groupWinner = null;
                        if (localQualifiers.Count == 1)
                        {
                            groupWinner = localQualifiers[0];
                        }
                        else
                        {
                            LogParticipants($"Group {groupName} qualifiers", localQualifiers);
                            yield return RunFFARoutine(localQualifiers, w => groupWinner = w);
                        }
                        if (groupWinner != null)
                        {
                            _groupWinners.Add((groupName, groupWinner));
                            _manager.StageWinner(groupWinner, _groupWinners.Count - 1);
                        }
                        continue;
                    }

                    // If advancing multiple per group: run FFAs to select exactly adv winners
                    var pool = new List<NPC>(localQualifiers);
                    var toAdvance = new List<NPC>();
                    while (toAdvance.Count < adv && pool.Count > 0)
                    {
                        LogParticipants($"Group {groupName} selection pool", pool);
                        NPC w = null;
                        yield return RunFFARoutine(pool, x => w = x);
                        if (w != null)
                        {
                            toAdvance.Add(w);
                            pool.Remove(w);
                            if (_settings.HealBetweenRounds) HealNPC(w);
                        }
                        if (_settings.InterRoundDelaySeconds > 0f) yield return new WaitForSeconds(_settings.InterRoundDelaySeconds);
                    }
                    // Record and stage exactly the selected winners
                    for (int i = 0; i < toAdvance.Count; i++)
                    {
                        _groupWinners.Add((groupName, toAdvance[i]));
                        _manager.StageWinner(toAdvance[i], _groupWinners.Count - 1);
                    }
                }
            }
            else
            {
                // Single-elimination across all selected groups
                var everyone = new List<NPC>();
                for (int gi = 0; gi < _settings.SelectedGroups.Count; gi++)
                {
                    var groupName = _settings.SelectedGroups[gi];
                    var def = _groups.Find(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
                    if (def == null) continue;
                    everyone.AddRange(PickParticipants(FilterNPCsByGroup(def), _settings.ParticipantsPerGroup, _settings.ShuffleParticipants));
                }
                if (everyone.Count >= 2)
                {
                    NPC champion = null;
                    yield return RunBracketRoutine(everyone, w => champion = w);
                    if (champion != null)
                    {
                        MelonLogger.Msg($"[BR] Tournament complete. Champion: {champion.fullName}");
                        _manager.StageWinner(champion, 0);
                    }
                }
                else
                {
                    MelonLogger.Msg("[BR] Not enough participants for single-elimination mode.");
                }
                yield break;
            }

            // Finals: single FFA among group winners
            if (_groupWinners.Count >= 2)
            {
                var finalists = new List<NPC>();
                for (int i = 0; i < _groupWinners.Count; i++) finalists.Add(_groupWinners[i].winner);
                LogParticipants("Finalists", finalists);

                NPC champion = null;
                yield return RunFFARoutine(finalists, w => champion = w);
                if (champion != null)
                {
                    MelonLogger.Msg($"[BR] Tournament complete. Champion: {champion.fullName}");
                    // Stage champion distinctly
                    _manager.StageWinner(champion, 0);
                    _manager.ShowWinnerToast(champion.fullName);
                }
            }
            else
            {
                MelonLogger.Msg("[BR] Not enough group winners to run finals.");
            }
        }

        private List<NPC> FilterNPCsByGroup(GroupDefinition def)
        {
            var result = new List<NPC>();
            // NPCManager.NPCRegistry is authoritative; also ensure NPCs are spawned and have movement & health
            foreach (var npc in NPCManager.NPCRegistry)
            {
                if (npc == null) continue;
                if (_manager.IsIgnored(npc.ID)) continue;
                if (npc.Movement == null || npc.Health == null) continue;
                if (!npc.Health.IsDead && GroupConfig.IsMember(def, npc))
                {
                    result.Add(npc);
                }
            }
            return result;
        }

        private List<NPC> PickParticipants(List<NPC> candidates, int count, bool shuffle)
        {
            var list = new List<NPC>(candidates);
            if (shuffle) Shuffle(list);
            if (count > 0 && list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        private NPC RunFreeForAll(List<NPC> participants)
        {
            MelonLogger.Msg($"[BR] Starting FFA with {participants.Count} participants");
            LogParticipants("FFA participants", participants);

            _currentParticipants.Clear();
            _currentParticipants.AddRange(participants);

            // Ensure we're in a clean state
            if (_manager.State != RoundState.Idle)
            {
                _manager.StopRound();
            }

            // Set arena
            _manager.ActiveArenaIndex = Mathf.Clamp(_settings.ArenaIndex, 0, _manager.Arenas.Length - 1);
            var arena = _manager.Arenas[_manager.ActiveArenaIndex];

            // Gather, revive, and subscribe
            _manager.State = RoundState.Gathering;
            _manager.UnsubscribeAll();

            for (int i = 0; i < participants.Count; i++)
            {
                var npc = participants[i];
                if (npc == null) continue;

                // Revive if needed
                if (npc.Health.IsDead || npc.Health.IsKnockedOut)
                {
                    npc.Health.Revive();
                }

                // Disable schedule and ScheduleBehaviour components
                if (npc.Behaviour != null && npc.Behaviour.ScheduleManager != null)
                {
                    npc.Behaviour.ScheduleManager.DisableSchedule();
                    var schedBehaviours = npc.GetComponentsInChildren<S1Behaviour.ScheduleBehaviour>(includeInactive: true);
                    for (int s = 0; s < schedBehaviours.Length; s++)
                    {
                        schedBehaviours[s].Disable_Networked(null);
                    }
                    
                    // Clear any existing combat targets to prevent chasing non-participants
                    if (npc.Behaviour.CombatBehaviour != null && npc.Behaviour.CombatBehaviour.Active)
                    {
                        npc.Behaviour.CombatBehaviour.Disable_Networked(null);
                    }
                }

                // Position in arena
                _manager.TeleportToArenaGrid(npc, arena.Center, arena.Radius * 0.6f, i, participants.Count);
                _manager.SubscribeElimination(npc);
            }

            // Start fighting
            _manager.PairAndAggroAll();
            _manager.SetGatesActive(true);
            _manager.State = RoundState.Fighting;

            MelonLogger.Msg($"[BR] FFA started! Timeout: {_settings.MatchTimeoutSeconds}s");

            // Blocking version used only as fallback if routines aren't used
            float start = Time.time;
            int lastAliveCount = participants.Count;
            while (Time.time - start < _settings.MatchTimeoutSeconds)
            {
                var alive = GetAlive(_currentParticipants);
                if (alive.Count != lastAliveCount)
                {
                    MelonLogger.Msg($"[BR] {alive.Count} participants remaining");
                    lastAliveCount = alive.Count;
                }
                if (alive.Count <= 1)
                {
                    var winner = alive.Count == 1 ? alive[0] : null;
                    MelonLogger.Msg($"[BR] FFA complete! Winner: {(winner != null ? winner.fullName : "None")}");
                    _manager.StopRound();
                    return winner;
                }
            }

            // Timeout: pick highest health alive
            var survivors = GetAlive(_currentParticipants);
            NPC best = null;
            float bestHp = -1f;

            for (int i = 0; i < survivors.Count; i++)
            {
                var hp = survivors[i].Health.Health;
                if (hp > bestHp) { bestHp = hp; best = survivors[i]; }
            }

            MelonLogger.Msg($"[BR] FFA timed out! Winner by health: {(best != null ? best.fullName : "None")}");
            _manager.StopRound();
            return best;
        }

        private IEnumerator RunFFARoutine(List<NPC> participants, Action<NPC> onComplete)
        {
            _currentParticipants.Clear();
            _currentParticipants.AddRange(participants);
            LogParticipants("FFA participants", participants);

            if (_manager.State != RoundState.Idle)
                _manager.StopRound();

            _manager.ActiveArenaIndex = Mathf.Clamp(_settings.ArenaIndex, 0, _manager.Arenas.Length - 1);
            var arena = _manager.Arenas[_manager.ActiveArenaIndex];

            // Signal manager that a controlled tournament round is running
            _manager.SetExternalControl(true);
            _manager.SetActiveParticipants(_currentParticipants);

            _manager.State = RoundState.Gathering;
            _manager.UnsubscribeAll();
            for (int i = 0; i < participants.Count; i++)
            {
                var npc = participants[i];
                if (npc == null) continue;
                if (npc.Health.IsDead || npc.Health.IsKnockedOut) npc.Health.Revive();
                if (npc.Behaviour != null && npc.Behaviour.ScheduleManager != null)
                {
                    npc.Behaviour.ScheduleManager.DisableSchedule();
                    var schedBehaviours = npc.GetComponentsInChildren<S1Behaviour.ScheduleBehaviour>(includeInactive: true);
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
                _manager.TeleportToArenaGrid(npc, arena.Center, arena.Radius * 0.6f, i, participants.Count);
                _manager.SubscribeElimination(npc);
            }
            // Only pair the active participants to avoid global aggro storms
            _manager.PairAndAggroActiveParticipants();
            _manager.SetGatesActive(true);
            _manager.State = RoundState.Fighting;

            float start = Time.time;
            int lastAliveCount = participants.Count;
            float lastCheckTime = Time.time;
            
            while (Time.time - start < _settings.MatchTimeoutSeconds)
            {
                // Only check every 0.5 seconds to reduce performance impact
                if (Time.time - lastCheckTime >= 0.5f)
                {
                    var alive = GetAlive(_currentParticipants);
                    if (alive.Count != lastAliveCount)
                    {
                        MelonLogger.Msg($"[BR] {alive.Count} participants remaining");
                        lastAliveCount = alive.Count;
                        // Re-pair surviving participants so they keep fighting
                        _manager.SetActiveParticipants(alive);
                        _manager.PairAndAggroActiveParticipants();
                    }
                    if (alive.Count <= 1)
                    {
                        var winner = alive.Count == 1 ? alive[0] : null;
                        _manager.StopRound();
                        onComplete?.Invoke(winner);
                        yield break;
                    }
                    lastCheckTime = Time.time;
                }
                yield return new WaitForSeconds(0.1f); // Wait 0.1 seconds between checks instead of every frame
            }

            // Timeout
            var survivors = GetAlive(_currentParticipants);
            NPC best = null;
            float bestHp = -1f;
            for (int i = 0; i < survivors.Count; i++)
            {
                var hp = survivors[i].Health.Health;
                if (hp > bestHp) { bestHp = hp; best = survivors[i]; }
            }
            _manager.StopRound();
            _manager.SetExternalControl(false);
            _manager.ClearActiveParticipants();
            onComplete?.Invoke(best);
        }

        private IEnumerator RunBracketRoutine(List<NPC> finalists, Action<NPC> onComplete)
        {
            var queue = new Queue<NPC>(finalists);
            var nextRound = new List<NPC>();
            // Pure single-elimination bracket
            while (queue.Count + nextRound.Count > 1)
            {
                // If current round exhausted, move to next
                if (queue.Count <= 1)
                {
                    queue = new Queue<NPC>(nextRound);
                    nextRound.Clear();
                }

                // If odd participant count, give a bye
                NPC first = queue.Dequeue();
                if (queue.Count == 0)
                {
                    nextRound.Add(first);
                    continue;
                }
                NPC second = queue.Dequeue();

                NPC winner = null;
                yield return RunFFARoutine(new List<NPC> { first, second }, w => winner = w);
                if (winner != null)
                {
                    nextRound.Add(winner);
                    // Stage the winner visibly
                    _manager.StageWinner(winner, nextRound.Count - 1);
                }
                yield return null;
            }
            var champ = (queue.Count == 1 ? queue.Dequeue() : (nextRound.Count == 1 ? nextRound[0] : null));
            onComplete?.Invoke(champ);
        }

        private NPC RunBracket(List<NPC> finalists)
        {
            var list = new List<NPC>(finalists);
            Shuffle(list);
            while (list.Count > 1)
            {
                var a = list[0];
                var b = list[1];
                list.RemoveAt(0);
                list.RemoveAt(0);
                var winner = RunFreeForAll(new List<NPC> { a, b });
                if (winner != null) list.Add(winner);
            }
            return list.Count == 1 ? list[0] : null;
        }

        private static List<NPC> GetAlive(List<NPC> list)
        {
            var result = new List<NPC>();
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (n == null || n.Health == null) continue;
                if (n.Health.IsDead) continue;
                if (n.Health.IsKnockedOut) continue;
                result.Add(n);
            }
            return result;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private void HealNPC(NPC n)
        {
            if (n == null || n.Health == null) return;
            if (n.Health.IsDead || n.Health.IsKnockedOut) n.Health.Revive();
            n.Health.Health = n.Health.MaxHealth;
        }
    }
}


