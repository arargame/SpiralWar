using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SpiralWar.Audio;
using SpiralWar.Core;
using SpiralWar.Core.Config;
using SpiralWar.Core.Input;
using SpiralWar.Entities;
using SpiralWar.Levels;
using SpiralWar.Rendering;
using SpiralWar.Systems;

namespace SpiralWar.Screens
{
    /// <summary>
    /// The arena. Owns the level's entities and systems, wires their events together and
    /// decides when the level ends.
    ///
    /// It deliberately contains no rendering maths and no collision maths: those live in
    /// <see cref="HudRenderer"/>, <see cref="NeonRenderer"/> and <see cref="CollisionSystem"/>.
    /// What is left here is the part that genuinely belongs to "a level of SpiralWar" — the
    /// state machine, the feedback routing and the progression rules.
    /// </summary>
    public sealed class GameScreen : IScreen
    {
        private enum Phase { Intro, Playing, Outcome }

        private readonly ScreenManager _manager;
        private readonly LevelDefinition _level;
        private readonly string _milestoneBanner;

        // Entities
        private Player? _player;
        private Snake? _snake;

        // Systems
        private BulletSystem? _bullets;
        private SupplySystem? _supplies;
        private CollisionSystem? _collisions;
        private ParticleSystem? _particles;
        private FloatingTextSystem? _floatingText;
        private readonly CameraShake _shake = new CameraShake();
        private readonly HudRenderer _hud = new HudRenderer();

        // Volley buffer, allocated once — a fired volley must never allocate.
        private readonly float[] _shotAngles = new float[Player.MaxShotsPerVolley];

        // State
        private Phase _phase = Phase.Intro;
        private float _phaseTimer;
        private bool _playerWon;
        private float _runSeconds;
        private int _ringsDestroyed;

        private const float IntroDuration = 2.0f;
        private const float OutcomeDuration = 2.6f;
        private const float HintFadeStart = 4f;
        private const float HintFadeLength = 2f;

        public bool IsPopup => false;

        public GameScreen(ScreenManager manager, int levelIndex)
        {
            _manager = manager;
            _level = manager.Levels.GetLevel(levelIndex);
            _milestoneBanner = LevelDesigner.GetMilestoneBanner(_level);
        }

        // ── Setup ──────────────────────────────────────────────────────────

        public void LoadContent(ContentManager content)
        {
            var pixel = _manager.Pixel!;
            var font = _manager.GameFont!;

            _bullets = new BulletSystem(pixel);
            _supplies = new SupplySystem(pixel, font);
            _particles = new ParticleSystem(pixel);
            _floatingText = new FloatingTextSystem();
            _collisions = new CollisionSystem(_bullets);

            _player = new Player(pixel);
            _player.Fired += OnPlayerFired;
            _player.Died += OnPlayerDied;

            // Seeding by level keeps a given level's ring layout stable across attempts, so a
            // player can learn it instead of re-rolling the dice on every retry.
            _snake = new Snake(_level, font, pixel, seed: _level.Index * 7919 + 13);
            _snake.SegmentKilled += OnSegmentKilled;

            _collisions.BulletImpact += OnBulletImpact;
            _collisions.PlayerContact += OnPlayerContact;
            _collisions.CoreBreached += OnCoreBreached;

            _supplies.Collected += OnSupplyCollected;
            _supplies.Dropped += OnSupplyDropped;
        }

        public void Enter()
        {
            if (_phase == Phase.Intro && _level.IsBoss)
                _manager.Audio.Play(SoundId.BossWarning, 0.8f);
        }

        public void Leave() { }

        public bool OnBack()
        {
            if (_phase == Phase.Outcome) return true; // do not interrupt the result card

            _manager.Push(new PauseScreen(_manager, _level.Index));
            return true;
        }

        // ── Update ─────────────────────────────────────────────────────────

        public void Update(GameTime gameTime, InputState input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _phaseTimer += dt;

            _shake.Update(gameTime);
            _particles?.Update(gameTime);
            _floatingText?.Update(gameTime);

            switch (_phase)
            {
                case Phase.Intro:
                    UpdateIntro(gameTime, input);
                    break;

                case Phase.Playing:
                    UpdatePlaying(gameTime, input, dt);
                    break;

                default:
                    UpdateOutcome();
                    break;
            }
        }

        private void UpdateIntro(GameTime gameTime, InputState input)
        {
            // The ship is live during the intro so the player can already aim, but the snake
            // holds still. Nothing feels worse than a countdown that steals control.
            _player?.Update(gameTime, input);
            _bullets?.Update(gameTime);

            if (_phaseTimer >= IntroDuration)
                EnterPhase(Phase.Playing);
        }

        private void UpdatePlaying(GameTime gameTime, InputState input, float dt)
        {
            _runSeconds += dt;

            _player?.Update(gameTime, input);
            _snake?.Update(gameTime);
            _bullets?.Update(gameTime);
            _supplies?.Update(gameTime, _player);

            if (_snake != null && _player != null)
                _collisions?.Update(_snake, _player);

            if (_snake != null && _snake.IsCleared)
                FinishLevel(won: true);
        }

        private void UpdateOutcome()
        {
            if (_phaseTimer < OutcomeDuration) return;

            if (!_playerWon)
            {
                _manager.PopToRoot();
                return;
            }

            if (_level.Index >= GameConstants.MaxLevel)
            {
                _manager.PopToRoot();
                return;
            }

            _manager.Replace(new GameScreen(_manager, _level.Index + 1));
        }

        private void EnterPhase(Phase phase)
        {
            _phase = phase;
            _phaseTimer = 0f;
        }

        // ── Events ─────────────────────────────────────────────────────────

        private void OnPlayerFired(object? sender, EventArgs e)
        {
            if (_player == null || _bullets == null) return;

            int count = _player.GetShotAngles(_shotAngles);
            Vector2 muzzle = _player.GetMuzzlePosition();
            Color tint = _player.UsesWideFireSound ? Bullet.WideColor : Bullet.DefaultColor;

            for (int i = 0; i < count; i++)
            {
                float angle = _shotAngles[i];
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                _bullets.Spawn(muzzle, direction, _player.BulletDamage, tint);
            }

            _particles?.EmitMuzzle(muzzle, _player.Rotation, tint);

            // One sound per VOLLEY, not per bullet — seven stacked copies of the same waveform
            // is what made the old build unpleasant to listen to.
            SoundId id = _player.UsesWideFireSound ? SoundId.ShootWide : SoundId.Shoot;
            _manager.Audio.PlayVaried(id, 0.9f, 0.10f);
        }

        private void OnBulletImpact(object? sender, BulletImpactEventArgs e)
        {
            SnakeSegment segment = e.Segment;

            _particles?.EmitHit(e.Point, e.Direction, segment.NeonColor);
            _shake.Add(GameConstants.ShakeOnSegmentHit);

            float pan = AudioManager.PanFor(e.Point.X);

            if (e.Destroyed)
            {
                // Destruction is handled in OnSegmentKilled so it also covers any future
                // non-bullet kill. Only the impact-specific feedback belongs here.
                return;
            }

            if (GameConfig.Settings.ShowDamageNumbers && e.DamageApplied > 0)
                _floatingText?.ShowDamage(e.Point, e.DamageApplied,
                    Color.Lerp(segment.NeonColor, Color.White, 0.55f));

            // Pitch rises with how much of the ring the hit took off, so a hit that nearly
            // finishes a ring sounds different from a scratch on a fresh one.
            float bite = segment.MaxHealth > 0
                ? MathHelper.Clamp(e.DamageApplied / (float)segment.MaxHealth, 0f, 1f)
                : 0f;

            SoundId hitSound = segment.IsArmored ? SoundId.SegmentArmorHit : SoundId.SegmentHit;
            _manager.Audio.PlayVaried(hitSound, 0.75f + bite * 0.25f, 0.14f, pan);
        }

        private void OnSegmentKilled(object? sender, SnakeSegment segment)
        {
            _ringsDestroyed++;

            float scale = segment.IsArmored ? 1.25f : 1f;
            _particles?.EmitDestruction(segment.Position, segment.NeonColor, scale);
            _shake.Add(GameConstants.ShakeOnSegmentKill * scale);

            _manager.Audio.PlayVaried(SoundId.SegmentDestroy, 0.85f, 0.12f,
                AudioManager.PanFor(segment.Position.X));

            _supplies?.ReleaseFrom(segment);
        }

        private void OnSupplyDropped(object? sender, Supply supply)
        {
            _manager.Audio.Play(SoundId.SupplyDrop, 0.55f, 0f,
                AudioManager.PanFor(supply.Position.X));
        }

        private void OnSupplyCollected(object? sender, Supply supply)
        {
            _particles?.EmitPickup(supply.Position, supply.Tint);
            _floatingText?.ShowLabel(
                supply.Position + new Vector2(0f, -28f),
                SupplyTypeInfo.NameOf(supply.Type),
                Color.Lerp(supply.Tint, Color.White, 0.4f),
                0.8f);

            _manager.Audio.Play(
                supply.Type == SupplyType.HealthPack ? SoundId.HealthPickup : SoundId.SupplyPickup,
                0.85f);
        }

        private void OnPlayerContact(object? sender, SnakeSegment segment)
        {
            if (_player == null) return;

            Vector2 away = _player.Position - segment.Position;
            if (away.LengthSquared() < 0.001f) away = Vector2.UnitY;

            _particles?.EmitPlayerHurt(_player.Position, away);
            _shake.Add(GameConstants.ShakeOnPlayerHurt);
            _manager.Audio.Play(SoundId.PlayerHurt, 0.9f);

            if (GameConfig.Settings.ShowDamageNumbers)
                _floatingText?.ShowDamage(_player.Position + new Vector2(0f, -30f),
                    GameConstants.ContactDamagePerTick, new Color(255, 80, 80));
        }

        private void OnCoreBreached(object? sender, EventArgs e)
        {
            _shake.Add(30f);
            if (_player != null)
                _particles?.EmitDestruction(_player.Position, new Color(255, 70, 70), 2.2f);
        }

        private void OnPlayerDied(object? sender, EventArgs e) => FinishLevel(won: false);

        // ── Level completion ───────────────────────────────────────────────

        private void FinishLevel(bool won)
        {
            if (_phase == Phase.Outcome) return;

            _playerWon = won;
            EnterPhase(Phase.Outcome);

            _manager.Audio.Play(won ? SoundId.LevelClear : SoundId.PlayerDown, 0.95f);

            long score = _ringsDestroyed * (long)_level.Index * 10L;

            SaveManager.RecordRun(
                segmentsDestroyed: _ringsDestroyed,
                shotsFired: _bullets?.ShotsFired ?? 0,
                suppliesCollected: _supplies?.CollectedCount ?? 0,
                playSeconds: _runSeconds,
                died: !won,
                score: score);

            if (won)
            {
                int next = Math.Min(GameConstants.MaxLevel, _level.Index + 1);
                SaveManager.SaveLevel(next);
            }
        }

        // ── Draw ───────────────────────────────────────────────────────────

        public void Draw(DrawContext ctx, GameTime gameTime)
        {
            ctx.ShakeOffset = _shake.Offset;

            DrawWorld(ctx, gameTime);
            DrawHud(ctx, gameTime);

            ctx.ShakeOffset = Vector2.Zero;
        }

        private void DrawWorld(DrawContext ctx, GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            float danger = _snake?.Danger ?? 0f;

            ctx.UseWorld();

            _manager.Background.Draw(ctx.Batch, ctx.Pixel, _level.ThemeColor, 1f);

            NeonRenderer.DrawGrid(ctx.Batch, ctx.Pixel, ctx.Width, ctx.Height,
                _level.ThemeColor, 55, 1f + danger * 0.8f);

            NeonRenderer.DrawCoreRing(ctx.Batch, ctx.Pixel, ctx.Center, danger, time,
                _level.ThemeColor);

            _snake?.Draw(ctx.Batch, gameTime);
            _supplies?.Draw(ctx.Batch, gameTime);
            _bullets?.Draw(ctx.Batch, gameTime);
            _particles?.Draw(ctx.Batch);
            _player?.Draw(ctx.Batch, gameTime);

            _floatingText?.Draw(ctx.Batch, ctx.GameFont);
        }

        private void DrawHud(DrawContext ctx, GameTime gameTime)
        {
            float time = (float)gameTime.TotalGameTime.TotalSeconds;

            if (_player != null)
            {
                _hud.DrawHealth(ctx, _player, time);
                _hud.DrawBoosts(ctx, _player);
            }

            if (_snake != null)
            {
                _hud.DrawLevelStatus(ctx, _level,
                    _snake.AliveSegmentCount, _snake.SecondsRemaining, _snake.Danger);
            }

            _hud.DrawHint(ctx, HintAlpha());

            if (_phase == Phase.Intro)
                _hud.DrawIntro(ctx, _level, _milestoneBanner, IntroAlpha());

            if (_phase == Phase.Outcome)
            {
                _hud.DrawOutcome(ctx, _playerWon, _level, OutcomeAlpha(),
                    _ringsDestroyed, _supplies?.CollectedCount ?? 0);
            }
        }

        /// <summary>Fades the intro card in over 0.3 s and out over the last 0.5 s.</summary>
        private float IntroAlpha()
        {
            if (_phaseTimer < 0.3f) return _phaseTimer / 0.3f;
            float remaining = IntroDuration - _phaseTimer;
            if (remaining < 0.5f) return MathHelper.Clamp(remaining / 0.5f, 0f, 1f);
            return 1f;
        }

        private float OutcomeAlpha()
            => MathHelper.Clamp(_phaseTimer / 0.45f, 0f, 1f);

        private float HintAlpha()
        {
            if (_phase != Phase.Playing) return 0f;
            if (_runSeconds < HintFadeStart) return 1f;
            return MathHelper.Clamp(1f - (_runSeconds - HintFadeStart) / HintFadeLength, 0f, 1f);
        }
    }
}
