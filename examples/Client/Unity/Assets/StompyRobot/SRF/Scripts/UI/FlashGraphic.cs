namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Instantly sets colour to FlashColor on pointer down, then fades back to DefaultColour once pointer is released.
    /// </summary>
    [AddComponentMenu(ComponentMenuPaths.FlashGraphic)]
    [ExecuteInEditMode]
    public class FlashGraphic : UIBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float DecayTime = 0.15f;
        public Color DefaultColor = new Color(1, 1, 1, 0);
        public Color FlashColor = Color.white;
        public Graphic Target;

        public void OnPointerDown(PointerEventData eventData)
        {
            Target.CrossFadeColor(FlashColor, 0f, true, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Target.CrossFadeColor(DefaultColor, DecayTime, true, true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Target.CrossFadeColor(DefaultColor, 0f, true, true);
        }

        protected void Update()
        {
#if UNITY_EDITOR

            if (!Application.isPlaying && Target != null)
            {
                Target.CrossFadeColor(DefaultColor, 0, true, true);
            }

#endif
        }

        public void Flash()
        {
            Target.CrossFadeColor(FlashColor, 0f, true, true);
            Target.CrossFadeColor(DefaultColor, DecayTime, true, true);
        }
    }
}
