using UnityEngine;
using Arkanoid.Gameplay;

namespace Arkanoid.Presentation.View
{
    // TS renderBar.ts 1:1 매칭. 좌 반원 + 흰띠 + 사각 base + 흰띠 + 우 반원.
    // sprite 5개 (white square/circle) runtime 생성. 색만 effect 별로.
    public sealed class BarRenderer : MonoBehaviour
    {
        // TS BAR_COLORS (renderBar.ts L18-23)
        private static readonly (Color32 baseCol, Color32 semiCol)[] COLORS =
        {
            (new Color32(0x66, 0x66, 0x66, 0xFF), new Color32(0x88, 0xCC, 0xFF, 0xFF)), // None
            (new Color32(0xB8, 0x88, 0x44, 0xFF), new Color32(0xFF, 0xDD, 0xAA, 0xFF)), // Expand
            (new Color32(0x3A, 0x5F, 0xB8, 0xFF), new Color32(0xAA, 0xCC, 0xFF, 0xFF)), // Magnet
            (new Color32(0xB8, 0x3A, 0x4A, 0xFF), new Color32(0xFF, 0xAA, 0xAA, 0xFF)), // Laser
        };

        private const float STRIP_WIDTH = 4f;

        private SpriteRenderer _semL, _semR, _baseRect, _stripL, _stripR;
        private static Sprite _squareSprite;
        private static Sprite _circleSprite;
        private const int UNIT_PX = 64;

        private void Awake() => EnsureInitialized();

        // 컴포넌트 enabled=false 또는 Awake 누락 시에도 Bind 직전 호출돼 sprite/자식 보장.
        private void EnsureInitialized()
        {
            if (_semL != null && _semL.sprite != null) return;
            EnsureSprites();

            // 옛 자식 (V2 sprite 시절 body 등) 비활성 — 새 5-도형 위에 겹치지 않게
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                var n = c.name;
                if (n != "SemiL" && n != "SemiR" && n != "Base" && n != "StripL" && n != "StripR")
                    c.gameObject.SetActive(false);
            }

            _semL     = FindOrMake("SemiL",   _circleSprite, 0);
            _semR     = FindOrMake("SemiR",   _circleSprite, 0);
            _baseRect = FindOrMake("Base",    _squareSprite, 1);
            _stripL   = FindOrMake("StripL",  _squareSprite, 2);
            _stripR   = FindOrMake("StripR",  _squareSprite, 2);

            // sprite 가 null 인 경우 강제 재설정 (FindOrMake 가 existing 일 때 _circleSprite/null 가능성)
            if (_semL.sprite == null) _semL.sprite = _circleSprite;
            if (_semR.sprite == null) _semR.sprite = _circleSprite;
            if (_baseRect.sprite == null) _baseRect.sprite = _squareSprite;
            if (_stripL.sprite == null) _stripL.sprite = _squareSprite;
            if (_stripR.sprite == null) _stripR.sprite = _squareSprite;

            _stripL.color = Color.white;
            _stripR.color = Color.white;
        }

        private SpriteRenderer FindOrMake(string name, Sprite sprite, int order)
        {
            var existing = transform.Find(name);
            if (existing != null)
            {
                var sr = existing.GetComponent<SpriteRenderer>();
                if (sr == null) sr = existing.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
                existing.gameObject.SetActive(true);
                return sr;
            }
            return MakeChild(name, sprite, order);
        }

        public void Bind(BarState bar)
        {
            EnsureInitialized();
            transform.localPosition = new Vector3(bar.X, bar.Y, 0f);

            float w = bar.Width;
            float h = PlayfieldLayout.BarHeight;
            float halfW = w / 2f;
            float r = h / 2f;
            float midW = w - 2f * r;

            int idx = bar.ActiveEffect switch
            {
                BarEffect.Expand => 1,
                BarEffect.Magnet => 2,
                BarEffect.Laser  => 3,
                _                => 0,
            };
            var (baseCol, semiCol) = COLORS[idx];

            // 좌/우 반원 — diameter=h, 위치 (±(halfW-r), 0)
            _semL.color = semiCol;
            _semL.transform.localPosition = new Vector3(-(halfW - r), 0f, 0f);
            _semL.transform.localScale    = new Vector3(h / UNIT_PX, h / UNIT_PX, 1f);

            _semR.color = semiCol;
            _semR.transform.localPosition = new Vector3(+(halfW - r), 0f, 0f);
            _semR.transform.localScale    = new Vector3(h / UNIT_PX, h / UNIT_PX, 1f);

            // 가운데 사각 — w-2r × h, 중앙
            _baseRect.color = baseCol;
            _baseRect.transform.localPosition = Vector3.zero;
            _baseRect.transform.localScale    = new Vector3(midW / UNIT_PX, h / UNIT_PX, 1f);

            // 좌 흰띠 — 사각의 좌 가장자리 (pos.x = -midW/2 + STRIP/2)
            _stripL.transform.localPosition = new Vector3(-midW / 2f + STRIP_WIDTH / 2f, 0f, 0f);
            _stripL.transform.localScale    = new Vector3(STRIP_WIDTH / UNIT_PX, h / UNIT_PX, 1f);

            _stripR.transform.localPosition = new Vector3(+midW / 2f - STRIP_WIDTH / 2f, 0f, 0f);
            _stripR.transform.localScale    = new Vector3(STRIP_WIDTH / UNIT_PX, h / UNIT_PX, 1f);
        }

        private SpriteRenderer MakeChild(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static void EnsureSprites()
        {
            if (_squareSprite == null) _squareSprite = MakeSquareSprite();
            if (_circleSprite == null) _circleSprite = MakeCircleSprite();
        }

        // D3.4 "1 unit = 1 px" 규약 — Sprite PPU=1 로 만들어 64px sprite 가 world 64 unit 이 되도록.
        // 옛 PPU=UNIT_PX(=64) 는 sprite world 1 unit → scale 0.375 적용 시 sub-pixel 로 안 보임.
        private static Sprite MakeSquareSprite()
        {
            var tex = new Texture2D(UNIT_PX, UNIT_PX, TextureFormat.RGBA32, false);
            var pixels = new Color32[UNIT_PX * UNIT_PX];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, UNIT_PX, UNIT_PX), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite MakeCircleSprite()
        {
            var tex = new Texture2D(UNIT_PX, UNIT_PX, TextureFormat.RGBA32, false);
            var pixels = new Color32[UNIT_PX * UNIT_PX];
            float r = UNIT_PX * 0.5f;
            for (int y = 0; y < UNIT_PX; y++)
            for (int x = 0; x < UNIT_PX; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * UNIT_PX + x] = d <= r
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, UNIT_PX, UNIT_PX), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
