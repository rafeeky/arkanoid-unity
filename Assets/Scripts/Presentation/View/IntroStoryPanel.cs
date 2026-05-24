using TMPro;
using UnityEngine;

namespace Arkanoid.Presentation.View
{
    // IntroStory — 페이지 일러스트 + typing 텍스트.
    public sealed class IntroStoryPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyText;

        [System.Serializable]
        private struct PageEntry { public int PageIndex; public Sprite Illustration; }

        [SerializeField] private SpriteRenderer illustration;
        [SerializeField] private PageEntry[] pages;

        public void Bind(IntroScreenViewModel vm)
        {
            gameObject.SetActive(vm.IsVisible);
            if (!vm.IsVisible) return;

            if (bodyText != null) bodyText.text = vm.VisibleText;
            if (illustration != null && pages != null)
            {
                foreach (var p in pages)
                {
                    if (p.PageIndex == vm.PageIndex)
                    {
                        illustration.sprite = p.Illustration;
                        break;
                    }
                }
            }
        }
    }
}
