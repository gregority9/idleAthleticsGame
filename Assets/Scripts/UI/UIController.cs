using System;
using System.Collections.Generic;
using TrackDynasty.Mvp03.Core;
using TrackDynasty.Mvp03.Domain;
using TrackDynasty.Mvp03.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        public virtual void Hide() => gameObject.SetActive(false);
        public virtual void Refresh() { if (!_initialized) return; }

        protected void Rebuild()
        {
            UIFactory.Clear(Content);
            Build();
        }

        protected abstract void Build();
    }

    public enum ScreenId
    {
        Setup,
        HQ,
        Team,
        Athlete,
        Calendar,
        Applications,
        RacePrep,
        Race,
        Results
    }

    public class MainUIController : MonoBehaviour
    {
        private GameManager _manager;
        private Canvas _canvas;
        private RectTransform _screenHost;
        private GameObject _header;
        private GameObject _nav;
        private Text _managementText;
        private Text _weekText;
        private Text _cashText;
        private Text _rosterText;
        private Text _repText;
        private readonly Dictionary<ScreenId, GameScreen> _screens = new Dictionary<ScreenId, GameScreen>();
        private ScreenId _current;

        public ScreenId Current => _current;

        public void Initialize(GameManager manager)
        {
            _manager = manager;
            BuildCanvas();
            BuildChrome();
            BuildScreens();
            _manager.StateChanged += OnStateChanged;
            Navigate(_manager.State.SetupCompleted ? ScreenId.HQ : ScreenId.Setup);
        }

        private void OnDestroy()
        {
            if (_manager != null) _manager.StateChanged -= OnStateChanged;
        }

        private void BuildCanvas()
        {
            GameObject canvasGo = new GameObject("TrackDynasty_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(430f, 930f);
            scaler.matchWidthOrHeight = 0.5f;

            Image bg = UIFactory.Panel(canvasGo.transform, UITheme.Background, "Background");
            UIFactory.Stretch(bg.rectTransform, 0, 0, 0, 0);
            bg.transform.SetAsFirstSibling();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        private void BuildChrome()
        {
            _header = UIFactory.CreateRect("Header", _canvas.transform);
            RectTransform headerRt = UIFactory.Rect(_header);
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 92f);
            headerRt.anchoredPosition = Vector2.zero;
            _header.AddComponent<Image>().color = UITheme.Panel;

            _managementText = UIFactory.Text(_header.transform, "", 20, TextAnchor.MiddleLeft, UITheme.Text, FontStyle.Bold, 30f);
            RectTransform managementRt = _managementText.rectTransform;
            managementRt.anchorMin = new Vector2(0f, 1f);
            managementRt.anchorMax = new Vector2(0f, 1f);
            managementRt.pivot = new Vector2(0f, 1f);
            managementRt.anchoredPosition = new Vector2(14f, -8f);
            managementRt.sizeDelta = new Vector2(250f, 30f);

            _weekText = UIFactory.Text(_header.transform, "", 13, TextAnchor.MiddleLeft, UITheme.Green, FontStyle.Bold, 22f);
            RectTransform weekRt = _weekText.rectTransform;
            weekRt.anchorMin = new Vector2(0f, 0f);
            weekRt.anchorMax = new Vector2(0f, 0f);
            weekRt.pivot = new Vector2(0f, 0f);
            weekRt.anchoredPosition = new Vector2(14f, 10f);
            weekRt.sizeDelta = new Vector2(240f, 22f);

            _cashText = RightHeaderText(10f, UITheme.Gold, 15);
            _rosterText = RightHeaderText(34f, UITheme.Text, 13);
            _repText = RightHeaderText(56f, UITheme.Green, 13);

            _screenHost = UIFactory.Rect(UIFactory.CreateRect("ScreenHost", _canvas.transform));
            _screenHost.anchorMin = Vector2.zero;
            _screenHost.anchorMax = Vector2.one;
            _screenHost.offsetMin = new Vector2(0f, 64f);
            _screenHost.offsetMax = new Vector2(0f, -94f);

            _nav = UIFactory.CreateRect("BottomNav", _canvas.transform);
            RectTransform navRt = UIFactory.Rect(_nav);
            navRt.anchorMin = new Vector2(0f, 0f);
            navRt.anchorMax = new Vector2(1f, 0f);
            navRt.pivot = new Vector2(0.5f, 0f);
            navRt.sizeDelta = new Vector2(0f, 64f);
            _nav.AddComponent<Image>().color = UITheme.Panel;
            HorizontalLayoutGroup layout = _nav.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 7, 7);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            AddNav("HQ", ScreenId.HQ);
            AddNav("TEAM", ScreenId.Team);
            AddNav("CAL", ScreenId.Calendar);
            AddNav("INBOX", ScreenId.Applications);
        }

        private Text RightHeaderText(float bottom, Color color, int size)
        {
            Text text = UIFactory.Text(_header.transform, "", size, TextAnchor.MiddleRight, color, FontStyle.Bold, 22f);
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-14f, bottom);
            rt.sizeDelta = new Vector2(170f, 22f);
            return text;
        }

        private void AddNav(string label, ScreenId id) => UIFactory.Button(_nav.transform, label, () => Navigate(id), UITheme.PanelAlt, 50f);

        private void BuildScreens()
        {
            AddScreen<SetupScreen>(ScreenId.Setup);
            AddScreen<HQScreen>(ScreenId.HQ);
            AddScreen<TeamScreen>(ScreenId.Team);
            AddScreen<AthleteScreen>(ScreenId.Athlete);
            AddScreen<CalendarScreen>(ScreenId.Calendar);
            AddScreen<ApplicationsScreen>(ScreenId.Applications);
            AddScreen<RacePrepScreen>(ScreenId.RacePrep);
            AddScreen<RaceScreen>(ScreenId.Race);
            AddScreen<ResultsScreen>(ScreenId.Results);
        }

        private void AddScreen<T>(ScreenId id) where T : GameScreen
        {
            GameObject go = UIFactory.CreateRect(id.ToString(), _screenHost);
            UIFactory.Stretch(UIFactory.Rect(go), 0, 0, 0, 0);
            T screen = go.AddComponent<T>();
            screen.Initialize(_manager, this);
            screen.Hide();
            _screens[id] = screen;
        }

        public void Navigate(ScreenId id)
        {
            foreach (KeyValuePair<ScreenId, GameScreen> pair in _screens) pair.Value.Hide();
            _current = id;
            bool immersive = id == ScreenId.Setup || id == ScreenId.RacePrep || id == ScreenId.Race || id == ScreenId.Results;
            _header.SetActive(!immersive);
            _nav.SetActive(!immersive);
            _screenHost.offsetMin = new Vector2(0f, immersive ? 0f : 64f);
            _screenHost.offsetMax = new Vector2(0f, immersive ? 0f : -94f);
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

        public void OpenRace() => Navigate(ScreenId.Race);
        public void OpenResults() => Navigate(ScreenId.Results);

        private void OnStateChanged()
        {
            RefreshChrome();
            if (_screens.TryGetValue(_current, out GameScreen screen)) screen.Refresh();
            if (!_manager.State.SetupCompleted && _current != ScreenId.Setup) Navigate(ScreenId.Setup);
        }

        private void RefreshChrome()
        {
            if (_manager == null || _manager.State == null) return;
            _managementText.text = _manager.State.Management != null ? _manager.State.Management.Name.ToUpperInvariant() : "TRACK DYNASTY";
            _weekText.text = _manager.State.CurrentWeek != null ? _manager.State.CurrentWeek.Label : "WEEK 01/52 • 2026";
            _cashText.text = "$" + _manager.State.Cash.ToString("N0");
            _rosterText.text = "ATHLETES " + (_manager.State.Roster != null ? _manager.State.Roster.Count : 0);
            _repText.text = "REP " + (_manager.State.Management != null ? _manager.State.Management.Reputation : 0).ToString("N0");
        }
    }
}
