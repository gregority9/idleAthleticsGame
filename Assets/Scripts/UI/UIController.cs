using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Core;
using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.UI.Screens;
using UnityEngine.EventSystems;

namespace TrackDynasty.Mvp03.UI
{
    public abstract class GameScreen : MonoBehaviour
    {
        protected GameManager Manager;
        protected MainUIController Controller;
        protected Transform Content;
        private bool _initialized;

        public virtual void Initialize(GameManager manager, MainUIController controller)
        {
            Manager = manager;
            Controller = controller;
            Content = transform;
            _initialized = true;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void Refresh()
        {
            if (!_initialized) return;
        }

        protected void Rebuild()
        {
            UIFactory.Clear(Content);
            try
            {
                Build();
                Canvas.ForceUpdateCanvases();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                UIFactory.Clear(Content);
                UIFactory.BuildScreenError(Content, ex);
            }
        }

        protected abstract void Build();
    }
}

namespace TrackDynasty.Mvp03.UI
{
    public enum ScreenId
    {
        ScoutChoice,
        HQ,
        Team,
        Athlete,
        Calendar,
        Scout,
        Applications,
        HallOfFame,
        RacePrep,
        Race,
        Results
    }

    public class MainUIController : MonoBehaviour
    {
        private GameManager _manager;
        private Canvas _canvas;
        private RectTransform _phoneRoot;
        private RectTransform _screenHost;
        private GameObject _header;
        private GameObject _nav;
        private Text _dateText;
        private Text _cashText;
        private Text _repText;
        private Text _zoomText;
        private readonly Dictionary<ScreenId, GameScreen> _screens = new Dictionary<ScreenId, GameScreen>();
        private ScreenId _current;
        private float _zoomMultiplier = 1f;

        private const float PhoneWidth = 430f;
        private const float PhoneHeight = 930f;
        private const float MinZoom = 0.50f;
        private const float MaxZoom = 1.50f;
        private const float ZoomStep = 0.10f;

        public ScreenId Current => _current;

        public void Initialize(GameManager manager)
        {
            _manager = manager;
            ConfigurePlatformDisplay();
            BuildCanvas();
            BuildChrome();
            BuildScreens();
            _manager.StateChanged += OnStateChanged;

            if (_manager.State.ChosenScout == null)
                Navigate(ScreenId.ScoutChoice);
            else
                Navigate(ScreenId.HQ);
        }

        private void OnDestroy()
        {
            if (_manager != null) _manager.StateChanged -= OnStateChanged;
        }

        private void ConfigurePlatformDisplay()
        {
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.orientation = ScreenOrientation.Portrait;
                return;
            }

#if !UNITY_EDITOR
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(516, 1116, false);
#endif
        }

        private void BuildDesktopZoomControls()
        {
            if (Application.isMobilePlatform) return;

            GameObject controls = UIFactory.CreateRect("DesktopPreviewControls", _canvas.transform);
            RectTransform rt = UIFactory.Rect(controls);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(12f, -12f);
            rt.sizeDelta = new Vector2(286f, 44f);

            Image bg = controls.AddComponent<Image>();
            bg.color = new Color(0.025f, 0.035f, 0.05f, 0.96f);

            HorizontalLayoutGroup layout = controls.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 5, 5);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            Button minus = UIFactory.Button(controls.transform, "−", () => SetZoom(_zoomMultiplier - ZoomStep), UITheme.PanelAlt, 34f);
            UIFactory.SetPreferredWidth(minus, 46f);

            Button fit = UIFactory.Button(controls.transform, "FIT", () => SetZoom(1f), UITheme.GreenDark, 34f);
            UIFactory.SetPreferredWidth(fit, 58f);

            Button plus = UIFactory.Button(controls.transform, "+", () => SetZoom(_zoomMultiplier + ZoomStep), UITheme.PanelAlt, 34f);
            UIFactory.SetPreferredWidth(plus, 46f);

            _zoomText = UIFactory.Text(controls.transform, "100%", 13, TextAnchor.MiddleCenter, UITheme.Muted, FontStyle.Bold, 34f);
            UIFactory.SetPreferredWidth(_zoomText, 64f);
        }

        private void SetZoom(float value)
        {
            _zoomMultiplier = Mathf.Clamp(value, MinZoom, MaxZoom);
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_phoneRoot != null)
                _phoneRoot.localScale = new Vector3(_zoomMultiplier, _zoomMultiplier, 1f);

            if (_zoomText != null)
                _zoomText.text = Mathf.RoundToInt(_zoomMultiplier * 100f) + "%";
        }

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("MVP033_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PhoneWidth, PhoneHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;

            Image desktopBg = UIFactory.Panel(canvasGo.transform, new Color(0.012f, 0.017f, 0.026f, 1f), "DesktopLetterbox");
            UIFactory.Stretch(desktopBg.rectTransform, 0, 0, 0, 0);
            desktopBg.transform.SetAsFirstSibling();

            GameObject phoneGo = UIFactory.CreateRect("PhoneViewport_430x930", canvasGo.transform);
            _phoneRoot = UIFactory.Rect(phoneGo);
            _phoneRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _phoneRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _phoneRoot.pivot = new Vector2(0.5f, 0.5f);
            _phoneRoot.anchoredPosition = Vector2.zero;
            _phoneRoot.sizeDelta = new Vector2(PhoneWidth, PhoneHeight);

            Image phoneBg = phoneGo.AddComponent<Image>();
            phoneBg.color = UITheme.Background;
            RectMask2D clip = phoneGo.AddComponent<RectMask2D>();
            clip.padding = Vector4.zero;
            Outline phoneOutline = phoneGo.AddComponent<Outline>();
            phoneOutline.effectColor = new Color(1f, 1f, 1f, 0.12f);
            phoneOutline.effectDistance = new Vector2(1f, -1f);

            BuildDesktopZoomControls();
            ApplyZoom();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        private void BuildChrome()
        {
            _header = UIFactory.CreateRect("Header", _phoneRoot);
            RectTransform headerRt = UIFactory.Rect(_header);
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 74f);
            headerRt.anchoredPosition = Vector2.zero;
            Image headerBg = _header.AddComponent<Image>();
            headerBg.color = UITheme.Panel;

            Text logo = UIFactory.Text(_header.transform, "TRACK DYNASTY", 24, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 34f);
            RectTransform logoRt = logo.rectTransform;
            logoRt.anchorMin = new Vector2(0f, 0.5f);
            logoRt.anchorMax = new Vector2(0f, 0.5f);
            logoRt.pivot = new Vector2(0f, 0.5f);
            logoRt.anchoredPosition = new Vector2(16f, 14f);
            logoRt.sizeDelta = new Vector2(220f, 32f);

            _dateText = UIFactory.Text(_header.transform, "", 13, TextAnchor.MiddleLeft, UITheme.Muted, FontStyle.Normal, 22f);
            RectTransform dateRt = _dateText.rectTransform;
            dateRt.anchorMin = new Vector2(0f, 0f);
            dateRt.anchorMax = new Vector2(0f, 0f);
            dateRt.pivot = new Vector2(0f, 0f);
            dateRt.anchoredPosition = new Vector2(16f, 8f);
            dateRt.sizeDelta = new Vector2(220f, 20f);

            _cashText = UIFactory.Text(_header.transform, "", 14, TextAnchor.MiddleRight, UITheme.Gold, FontStyle.Bold, 24f);
            RectTransform cashRt = _cashText.rectTransform;
            cashRt.anchorMin = new Vector2(1f, 1f);
            cashRt.anchorMax = new Vector2(1f, 1f);
            cashRt.pivot = new Vector2(1f, 1f);
            cashRt.anchoredPosition = new Vector2(-16f, -10f);
            cashRt.sizeDelta = new Vector2(140f, 22f);

            _repText = UIFactory.Text(_header.transform, "", 13, TextAnchor.MiddleRight, UITheme.Green, FontStyle.Bold, 22f);
            RectTransform repRt = _repText.rectTransform;
            repRt.anchorMin = new Vector2(1f, 0f);
            repRt.anchorMax = new Vector2(1f, 0f);
            repRt.pivot = new Vector2(1f, 0f);
            repRt.anchoredPosition = new Vector2(-16f, 8f);
            repRt.sizeDelta = new Vector2(140f, 20f);

            _screenHost = UIFactory.Rect(UIFactory.CreateRect("ScreenHost", _phoneRoot));
            _screenHost.anchorMin = Vector2.zero;
            _screenHost.anchorMax = Vector2.one;
            _screenHost.offsetMin = new Vector2(0f, 64f);
            _screenHost.offsetMax = new Vector2(0f, -76f);

            _nav = UIFactory.CreateRect("BottomNav", _phoneRoot);
            RectTransform navRt = UIFactory.Rect(_nav);
            navRt.anchorMin = new Vector2(0f, 0f);
            navRt.anchorMax = new Vector2(1f, 0f);
            navRt.pivot = new Vector2(0.5f, 0f);
            navRt.sizeDelta = new Vector2(0f, 64f);
            navRt.anchoredPosition = Vector2.zero;
            Image navBg = _nav.AddComponent<Image>();
            navBg.color = UITheme.Panel;
            HorizontalLayoutGroup navLayout = _nav.AddComponent<HorizontalLayoutGroup>();
            navLayout.padding = new RectOffset(8, 8, 7, 7);
            navLayout.spacing = 6f;
            navLayout.childControlWidth = true;
            navLayout.childControlHeight = true;
            navLayout.childForceExpandWidth = true;
            navLayout.childForceExpandHeight = true;

            AddNav("HQ", ScreenId.HQ);
            AddNav("TEAM", ScreenId.Team);
            AddNav("CAL", ScreenId.Calendar);
            AddNav("SCOUT", ScreenId.Scout);
            AddNav("INBOX", ScreenId.Applications);
        }

        private void AddNav(string label, ScreenId id)
        {
            UIFactory.Button(_nav.transform, label, () => Navigate(id), UITheme.PanelAlt, 50f);
        }

        private void BuildScreens()
        {
            AddScreen<ScoutChoiceScreen>(ScreenId.ScoutChoice);
            AddScreen<HQScreen>(ScreenId.HQ);
            AddScreen<TeamScreen>(ScreenId.Team);
            AddScreen<AthleteScreen>(ScreenId.Athlete);
            AddScreen<CalendarScreen>(ScreenId.Calendar);
            AddScreen<ScoutScreen>(ScreenId.Scout);
            AddScreen<ApplicationsScreen>(ScreenId.Applications);
            AddScreen<HallOfFameScreen>(ScreenId.HallOfFame);
            AddScreen<RacePrepScreen>(ScreenId.RacePrep);
            AddScreen<RaceScreen>(ScreenId.Race);
            AddScreen<ResultsScreen>(ScreenId.Results);
        }

        private void AddScreen<T>(ScreenId id) where T : GameScreen
        {
            GameObject go = UIFactory.CreateRect(id.ToString(), _screenHost);
            RectTransform rt = UIFactory.Rect(go);
            UIFactory.Stretch(rt, 0, 0, 0, 0);
            T screen = go.AddComponent<T>();
            screen.Initialize(_manager, this);
            screen.Hide();
            _screens[id] = screen;
        }

        public void Navigate(ScreenId id)
        {
            foreach (KeyValuePair<ScreenId, GameScreen> pair in _screens)
                pair.Value.Hide();
            _current = id;
            bool immersive = id == ScreenId.ScoutChoice || id == ScreenId.RacePrep || id == ScreenId.Race || id == ScreenId.Results;
            _header.SetActive(!immersive);
            _nav.SetActive(!immersive);
            _screenHost.offsetMin = new Vector2(0f, immersive ? 0f : 64f);
            _screenHost.offsetMax = new Vector2(0f, immersive ? 0f : -76f);
            RefreshChrome();
            _screens[id].Show();
        }

        public void OpenAthlete(Athlete athlete)
        {
            _manager.SelectAthlete(athlete);
            Navigate(ScreenId.Athlete);
        }

        public void OpenRacePrep(Athlete athlete)
        {
            _manager.PrepareRace(athlete);
            if (_manager.ActiveCompetition != null) Navigate(ScreenId.RacePrep);
        }

        public void OpenRace()
        {
            Navigate(ScreenId.Race);
        }

        public void OpenResults()
        {
            Navigate(ScreenId.Results);
        }

        public void OpenHallOfFame()
        {
            Navigate(ScreenId.HallOfFame);
        }

        private void OnStateChanged()
        {
            RefreshChrome();
            if (_screens.TryGetValue(_current, out GameScreen screen)) screen.Refresh();
        }

        private void RefreshChrome()
        {
            if (_manager == null || _manager.State == null) return;
            _dateText.text = _manager.State.CurrentDate.LongLabel;
            _cashText.text = "$" + _manager.State.Cash.ToString("N0");
            _repText.text = "REP " + _manager.State.Reputation.ToString("N0");
        }
    }
}
