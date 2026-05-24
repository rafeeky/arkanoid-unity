using UnityEngine;
using Arkanoid.Definitions;

namespace Arkanoid.Definitions.SO
{
    [CreateAssetMenu(fileName = "LayoutConfig", menuName = "Arkanoid/Presentation/Layout Config")]
    public sealed class LayoutConfigSO : ScriptableObject
    {
        [Header("Canvas")]
        [SerializeField] private float canvasWidth = 1080f;
        [SerializeField] private float canvasHeight = 1920f;

        [Header("Playfield")]
        [SerializeField] private float playfieldWidth = 720f;
        [SerializeField] private float playfieldHeight = 900f;
        [SerializeField] private float playfieldOffsetX = 180f;
        [SerializeField] private float playfieldOffsetY = 600f;

        [Header("HUD")]
        [SerializeField] private float hudLabelY = 100f;
        [SerializeField] private float hudValueY = 170f;
        [SerializeField] private float hudLeftX = 80f;
        [SerializeField] private float hudCenterX = 360f;
        [SerializeField] private float hudRightX = 660f;
        [SerializeField] private float hudLabelFontPx = 36f;
        [SerializeField] private float hudValueFontPx = 52f;

        [Header("Mascot")]
        [SerializeField] private float mascotCenterX = 900f;
        [SerializeField] private float mascotCenterY = 200f;
        [SerializeField] private float mascotSize = 200f;

        [Header("Lives Bar")]
        [SerializeField] private float livesBarStartX = 80f;
        [SerializeField] private float livesBarY = 1700f;
        [SerializeField] private float livesBarScale = 0.4f;
        [SerializeField] private float livesBarGap = 12f;
        [SerializeField] private int livesBarMaxDisplay = 7;

        [Header("Bar Slider")]
        [SerializeField] private float sliderCenterY = 1800f;
        [SerializeField] private float sliderTrackHalfWidth = 400f;
        [SerializeField] private float sliderTrackHeight = 20f;
        [SerializeField] private float sliderKnobRadius = 40f;

        public LayoutConfigData Data => new(
            Canvas: new(canvasWidth, canvasHeight),
            Playfield: new(playfieldWidth, playfieldHeight, playfieldOffsetX, playfieldOffsetY),
            Hud: new(hudLabelY, hudValueY, hudLeftX, hudCenterX, hudRightX, hudLabelFontPx, hudValueFontPx),
            Mascot: new(mascotCenterX, mascotCenterY, mascotSize),
            LivesBar: new(livesBarStartX, livesBarY, livesBarScale, livesBarGap, livesBarMaxDisplay),
            BarSlider: new(sliderCenterY, sliderTrackHalfWidth, sliderTrackHeight, sliderKnobRadius));
    }
}
