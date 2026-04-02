using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Spiral hareketi ile dışarıdan içe doğru ilerleyen yılanı yönetir.
    /// Kafa parametrik spiral yolda ilerler; her segment, kafanın
    /// konum geçmişini bir ofsetle takip eder (trail pattern).
    /// </summary>
    public sealed class Snake : GameObject
    {
        // ── Segmentler ─────────────────────────────────────────────────────────
        public SnakeSegment?              Head     { get; private set; }
        public IReadOnlyList<SnakeSegment> Segments => _segments;

        private readonly List<SnakeSegment> _segments = new();
        private readonly List<Vector2>      _trail    = new();   // kafa konum geçmişi

        // ── Spiral parametreleri ────────────────────────────────────────────────
        private float _elapsed;
        private float _startRadius;
        private float _startAngle;
        private readonly Vector2 _center;

        // Yılan kafasının merkeze ulaşıp ulaşmadığı
        public bool HeadReachedCenter { get; private set; }
        private const float CenterThreshold = 40f;

        // Supply drop event'i (segment öldüğünde tetiklenir)
        public event EventHandler<SnakeSegment>? SegmentKilled;

        private readonly SpriteFont _font;
        private readonly Texture2D  _pixel;
        
        public int TotalSegmentCount { get; private set; }

        public Snake(int level, SpriteFont font, Texture2D pixel)
        {
            _font   = font;
            _pixel  = pixel;
            _center = new Vector2(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);

            // Ekranın köşesinden biraz dışarıda başla
            _startRadius = (float)Math.Sqrt(
                Math.Pow(GameConstants.ScreenWidth  / 2f + 100, 2) +
                Math.Pow(GameConstants.ScreenHeight / 2f + 100, 2));
            _startAngle = 0f;

            TotalSegmentCount = 10 + (level * 4);
            BuildSnake(level);
        }

        private void BuildSnake(int level)
        {
            Head = new SnakeSegment(isHead: true, level, _font, _pixel);
            // Trail yeterince dolana kadar kafanın başlangıç pozuna set et
            float initX = _center.X + _startRadius;
            float initY = _center.Y;
            Head.SetPosition(new Vector2(initX, initY));

            // Geçmişi kafanın ilk pozisyonuyla önceden doldur
            int totalTrailNeeded = (TotalSegmentCount + 1)
                                   * GameConstants.SegmentTrailSteps + 60;
            for (int i = 0; i < totalTrailNeeded; i++)
                _trail.Add(Head.Position);

            // Gövde segmentleri
            for (int i = 0; i < TotalSegmentCount; i++)
            {
                var seg = new SnakeSegment(isHead: false, level, _font, _pixel);
                seg.Died += OnSegmentDied;
                _segments.Add(seg);
            }
        }

        private void OnSegmentDied(object? sender, EventArgs e)
        {
            if (sender is SnakeSegment seg)
                SegmentKilled?.Invoke(this, seg);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsed += dt;

            // ── Kafa: parametrik spiral ──────────────────────────────────────
            float r     = _startRadius - GameConstants.SpiralInwardSpeed * _elapsed;
            float angle = _startAngle  + GameConstants.SpiralAngularSpeed * _elapsed;

            if (r < 0f) r = 0f;

            Vector2 headTarget = _center + new Vector2(
                MathF.Cos(angle) * r,
                MathF.Sin(angle) * r);

            // Kafanın hareketi yönüne döndür
            Vector2 moveDir = headTarget - Head.Position;
            if (moveDir.LengthSquared() > 0.01f)
                Head.UpdateRotation(MathF.Atan2(moveDir.Y, moveDir.X));

            Head.SetPosition(headTarget);
            Head.Update(gameTime);

            // Trail başına ekle
            _trail.Insert(0, Head.Position);
            // Çok büyümesin
            int maxTrail = (_segments.Count + 2) * GameConstants.SegmentTrailSteps + 80;
            while (_trail.Count > maxTrail) _trail.RemoveAt(_trail.Count - 1);

            // ── Gövde: trail takibi ──────────────────────────────────────────
            for (int i = 0; i < _segments.Count; i++)
            {
                var seg        = _segments[i];
                int trailIndex = (i + 1) * GameConstants.SegmentTrailSteps;

                if (trailIndex < _trail.Count)
                {
                    Vector2 prevPos = seg.Position;
                    seg.SetPosition(_trail[trailIndex]);

                    // Hareket yönüne döndür
                    Vector2 segDir = seg.Position - prevPos;
                    if (segDir.LengthSquared() > 0.01f)
                        seg.UpdateRotation(MathF.Atan2(segDir.Y, segDir.X));
                }

                seg.Update(gameTime);
            }

            // ── Yılan kafası merkeze ulaştı mı? ──────────────────────────────
            if (Vector2.Distance(Head.Position, _center) < CenterThreshold)
                HeadReachedCenter = true;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Kuyruktan başa doğru çiz (head en üstte)
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                if (_segments[i].IsAlive)
                    _segments[i].Draw(spriteBatch, gameTime);
            }
            if (Head.IsAlive)
                Head.Draw(spriteBatch, gameTime);
        }

        /// <summary>
        /// Tüm canlı gövde segmentlerini döndürür (collision için).
        /// </summary>
        public IEnumerable<SnakeSegment> GetAliveBodySegments()
        {
            foreach (var s in _segments)
                if (s.IsAlive) yield return s;
        }

        public bool IsDefeated()
        {
            // Tüm segmentler yok ve kafa merkeze ulaştıysa
            return HeadReachedCenter;
        }
    }
}
