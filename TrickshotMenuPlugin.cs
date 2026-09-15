using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;

namespace TrickshotMenu
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed class TrickshotMenuPlugin : BaseUnityPlugin
    {
        public const string GUID = "trickshot.howtofish";
        public const string NAME = "Trickshot Menu";
        public const string VERSION = "2.0.0";

        public static TrickshotMenuPlugin Instance { get; private set; }

        public static readonly Color[] BonusColors =
        {
            new Color(0.29f, 0.94f, 1f, 1f),
            new Color(0.68f, 0.55f, 1f, 1f),
            new Color(0.36f, 0.95f, 0.5f, 1f),
            new Color(1f, 0.82f, 0.35f, 1f),
            new Color(0.95f, 0.5f, 0.4f, 1f)
        };

        // ---- config ----
        public static ConfigEntry<bool> AutoTrick;
        public static ConfigEntry<bool> AirborneOnly;
        public static ConfigEntry<bool> AutoJump;
        public static ConfigEntry<bool> OneShotHelper;
        public static ConfigEntry<bool> ForceLastBullet;
        public static ConfigEntry<bool> AimHead;
        public static ConfigEntry<bool> ShowHud;
        public static ConfigEntry<float> SpinSpeed;
        public static ConfigEntry<float> LeadTime;
        public static ConfigEntry<float> RangeLimit;
        public static ConfigEntry<int> DamageAmount;
        public static ConfigEntry<string> WeaponFilter;
        public static ConfigEntry<string> MenuKey;
        public static ConfigEntry<string> ManualKey;
        public static ConfigEntry<string> LogKey;
        public static ConfigEntry<string> DebugKey;
        public static ConfigEntry<float> SpinExtra;
        public static ConfigEntry<int> SpinDirection;
        public static ConfigEntry<float> ComboWindow;
        public static ConfigEntry<bool> SpinFx;
        public static ConfigEntry<bool> AlwaysWinRoulette;
        public static ConfigEntry<string> SlotForce;
        public static ConfigEntry<bool> SlotFreeSpins;

        // ---- cached reflection ----
        private static MethodInfo _weaponShoot;
        private static FieldInfo _ammoField;
        private static FieldInfo _reloadField;
        private static FieldInfo _queueReloadField;
        private static FieldInfo _camRotField;
        private static FieldInfo _lcIsSpinningField;
        private static FieldInfo _lcIsPlayingField;
        private static FieldInfo _lcCurColorField;
        private static MethodInfo _lcGetColorMethod;
        private static FieldInfo _casinoCurBetColorField;

        // ---- state ----
        private Creature _lineCandidate;
        private Creature _target;
        private float _targetSince = float.PositiveInfinity;
        private Creature _lastFish;
        private float _lastFinalMult = 1f;
        private float _lastTrickTime = -10f;
        private float _nextScanTime;
        private Coroutine _trickRoutine;
        private float _spinFrac;
        private bool _menuVisible = true;
        private bool _logVisible;
        private string _status = "Idle";

        private bool _cursorManaged;
        private CursorLockMode _prevCursorLock;
        private bool _prevCursorVisible;

        private string _rebindTarget = "";
        private int _rebindStartFrame = -10;
        private bool _guideVisible;
        private float _guideAnim;
        private SlotMachine _slotMachine;
        private float _nextSlotSpinTime;

        private int _tricksFired;
        private int _killsConfirmed;
        private int _combo;
        private int _bestCombo;
        private float _bestMult = 1f;
        private double _estEarned;
        private float _comboUntil;
        private float _lastShotTime = -10f;
        private float _comboFlashTime = -10f;
        private float _menuAnim;
        private Creature _pendingKill;
        public string LastBonusSummary { get; private set; } = "ready";
        public Vector2? TargetScreenPos;
        public float TargetDist;
        public string TargetName => _target != null ? _target.GetName() : "";
        public float LastMult => _lastFinalMult;

        private bool _prevRoulettePlaying;
        private float _rouletteResultAt = -100f;
        private int _rouletteLastColor = -1;
        private bool _rouletteLastWon;
        public string RouletteResult { get; private set; } = "";
        public float RouletteResultAge => Time.time - _rouletteResultAt;

        private readonly Harmony _harmony = new Harmony(GUID);

        private void Awake()
        {
            Instance = this;
            AutoTrick = Config.Bind("Main", "Auto Trickshot", true, "Automatically spin + fire on a launched fish.");
            AirborneOnly = Config.Bind("Main", "Airborne Only", true, "Only trickshot fish that are airborne and free of the rod. Turn OFF to also trickshot a fish still hooked on your line (no release/reel-out needed). The trickshot never fires while the fish is being reeled in or wound up close to the reel.");
            AutoJump = Config.Bind("Main", "Auto Jump", true, "Jump right before the trickshot for the Aerial / Dogfight bonus (on a 1.25x to 1.5x).");
            OneShotHelper = Config.Bind("Main", "One-Shot Helper", true, "Massively boosts weapon damage so the fish dies in one hit (One Shot One Kill + Overkill).");
            ForceLastBullet = Config.Bind("Main", "Force Last Bullet", true, "Loads exactly one round before firing so the shot lands as the 'Last Bullet' (+1.25x).");
            AimHead = Config.Bind("Main", "Aim Head", true, "Aim at the head for the Headshot bonus (+1.25x).");
            ShowHud = Config.Bind("Main", "HUD", true, "Show the small overlay.");
            SpinSpeed = Config.Bind("Main", "Spin Speed (deg/s)", 1500f, new ConfigDescription("How fast the 360 spins. Higher = quicker trickshots.", new AcceptableValueRange<float>(500f, 7200f)));
            LeadTime = Config.Bind("Main", "Target Lead (s)", 0f, new ConfigDescription("Aim ahead of the fish by this many seconds of its velocity.", new AcceptableValueRange<float>(-1f, 1f)));
            RangeLimit = Config.Bind("Main", "Max Range (m)", 40f, new ConfigDescription("Maximum distance to attempt a trickshot.", new AcceptableValueRange<float>(5f, 200f)));
            DamageAmount = Config.Bind("Main", "Helper Damage", 5000, new ConfigDescription("Damage used by the One-Shot helper.", new AcceptableValueRange<int>(1, 100000)));
            WeaponFilter = Config.Bind("Main", "Weapons", "pistol,sniper,shotgun", "Comma separated substrings matched against the held weapon name.");
            MenuKey = Config.Bind("Hotkeys", "Menu Toggle", "F8", "Key to open/close the menu.");
            ManualKey = Config.Bind("Hotkeys", "Manual Trickshot", "F9", "Force a trickshot right now on the tracked fish.");
            LogKey = Config.Bind("Hotkeys", "Log Window", "F7", "Key to open/close the log window.");
            DebugKey = Config.Bind("Hotkeys", "Debug Dump", "F6", "Print a full state dump to the log.");
            SpinExtra = Config.Bind("Trick", "Spin Degrees", 360f, new ConfigDescription("Total rotation before snapping onto the target (style overkill).", new AcceptableValueRange<float>(360f, 1080f)));
            SpinDirection = Config.Bind("Trick", "Spin Direction", 1, new ConfigDescription("1 = clockwise, -1 = counter-clockwise.", new AcceptableValueRange<int>(-1, 1)));
            ComboWindow = Config.Bind("Trick", "Combo Window (s)", 8f, new ConfigDescription("How long a chain of trickshots may last.", new AcceptableValueRange<float>(1f, 30f)));
            SpinFx = Config.Bind("Trick", "Spin FX", true, "Animated light sweep and vignette during a 360.");
            AlwaysWinRoulette = Config.Bind("Roulette", "Always Win Roulette", false, "Tells the table the ball landed on your bet color (BLACK/RED x2, GREEN x35). Works when you are the host (single player / your own co-op lobby).");
            SlotForce = Config.Bind("Casino", "Slot Machine Rarity", "OFF", new ConfigDescription("Force the cosmetic skin slot machine to land on a marked skin every spin. RED = rare-marked, GOLD = legendary-marked. The machine still needs a fish dropped in (or use FREE SPINS). Works when you are the host.", new AcceptableValueList<string>("OFF", "RED", "GOLD")));
            SlotFreeSpins = Config.Bind("Casino", "Free Spins", false, "Auto-spins the skin slot machine for free while you stand beside it - no fish is deposited or destroyed. Works when you are the host.");

            TrickshotLog.Init();
            TrickshotLog.Log(NAME + " v" + VERSION + " loading.");
            TrickshotLog.Log("Unity version: " + Application.unityVersion);

            _harmony.PatchAll(typeof(TrickshotMenuPlugin).Assembly);
            TrickshotLog.Log("Harmony PatchAll done. Attachments type available: " + (typeof(Attachments) != null));

            if (_weaponShoot == null)
            {
                _weaponShoot = typeof(Weapon).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_ammoField == null)
            {
                _ammoField = typeof(Weapon).GetField("<Ammo>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_reloadField == null)
            {
                _reloadField = typeof(Weapon).GetField("_isReloading", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_queueReloadField == null)
            {
                _queueReloadField = typeof(Weapon).GetField("_queueReload", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_camRotField == null)
            {
                _camRotField = typeof(PlayerCamera).GetField("_rot", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            TrickshotLog.Log("Reflection: Shoot=" + Display(_weaponShoot) + " Ammo=" + Display(_ammoField) +
                " _isReloading=" + Display(_reloadField) + " _queueReload=" + Display(_queueReloadField) + " _rot=" + Display(_camRotField));

            if (!string.IsNullOrEmpty(TrickshotLog.FilePath))
            {
                TrickshotLog.Log("Log file: " + TrickshotLog.FilePath);
            }
            Logger.LogInfo($"{NAME} loaded.");
        }

        private static string Display(MethodInfo m) { return m != null ? "ok" : "MISSING"; }
        private static string Display(FieldInfo f) { return f != null ? "ok" : "MISSING"; }

        private static bool KeyDown(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
            {
                return false;
            }
            return Enum.TryParse<KeyCode>(keyName, ignoreCase: true, out KeyCode key) && Input.GetKeyDown(key);
        }

        private void Update()
        {
            try
            {
                _menuAnim = Mathf.MoveTowards(_menuAnim, _menuVisible ? 1f : 0f, Time.deltaTime * 6f);
                _guideAnim = Mathf.MoveTowards(_guideAnim, _guideVisible ? 1f : 0f, Time.deltaTime * 6f);

                Player p = Player.LocalPlayer;

                bool rebinding = _rebindTarget != "";
                if (!rebinding)
                {
                    if (KeyDown(MenuKey.Value))
                    {
                        _menuVisible = !_menuVisible;
                    }
                    if (KeyDown(LogKey.Value))
                    {
                        _logVisible = !_logVisible;
                    }
                    if (KeyDown(DebugKey.Value))
                    {
                        DumpState(p);
                    }
                }
                else
                {
                    CaptureRebind();
                    if (!_menuVisible)
                    {
                        CancelRebind();
                    }
                }
                if (!rebinding && _guideVisible && KeyDown("Escape"))
                {
                    _guideVisible = false;
                }

                bool uiOpen = _menuVisible || _logVisible || _guideVisible;
                if (uiOpen != _cursorManaged)
                {
                    _cursorManaged = uiOpen;
                    if (uiOpen)
                    {
                        _prevCursorLock = Cursor.lockState;
                        _prevCursorVisible = Cursor.visible;
                    }
                    else if (!_menuVisible && !_logVisible && !_guideVisible)
                    {
                        Cursor.lockState = _prevCursorLock;
                        Cursor.visible = _prevCursorVisible;
                    }
                }
                if (uiOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if (!p)
                {
                    _lineCandidate = null;
                    _target = null;
                    TargetScreenPos = null;
                    _spinFrac = 0f;
                    return;
                }

                if (SlotFreeSpins.Value)
                {
                    TryFreeSlotSpin(p);
                }

                if (KeyDown(ManualKey.Value))
                {
                    ScanTargets(p);
                    TryStartTrick(p, manual: true);
                }

                ScanTargets(p);
                if (AutoTrick.Value)
                {
                    TryStartTrick(p, manual: false);
                }

                if (_lastFish && _lastFish.IsDead)
                {
                    _lastFinalMult = _lastFish.KillScoreMultiplier;
                    if (_pendingKill == _lastFish && Time.time - _lastShotTime < 1.5f)
                    {
                        ConfirmKill(_lastFish);
                    }
                }

                if (_combo > 0 && Time.time > _comboUntil)
                {
                    _combo = 0;
                }

                if (!IsSpinning)
                {
                    _spinFrac = 0f;
                }

                if (_target != null && !_target.IsDead && Camera.main != null)
                {
                    Vector3 sp = Camera.main.WorldToScreenPoint(_target.transform.position + Vector3.up * 0.8f);
                    if (sp.z > 0f)
                    {
                        TargetScreenPos = new Vector2(sp.x, Screen.height - sp.y);
                        TargetDist = Vector3.Distance(p.Transform.position, _target.transform.position);
                    }
                    else
                    {
                        TargetScreenPos = null;
                    }
                }
                else
                {
                    TargetScreenPos = null;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                TrickshotLog.LogException("Update", e);
            }
        }

        private void ConfirmKill(Creature fish)
        {
            _pendingKill = null;
            _killsConfirmed++;
            _combo++;
            _bestCombo = Mathf.Max(_bestCombo, _combo);
            _comboUntil = Time.time + ComboWindow.Value;
            _comboFlashTime = Time.time;

            float mult = Mathf.Max(_lastFinalMult, 1f);
            if (mult > _bestMult)
            {
                _bestMult = mult;
            }

            float worth = 0f;
            try
            {
                worth = fish.TotalWorth;
            }
            catch
            {
            }
            _estEarned += (double)(worth * mult);
            LastBonusSummary = BuildBonusSummary(fish, mult);

            UiKit.Toast("IMPRESSIVE!  x" + mult.ToString("0.00"), UiKit.Gold);
            if (_combo >= 2)
            {
                UiKit.Toast("COMBO x" + _combo, UiKit.Purple);
            }
            TrickshotLog.Log("KILL CONFIRMED | mult=" + mult.ToString("0.00") + " combo=" + _combo +
                " est=" + (worth * mult).ToString("0") + " | " + LastBonusSummary);
        }

        private string BuildBonusSummary(Creature fish, float mult)
        {
            System.Collections.Generic.List<string> b = new System.Collections.Generic.List<string>();
            b.Add("360");
            b.Add("NoScope");
            if (AimHead.Value && fish != null)
            {
                b.Add("Headshot");
            }
            if (OneShotHelper.Value)
            {
                b.Add("OneShot");
                b.Add("Overkill");
            }
            if (ForceLastBullet.Value)
            {
                b.Add("LastBullet");
            }
            b.Add("FlyFishing");
            if (AutoJump.Value)
            {
                b.Add("Aerial/Dogfight");
            }
            if (b.Count >= 5 && mult >= 5f)
            {
                b.Add("Impressive");
            }
            return string.Join(" + ", b.ToArray());
        }

        private void OnGUI()
        {
            try
            {
                UiKit.DrawHud(this);
                UiKit.DrawFx(this);
                UiKit.DrawToasts();
                UiKit.DrawPanel(this);
                if (_logVisible)
                {
                    UiKit.DrawLogWindow(this);
                }
                if (_guideVisible)
                {
                    UiKit.DrawGuidePanel(this);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                TrickshotLog.LogException("OnGUI", e);
            }
        }
        public static bool MenuVisibleForUi => Instance != null ? Instance._menuVisible : false;

        public bool GuideVisible => _guideVisible;
        public float GuideAnim => _guideAnim;
        public string RebindingEntry => _rebindTarget;
        public static bool GuideVisibleForUi => Instance != null && Instance._guideVisible;

        public void ToggleGuide()
        {
            _guideVisible = !_guideVisible;
        }

        public void BeginRebind(string entry)
        {
            _rebindTarget = entry == _rebindTarget ? "" : entry;
            if (_rebindTarget != "")
            {
                _rebindStartFrame = Time.frameCount;
            }
        }

        public void CancelRebind()
        {
            _rebindTarget = "";
        }

        public string KeybindValue(string entry)
        {
            ConfigEntry<string> e = KeybindFor(entry);
            return e != null ? e.Value : "?";
        }

        private ConfigEntry<string> KeybindFor(string entry)
        {
            switch (entry)
            {
                case "Menu": return MenuKey;
                case "Manual": return ManualKey;
                case "Log": return LogKey;
                case "Debug": return DebugKey;
                default: return null;
            }
        }

        private void CaptureRebind()
        {
            if (Time.frameCount - _rebindStartFrame < 3)
            {
                return;
            }
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(k))
                {
                    continue;
                }
                if (k == KeyCode.Escape)
                {
                    CancelRebind();
                    return;
                }
                ConfigEntry<string> e = KeybindFor(_rebindTarget);
                if (e != null)
                {
                    e.Value = k.ToString();
                    string keyName = k.ToString().ToUpper();
                    UiKit.Toast("BOUND  " + _rebindTarget.ToUpper() + "  →  " + keyName, UiKit.Accent);
                    TrickshotLog.Log("KEYBIND | " + _rebindTarget + " = " + keyName);
                }
                CancelRebind();
                return;
            }
        }

        // =====================================================================

        private void ScanTargets(Player p)
        {
            if (_trickRoutine != null)
            {
                return;
            }
            if (Time.time < _nextScanTime)
            {
                return;
            }
            _nextScanTime = Time.time + 0.1f;

            Creature nowLine = null;
            Creature nearestAir = null;
            float best = float.MaxValue;
            Vector3 plrPos = p.Transform.position;

            List<Item> items = new List<Item>(ItemManager.Items.Values);
            foreach (Item item in items)
            {
                if (item == null)
                {
                    continue;
                }
                Creature c = item.Creature;
                if (c == null || c.IsDead || c.KillScoreMultiplier != 1f)
                {
                    continue;
                }
                if (c.AttachedRod != null && c.AttachedRod.Holder == p)
                {
                    nowLine = c;
                    continue;
                }
                if (c == _target)
                {
                    _targetSince = Time.time;
                }
                if (AirborneOnly.Value && !IsAirborne(c))
                {
                    continue;
                }
                float dist = Vector3.Distance(plrPos, c.transform.position);
                if (dist > RangeLimit.Value)
                {
                    continue;
                }
                if (dist < best)
                {
                    best = dist;
                    nearestAir = c;
                }
            }

            if (nowLine != null)
            {
                bool fresh = _target != nowLine;
                _lineCandidate = nowLine;
                if (AirborneOnly.Value || RodReelingInOrReel(nowLine.AttachedRod))
                {
                    if (fresh)
                    {
                        string why = RodReelingInOrReel(nowLine.AttachedRod) ? "being reeled in / on the reel - waiting" : "waiting for launch";
                        TrickshotLog.Log("Fish on my line: " + nowLine.GetName() + " - " + why);
                    }
                    _target = null;
                    _targetSince = float.PositiveInfinity;
                    return;
                }
                if (fresh)
                {
                    TrickshotLog.Log("Fish on my line - trickshot armed (airborne not required): " + nowLine.GetName());
                }
                _target = nowLine;
                _targetSince = Time.time;
                return;
            }

            // our hooked fish just got launched
            if (_lineCandidate != null)
            {
                _target = _lineCandidate;
                _targetSince = Time.time;
                TrickshotLog.Log("Launched fish: " + _target.GetName() + " at " + _target.transform.position.ToString("0.0"));
            }
            _lineCandidate = null;

            if (_target == null)
            {
                _target = nearestAir;
                if (_target != null)
                {
                    _targetSince = Time.time;
                    TrickshotLog.Log("No hooked fish tracked, using nearest airborne creature: " + _target.GetName() + " (" + best.ToString("0.0") + "m)");
                }
            }
            else if (_target != null && Time.time - _targetSince > 3f)
            {
                TrickshotLog.Log("Target lost (timed out after 3s)");
                _target = null;
            }
        }

        private bool IsValidTarget(Creature c, Vector3 plrPos)
        {
            if (c == null || c.IsDead || c.KillScoreMultiplier != 1f)
            {
                return false;
            }
            if (c.AttachedRod != null && RodReelingInOrReel(c.AttachedRod))
            {
                return false;
            }
            if (AirborneOnly.Value)
            {
                if (c.AttachedRod != null)
                {
                    return false;
                }
                if (!IsAirborne(c))
                {
                    return false;
                }
            }
            return Vector3.Distance(plrPos, c.transform.position) <= RangeLimit.Value;
        }

        public bool IsAirborne(Creature c)
        {
            if (c == null || c.transform == null)
            {
                return false;
            }
            try
            {
                if (!Physics.Raycast(c.transform.position, Vector3.down, 1.6f, GameInfo.LevelLayer))
                {
                    return true;
                }
            }
            catch
            {
                // GameInfo not ready yet
            }
            if (c.Rig != null && c.transform.position.y > 1f)
            {
                return c.Rig.velocity.sqrMagnitude > 2f;
            }
            return false;
        }

        private static FieldInfo _rodIsReelingInField;
        private static FieldInfo _rodLineStepsField;

        private bool RodReelingInOrReel(FishingRod rod)
        {
            if (rod == null)
            {
                return false;
            }
            try
            {
                if (_rodIsReelingInField == null)
                {
                    _rodIsReelingInField = typeof(FishingRod).GetField("_isReelingIn", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_rodIsReelingInField != null && (bool)_rodIsReelingInField.GetValue(rod))
                {
                    return true;
                }
                if (_rodLineStepsField == null)
                {
                    _rodLineStepsField = typeof(FishingRod).GetField("_curLineLengthMulti", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_rodLineStepsField != null)
                {
                    int steps = (int)_rodLineStepsField.GetValue(rod);
                    if (steps <= 1)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // rod was destroyed mid-frame - treat as unsafe to trick
                return true;
            }
            return false;
        }

        private bool TargetReeled(Creature c)
        {
            return c != null && c.AttachedRod != null && RodReelingInOrReel(c.AttachedRod);
        }

        public struct RouletteState
        {
            public bool Present;
            public bool Spinning;
            public bool Playing;
            public bool BetPlaced;
            public bool Betting;
            public int TotalWorth;
            public int BetColor;     // 0=Black, 1=Red, 2=Green, -1 unknown
            public int BallColor;    // live slot under the ball, -1 unknown
        }

        public RouletteState GetRouletteState()
        {
            var s = new RouletteState();
            try
            {
                LocalCasino lc = LocalCasino.Instance;
                CasinoManager c = CasinoManager.Instance;
                if (lc == null || c == null)
                {
                    return s;
                }
                s.Present = true;

                if (_lcIsSpinningField == null)
                {
                    _lcIsSpinningField = typeof(LocalCasino).GetField("_isSpinning", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_lcIsPlayingField == null)
                {
                    _lcIsPlayingField = typeof(LocalCasino).GetField("_isPlaying", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_lcCurColorField == null)
                {
                    _lcCurColorField = typeof(LocalCasino).GetField("_curColor", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                if (_casinoCurBetColorField == null)
                {
                    _casinoCurBetColorField = typeof(CasinoManager).GetField("_curBetColor", BindingFlags.Static | BindingFlags.NonPublic);
                }
                if (_lcGetColorMethod == null)
                {
                    _lcGetColorMethod = typeof(LocalCasino).GetMethod("GetRouletteColorFromBall", BindingFlags.Instance | BindingFlags.NonPublic);
                }

                if (_lcIsSpinningField != null)
                {
                    s.Spinning = (bool)_lcIsSpinningField.GetValue(lc);
                }
                if (_lcIsPlayingField != null)
                {
                    s.Playing = (bool)_lcIsPlayingField.GetValue(lc);
                }
                s.BetColor = ReadRouletteBetColor();
                // live slot under the ball = exactly the value the server finalizes at the end of the spin
                if (_lcGetColorMethod != null)
                {
                    s.BallColor = (int)(BetColor)_lcGetColorMethod.Invoke(lc, null);
                }
                if (s.BetColor < 0 || s.BetColor > 2)
                {
                    s.BetColor = -1;
                }
                if (s.BallColor < 0 || s.BallColor > 2)
                {
                    s.BallColor = -1;
                }

                if (_prevRoulettePlaying && !s.Playing)
                {
                    int settled = -1;
                    if (_lcCurColorField != null)
                    {
                        settled = (int)(BetColor)_lcCurColorField.GetValue(lc);
                    }
                    if (settled >= 0 && settled <= 2)
                    {
                        _rouletteLastColor = settled;
                        _rouletteLastWon = settled == s.BetColor;
                        _rouletteResultAt = Time.time;
                        int mult = _rouletteLastWon ? (settled == 2 ? 35 : 2) : 0;
                        RouletteResult = settled + "|" + (_rouletteLastWon ? mult.ToString() : "0");
                        TrickshotLog.Log("ROULETTE RESULT | landed=" + RouletteColorName(settled) +
                            " bet=" + (s.BetColor >= 0 ? RouletteColorName(s.BetColor) : "?") +
                            (_rouletteLastWon ? " WON x" + mult : " LOST"));
                    }
                }
                _prevRoulettePlaying = s.Playing;

                s.Betting = CasinoManager.IsBetting;
                s.TotalWorth = c.TotalWorth;
                s.BetPlaced = s.TotalWorth > 0;
            }
            catch (Exception e)
            {
                TrickshotLog.LogException("Roulette", e);
            }
            return s;
        }

        private static string RouletteColorName(int c)
        {
            switch (c)
            {
                case 0: return "BLACK";
                case 1: return "RED";
                case 2: return "GREEN";
                default: return "UNKNOWN";
            }
        }

        private void TryFreeSlotSpin(Player p)
        {
            try
            {
                if (Time.time < _nextSlotSpinTime)
                {
                    return;
                }
                if (SlotMachine.IsRolling)
                {
                    _nextSlotSpinTime = Time.time + 1f;
                    return;
                }
                if (Server.Instance == null || !Server.Instance.IsServerInitialized)
                {
                    _nextSlotSpinTime = Time.time + 3f;
                    return;
                }
                if (_slotMachine == null)
                {
                    _slotMachine = UnityEngine.Object.FindObjectOfType<SlotMachine>();
                }
                if (_slotMachine == null)
                {
                    _nextSlotSpinTime = Time.time + 3f;
                    return;
                }
                float dist = Vector3.Distance(p.transform.position, _slotMachine.transform.position);
                if (dist > 9f)
                {
                    _nextSlotSpinTime = Time.time + 0.5f;
                    return;
                }
                _nextSlotSpinTime = Time.time + 7f;
                SlotMachineManager.RollRandom(p);
                UiKit.Toast("FREE SPIN", UiKit.Gold);
                TrickshotLog.Log("SLOT | free spin rolled from " + p.SteamName);
            }
            catch (Exception e)
            {
                TrickshotLog.LogException("SlotSpin", e);
            }
        }

        public static int ReadRouletteBetColor()
        {
            if (_casinoCurBetColorField == null)
            {
                _casinoCurBetColorField = typeof(CasinoManager).GetField("_curBetColor", BindingFlags.Static | BindingFlags.NonPublic);
            }
            if (_casinoCurBetColorField == null)
            {
                return -1;
            }
            try
            {
                int v = (int)(BetColor)_casinoCurBetColorField.GetValue(null);
                return v >= 0 && v <= 2 ? v : -1;
            }
            catch
            {
                return -1;
            }
        }

        public void TriggerManual()
        {
            Player p = Player.LocalPlayer;
            if (!p)
            {
                return;
            }
            ScanTargets(p);
            TryStartTrick(p, manual: true);
        }

        public bool ShowHudEnabled()
        {
            return Player.LocalPlayer != null && _trickRoutine != null || _lastFish != null || _status != "Idle";
        }

        public void ToggleLogWindow()
        {
            _logVisible = !_logVisible;
        }

        private void TryStartTrick(Player p, bool manual)
        {
            if (_trickRoutine != null || p == null || p.BlockInputs || p.Dying.IsDead)
            {
                return;
            }
            if (!manual && Time.time - _lastTrickTime < 1f)
            {
                return;
            }
            Item held = p.Holding != null ? p.Holding.HeldItem : null;
            if (held == null)
            {
                NoteReject("not holding any item", p);
                return;
            }
            Weapon w = held.Weapon;
            string holdingName = held.gameObject != null ? held.gameObject.name : "?";
            if (w == null)
            {
                NoteReject("held '" + holdingName + "' has no Weapon component", p);
                return;
            }
            string wName = w.gameObject != null ? w.gameObject.name : "?";
            if (!WeaponMatches(w))
            {
                NoteReject("held weapon '" + wName + "' does not match filter [" + WeaponFilter.Value + "]", p);
                return;
            }
            if (w.Ammo == 0 && !ForceLastBullet.Value)
            {
                NoteReject("weapon '" + wName + "' has 0 ammo and Force Last Bullet is OFF", p);
                return;
            }
            Creature t = _target;
            if (t == null)
            {
                string what = _lineCandidate != null
                    ? "a fish is STILL ON MY LINE (" + _lineCandidate.GetName() + ")" + (AirborneOnly.Value ? " - release it into the air (reel out!)" : "")
                    : "no qualifying creature in range";
                NoteReject(what, p);
                return;
            }
            if (!IsValidTarget(t, p.Transform.position))
            {
                NoteReject("target '" + t.GetName() + "' not valid (dead=" + t.IsDead + ", mult=" + t.KillScoreMultiplier +
                    ", onRod=" + (t.AttachedRod != null) + ", airborne=" + IsAirborne(t) +
                    ", dist=" + Vector3.Distance(p.Transform.position, t.transform.position).ToString("0.0") + "m)", p);
                return;
            }
            StartTrick(p, w, t);
        }

        private float _lastRejectLog;

        private void NoteReject(string reason, Player p)
        {
            if (Time.time - _lastRejectLog < 2f)
            {
                return;
            }
            _lastRejectLog = Time.time;
            Weapon w = p != null && p.Holding != null && p.Holding.HeldItem != null ? p.Holding.HeldItem.Weapon : null;
            string heldDesc = w != null ? w.gameObject.name + " [" + w.Ammo + " rnd]" : "none";
            TrickshotLog.Log("ARM CHECK | holding=" + heldDesc + " | " + reason);
        }

        private void DumpState(Player p)
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("========== STATE DUMP ==========");
                sb.Append("\n[Player] present=").Append(p != null);
                if (!p)
                {
                    TrickshotLog.Log(sb.ToString());
                    return;
                }
                sb.Append(" | BlockInputs=").Append(p.BlockInputs);
                sb.Append(" | Dying=").Append(p.Dying != null ? p.Dying.IsDead.ToString() : "null");
                sb.Append(" | Camera=").Append(p.Camera != null ? "ok" : "MISSING");
                sb.Append(" | CamObject=").Append(p.CamObject != null ? "ok" : "MISSING");

                Item held = p.Holding != null ? p.Holding.HeldItem : null;
                string heldName = held == null ? "none" : (held.gameObject != null ? held.gameObject.name : "destroyed-go");
                sb.Append("\n[HeldItem] ").Append(heldName);
                Weapon w = held != null ? held.Weapon : null;
                if (w != null)
                {
                    sb.Append("\n[Weapon] name=").Append(w.gameObject != null ? w.gameObject.name : "?");
                    sb.Append(" | ammo=").Append(w.Ammo);
                    sb.Append(" | matchesFilter=").Append(WeaponMatches(w));
                }
                else
                {
                    sb.Append("\n[Weapon] none");
                }

                sb.Append("\n[Flow] status=").Append(_status);
                sb.Append(" | routine=").Append(_trickRoutine != null ? "running" : "idle");
                sb.Append(" | lastMult=").Append(_lastFinalMult.ToString("0.00"));
                sb.Append("\n[LineCandidate] ").Append(_lineCandidate != null ? _lineCandidate.GetName() : "none");
                sb.Append("\n[Target] ").Append(_target != null ? _target.GetName() : "none");
                if (_target != null)
                {
                    sb.Append("\n  dead=").Append(_target.IsDead);
                    sb.Append(" | mult=").Append(_target.KillScoreMultiplier);
                    sb.Append(" | onRod=").Append(_target.AttachedRod != null);
                    sb.Append(" | airborne=").Append(IsAirborne(_target));
                    sb.Append(" | dist=").Append(Vector3.Distance(p.Transform.position, _target.transform.position).ToString("0.0")).Append("m");
                }

                sb.Append("\n[Config] auto=").Append(AutoTrick.Value);
                sb.Append(" | forceLast=").Append(ForceLastBullet.Value);
                sb.Append(" | oneShot=").Append(OneShotHelper.Value);
                sb.Append(" | aimHead=").Append(AimHead.Value);
                sb.Append(" | filter=[").Append(WeaponFilter.Value).Append("]");
                sb.Append(" | range=").Append(RangeLimit.Value).Append("m");
                sb.Append("\n[Patch] DamageActive=").Append(DamageActive).Append(" (overrides to ").Append(OverrideDamage).Append(")");
                sb.Append("\n[Reflection] Shoot=").Append(Display(_weaponShoot));
                sb.Append(" | AmmoField=").Append(Display(_ammoField));
                sb.Append(" | ReloadField=").Append(Display(_reloadField));
                sb.Append(" | QueueReloadField=").Append(Display(_queueReloadField));
                sb.Append(" | CamRotField=").Append(Display(_camRotField));

                List<Item> items = new List<Item>(ItemManager.Items.Values);
                int alive = 0;
                int aliveAir = 0;
                foreach (Item it in items)
                {
                    if (it == null || it.Creature == null || it.Creature.IsDead)
                    {
                        continue;
                    }
                    alive++;
                    if (IsAirborne(it.Creature))
                    {
                        aliveAir++;
                    }
                }
                sb.Append("\n[Creatures] tracked=").Append(items.Count).Append(" | alive=").Append(alive).Append(" | aliveAndAirborne=").Append(aliveAir);
                TrickshotLog.Log(sb.ToString());
            }
            catch (Exception e)
            {
                TrickshotLog.LogException("DumpState", e);
            }
        }

        private bool WeaponMatches(Weapon w)
        {
            string name = (w != null && w.gameObject != null) ? w.gameObject.name : "";
            foreach (string part in WeaponFilter.Value.Split(','))
            {
                if (part.Length > 0 && name.IndexOf(part.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        // =====================================================================

        private void StartTrick(Player p, Weapon w, Creature fish)
        {
            string weaponName = w != null && w.gameObject != null ? w.gameObject.name : "?";
            int ammo = w != null ? w.Ammo : -1;
            TrickshotLog.Log("TRICK START | weapon=" + weaponName + " ammo=" + ammo +
                " | target=" + fish.GetName() + " range=" + Vector3.Distance(p.Transform.position, fish.transform.position).ToString("0.0") +
                "m airborne=" + IsAirborne(fish));
            _lastTrickTime = Time.time;
            _lastFish = fish;
            _status = "Spinning";
            _trickRoutine = StartCoroutine(TrickRoutine(p, w, fish));
        }

        private IEnumerator TrickRoutine(Player p, Weapon w, Creature fish)
        {
            try
            {
                if (AutoJump.Value && p.Movement != null && p.Movement.Grounded)
                {
                    p.Movement.Jump();
                }
                yield return new WaitForEndOfFrame();
                yield return null;

                // stash the fired round so the killing blow is the last bullet
                if (ForceLastBullet.Value)
                {
                    try
                    {
                        SetAmmo(w, 1);
                        CancelReload(w);
                    }
                    catch
                    {
                        // best effort
                    }
                }

                Vector3 startAim = AimPoint(fish);
                float spinTotal = Mathf.Max(359f, SpinExtra.Value);
                float dirSign = SpinDirection.Value < 0 ? -1f : 1f;
                float startYaw = p.CamObject.eulerAngles.y + 720f; // anchor above 360 so we can drive monotonically
                float pitch = PitchToward(p, startAim);
                SetCamRot(p, startYaw, pitch);

                float spun = 0f;
                _spinFrac = 0f;
                while (spun < spinTotal - 1f)
                {
                    if (!Validate(p, w, fish))
                    {
                        _status = "Aborted";
                        TrickshotLog.Log("TRICK ABORT (spin): invalid target/state");
                        yield break;
                    }
                    float dt = Time.deltaTime;
                    if (dt <= 0f)
                    {
                        dt = 0.0005f;
                    }
                    spun += SpinSpeed.Value * dt;
                    _spinFrac = Mathf.Clamp01(spun / spinTotal);
                    SetCamRot(p, startYaw + dirSign * spun, pitch);
                    yield return null;
                }
                _spinFrac = 1f;

                _status = "Snapping";
                int guard = 0;
                while (guard++ < 60)
                {
                    if (!Validate(p, w, fish))
                    {
                        _status = "Aborted";
                        TrickshotLog.Log("TRICK ABORT (snap): invalid target/state");
                        yield break;
                    }
                    Vector3 aim = AimPoint(fish);
                    Vector3 from = p.CamObject.position;
                    Vector3 dir = aim - from;
                    float dist = dir.magnitude;
                    if (dist < 0.05f)
                    {
                        break;
                    }
                    dir /= dist;

                    float yawDes = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                    float curYaw = Mathf.Repeat(p.CamObject.eulerAngles.y, 360f);
                    float diff = Mathf.DeltaAngle(curYaw, yawDes);
                    float step = Mathf.Max(SpinSpeed.Value * Time.deltaTime, 0.6f);

                    if (Mathf.Abs(diff) <= step || Mathf.Abs(diff) < 0.4f)
                    {
                        SetLookAt(p, w, aim);
                        break;
                    }

                    SetCamRot(p, curYaw + Mathf.Sign(diff) * step, PitchToward(p, aim));
                    yield return null;
                }

                if (!Validate(p, w, fish))
                {
                    _status = "Aborted";
                    TrickshotLog.Log("TRICK ABORT (pre-fire): invalid target/state");
                    yield break;
                }
                Vector3 finalAim = AimPoint(fish);
                SetLookAt(p, w, finalAim);
                yield return null;

                _status = "Firing";
                Fire(p, w, finalAim);
                _status = "Fired";
                _tricksFired++;
                _lastShotTime = Time.time;
                _pendingKill = fish;
                UiKit.Toast("TRICK FIRED  " + fish.GetName(), UiKit.Accent);
                TrickshotLog.Log("TRICK FIRED at " + fish.GetName());
                _target = null;
                _lineCandidate = null;
            }
            finally
            {
                _trickRoutine = null;
            }
        }

        private bool Validate(Player p, Weapon w, Creature fish)
        {
            if (p == null || !Player.LocalPlayer || p.BlockInputs || p.Dying.IsDead)
            {
                return false;
            }
            Item held = p.Holding != null ? p.Holding.HeldItem : null;
            if (held == null || held.Weapon != w)
            {
                return false;
            }
            if (fish == null || fish.IsDead || fish.KillScoreMultiplier != 1f)
            {
                return false;
            }
            if (fish.AttachedRod != null && RodReelingInOrReel(fish.AttachedRod))
            {
                return false;
            }
            return true;
        }

        private Vector3 AimPoint(Creature fish)
        {
            Vector3 head = fish.transform.TransformPoint(new Vector3(0f, 0f, fish.HeadPos + 0.15f));
            Vector3 vel = Vector3.zero;
            if (fish.Rig != null)
            {
                vel = fish.Rig.velocity;
            }
            if (LeadTime.Value != 0f)
            {
                head += vel * LeadTime.Value;
            }
            return head;
        }

        private float PitchToward(Player p, Vector3 point)
        {
            Vector3 dir = point - p.CamObject.position;
            float dist = dir.magnitude;
            if (dist < 0.001f)
            {
                return 0f;
            }
            float pitch = Mathf.Asin(Mathf.Clamp(-dir.y / dist, -1f, 1f)) * Mathf.Rad2Deg;
            if (pitch < -89f)
            {
                pitch = -89f;
            }
            if (pitch > 89f)
            {
                pitch = 89f;
            }
            return pitch;
        }

        private void SetCamRot(Player p, float yaw, float pitch)
        {
            PlayerCamera cam = p.Camera;
            if (cam == null)
            {
                return;
            }
            if (_camRotField == null)
            {
                _camRotField = typeof(PlayerCamera).GetField("_rot", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            try
            {
                _camRotField.SetValue(cam, new Vector3(pitch, yaw, 0f));
            }
            catch
            {
                // ignored
            }
            cam.SetRot(yaw);
        }

        private void SetLookAt(Player p, Weapon w, Vector3 point)
        {
            Vector3 from = p.CamObject.position;
            Vector3 dir = point - from;
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }
            dir.Normalize();
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Asin(Mathf.Clamp(-dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            SetCamRot(p, yaw, pitch);
            if (AimHead.Value && w.Attachments != null && w.Attachments.FirePoint != null)
            {
                w.Attachments.FirePoint.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        private void Fire(Player p, Weapon w, Vector3 aim)
        {
            if (p == null || w == null || w.Attachments == null || w.Attachments.FirePoint == null)
            {
                return;
            }
            Vector3 dir = aim - w.Attachments.FirePoint.position;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = w.Attachments.FirePoint.forward;
            }
            dir.Normalize();
            w.Attachments.FirePoint.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (_weaponShoot == null)
            {
                _weaponShoot = typeof(Weapon).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            _weaponShoot.Invoke(w, null);
        }

        private void SetAmmo(Weapon w, int ammo)
        {
            if (_ammoField == null)
            {
                _ammoField = typeof(Weapon).GetField("<Ammo>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_ammoField != null)
            {
                _ammoField.SetValue(w, ammo);
            }
        }

        private void CancelReload(Weapon w)
        {
            if (_reloadField == null)
            {
                _reloadField = typeof(Weapon).GetField("_isReloading", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_queueReloadField == null)
            {
                _queueReloadField = typeof(Weapon).GetField("_queueReload", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            if (_reloadField != null)
            {
                _reloadField.SetValue(w, false);
            }
            if (_queueReloadField != null)
            {
                _queueReloadField.SetValue(w, false);
            }
        }

        // =====================================================================
        // Info for the UI
        // =====================================================================
        public static bool DamageActive => Instance != null && OneShotHelper.Value && AutoTrick.Value;
        public static int OverrideDamage => DamageAmount.Value;

        public string GetStatus()
        {
            Player p = Player.LocalPlayer;
            if (!p)
            {
                return "Idle";
            }
            Weapon w = p.Holding != null && p.Holding.HeldItem != null ? p.Holding.HeldItem.Weapon : null;
            string weapon = w != null ? (w.gameObject.name + "  [" + w.Ammo + " rnd]") : "no weapon";
            string target = _target != null ? (_target.IsDead ? "dead" : (_lastFish == _target ? _target.GetName() + " (launched)" : _target.GetName())) : "no target";
            if (_target != null && _target.transform != null && !_target.IsDead)
            {
                Vector3 rel = _target.transform.position - p.Transform.position;
                target += " · " + Mathf.RoundToInt(rel.magnitude) + "m";
            }
            return _status + " | " + weapon + " | " + target;
        }

        public string GetMultiplierText()
        {
            if (_lastFish != null && _lastFish.IsDead)
            {
                return "Last kill multiplier: x" + _lastFinalMult.ToString("0.00");
            }
            return "Last kill multiplier: x" + _lastFinalMult.ToString("0.00") + " (pending)";
        }

        // =====================================================================
        // UI read model
        // =====================================================================
        public float MenuAnim => Mathf.Clamp01(_menuAnim);
        public bool IsSpinning => _trickRoutine != null;
        public float SpinFrac => Mathf.Clamp01(_spinFrac);
        public bool HasTargetLock => _target != null && !_target.IsDead && _target.AttachedRod == null;
        public int TricksFired => _tricksFired;
        public int KillsConfirmed => _killsConfirmed;
        public int Combo => _combo;
        public int BestCombo => _bestCombo;
        public float BestMult => _bestMult;
        public double EstEarned => _estEarned;
        public float ComboAge => Time.time - _comboFlashTime;

        public void ResetStats()
        {
            _tricksFired = 0;
            _killsConfirmed = 0;
            _combo = 0;
            _bestCombo = 0;
            _bestMult = 1f;
            _estEarned = 0;
            _comboUntil = 0f;
            LastBonusSummary = "ready";
            TrickshotLog.Log("Stats reset.");
        }

        public string GetWeaponDesc()
        {
            Player p = Player.LocalPlayer;
            if (!p || p.Holding == null || p.Holding.HeldItem == null)
            {
                return "no weapon";
            }
            Weapon w = p.Holding.HeldItem.Weapon;
            return w != null ? (w.gameObject != null ? w.gameObject.name : "weapon") + "  [" + w.Ammo + " rnd]" : "no weapon";
        }
    }
}