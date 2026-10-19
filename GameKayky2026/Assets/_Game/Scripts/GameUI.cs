using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ValeDasReliquias
{
    public class GameUI : MonoBehaviour
    {
        public Font font;
        public Sprite buttonSprite, panelSprite;
        public GameObject menu, hud, pause, options, result;
        public Text titleText, objectiveText, countText, healthText, promptText, noticeText, resultTitle, resultBody, volumeText, sensitivityText;
        public Slider healthBar, volumeSlider, sensitivitySlider;
        public Button playButton, optionsButton, quitButton, continueButton, restartButton, pauseOptionsButton, menuButton, backButton, resultRestartButton, resultMenuButton;
        static readonly Color Ink = new Color(0.025f, 0.065f, 0.085f, 0.96f);
        static readonly Color Gold = new Color(0.96f, 0.71f, 0.3f);
        static readonly Color Paper = new Color(0.94f, 0.94f, 0.85f);

        void Start()
        {
            PrepareFooter();
            var s = GameSession.Ensure();
            playButton.onClick.AddListener(s.StartGame);
            optionsButton.onClick.AddListener(s.OpenOptions);
            quitButton.onClick.AddListener(s.Quit);
            continueButton.onClick.AddListener(s.Resume);
            restartButton.onClick.AddListener(s.RestartStage);
            pauseOptionsButton.onClick.AddListener(s.OpenOptions);
            menuButton.onClick.AddListener(s.ReturnToMenu);
            backButton.onClick.AddListener(s.CloseOptions);
            resultRestartButton.onClick.AddListener(() => { if (s.Mode == ScreenMode.Victory) s.StartGame(); else s.RestartStage(); });
            resultMenuButton.onClick.AddListener(s.ReturnToMenu);
            volumeSlider.SetValueWithoutNotify(s.Volume);
            sensitivitySlider.SetValueWithoutNotify(s.Sensitivity);
            volumeSlider.onValueChanged.AddListener(s.SetVolume);
            sensitivitySlider.onValueChanged.AddListener(s.SetSensitivity);
        }
        void Update()
        {
            var s = GameSession.Instance;
            if (s == null || s.Stage == null) return;
            menu.SetActive(s.Mode == ScreenMode.Menu);
            hud.SetActive(s.Stage.stageNumber > 0 && s.Mode == ScreenMode.Playing);
            pause.SetActive(s.Mode == ScreenMode.Paused);
            options.SetActive(s.Mode == ScreenMode.Options);
            result.SetActive(s.Mode == ScreenMode.Victory || s.Mode == ScreenMode.Defeat);
            var stage = s.Stage;
            titleText.text = $"0{stage.stageNumber}  /  {stage.stageTitle.ToUpperInvariant()}";
            objectiveText.text = stage.objective;
            var g = stage.goals;
            countText.text = stage.stageNumber == 2
                ? (s.HasKey ? "CHAVE ENCONTRADA   ●" : "CHAVE   ○  Procure nas ruínas")
                : $"MOEDAS  {g.Coins}/{g.coins}     DIAMANTES  {g.Diamonds}/{g.diamonds}     POTES  {g.Teapots}/{g.teapots}";
            healthBar.value = s.Health / GameSession.MaxHealth;
            healthText.text = $"VIDA   {s.Health:0} / 100";
            promptText.text = stage.Prompt;
            promptText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(stage.Prompt));
            noticeText.text = Time.unscaledTime < s.NoticeUntil ? s.Notice : "";
            volumeText.text = $"VOLUME   {s.Volume * 100:0}%";
            sensitivityText.text = $"SENSIBILIDADE DA CÂMERA   {s.Sensitivity:0.0}×";
            bool won = s.Mode == ScreenMode.Victory;
            resultTitle.text = won ? "O VALE ESTÁ COMPLETO" : "A JORNADA NÃO TERMINOU";
            resultBody.text = won ? "Você recuperou as relíquias, abriu os caminhos\ne atravessou o vale. Obrigado por jogar." : "Sua vida chegou a zero. Tente outro caminho,\nevite as bombas e procure as maletas de vida.";
            resultRestartButton.GetComponentInChildren<Text>().text = won ? "JOGAR NOVAMENTE" : "TENTAR NOVAMENTE";
        }

        // Called by the scene builder. All generated UI objects remain editable in the scene.
        public void Construct()
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (Object.FindFirstObjectByType<EventSystem>() == null) { var es = new GameObject("Entrada da interface", typeof(EventSystem), typeof(InputSystemUIInputModule)); }
            menu = Panel("Menu", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.01f, 0.025f, 0.035f, 0.2f));
            var left = Panel("Capa", menu.transform, new Vector2(0, 0), new Vector2(0.47f, 1), Vector2.zero, Vector2.zero, Ink);
            Label(left.transform, "EXPLORAÇÃO  /  TERCEIRA PESSOA", 20, Gold, new Vector2(72, -122), new Vector2(710, 44));
            Label(left.transform, "VALE DAS\nRELÍQUIAS", 82, Paper, new Vector2(66, -206), new Vector2(740, 215));
            Label(left.transform, "Três caminhos. Uma coleção para recuperar.", 24, Paper, new Vector2(72, -445), new Vector2(710, 60));
            playButton = ActionButton(left.transform, "JOGAR", new Vector2(72, -555), new Vector2(590, 76));
            optionsButton = ActionButton(left.transform, "OPÇÕES", new Vector2(72, -651), new Vector2(590, 76));
            quitButton = ActionButton(left.transform, "SAIR", new Vector2(72, -747), new Vector2(590, 76));
            Label(left.transform, "WASD  mover    SHIFT  correr    ESPAÇO  pular\nMOUSE  câmera    E  interagir    F  chute\n1–5  animações    ESC  pausa", 19, new Color(0.67f, 0.76f, 0.73f), new Vector2(72, -885), new Vector2(720, 115));
            var caption = Label(menu.transform, "UM PERCURSO PELO VALE", 22, Paper, Vector2.zero, new Vector2(710, 60));
            Anchor(caption.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-62, 55), new Vector2(710, 60)); caption.alignment = TextAnchor.MiddleRight;

            hud = Panel("HUD", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            var top = Panel("Objetivo", hud.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -34), new Vector2(870, 164), Ink);
            titleText = Label(top.transform, "ETAPA", 23, Gold, new Vector2(24, -15), new Vector2(815, 38));
            objectiveText = Label(top.transform, "Objetivo", 21, Paper, new Vector2(24, -57), new Vector2(815, 54));
            countText = Label(top.transform, "Coleção", 19, Paper, new Vector2(24, -117), new Vector2(815, 35));
            var life = Panel("Vida", hud.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-300, -34), new Vector2(268, 97), Ink);
            healthText = Label(life.transform, "VIDA", 21, Paper, new Vector2(22, -10), new Vector2(230, 35));
            healthBar = Bar(life.transform, new Vector2(22, -58), new Vector2(224, 16), new Color(0.24f, 0.78f, 0.62f)); healthBar.interactable = false;
            var prompt = Panel("Interação", hud.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-440, 172), new Vector2(880, 66), Ink);
            promptText = Label(prompt.transform, "", 23, Gold, new Vector2(18, -10), new Vector2(844, 46)); promptText.alignment = TextAnchor.MiddleCenter;
            noticeText = Label(hud.transform, "", 25, Gold, Vector2.zero, new Vector2(1160, 64));
            Anchor(noticeText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 245), new Vector2(1160, 64)); noticeText.alignment = TextAnchor.MiddleCenter;
            var controls = Label(hud.transform, "WASD  mover   ·   SHIFT  correr   ·   ESPAÇO  pular   ·   E  interagir   ·   F  chute   ·   1–5  ações   ·   ESC  pausa", 19, Paper, Vector2.zero, new Vector2(1600, 45));
            Anchor(controls.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1600, 45)); controls.alignment = TextAnchor.MiddleCenter;

            pause = Overlay("Pausa"); var pauseCard = Card(pause.transform);
            Label(pauseCard, "PAUSA", 52, Gold, new Vector2(55, -42), new Vector2(750, 90));
            Label(pauseCard, "Seu caminho espera por você.", 23, Paper, new Vector2(55, -143), new Vector2(750, 60));
            continueButton = ActionButton(pauseCard, "CONTINUAR", new Vector2(55, -238), new Vector2(750, 70));
            restartButton = ActionButton(pauseCard, "REINICIAR FASE", new Vector2(55, -326), new Vector2(750, 70));
            pauseOptionsButton = ActionButton(pauseCard, "OPÇÕES", new Vector2(55, -414), new Vector2(750, 70));
            menuButton = ActionButton(pauseCard, "VOLTAR AO MENU", new Vector2(55, -502), new Vector2(750, 70));
            options = Overlay("Opções"); var optionsCard = Card(options.transform);
            Label(optionsCard, "SEU JEITO DE JOGAR", 43, Gold, new Vector2(55, -42), new Vector2(750, 90));
            volumeText = Label(optionsCard, "VOLUME", 23, Paper, new Vector2(55, -190), new Vector2(750, 45));
            volumeSlider = Bar(optionsCard, new Vector2(55, -260), new Vector2(750, 30), Gold); volumeSlider.interactable = true;
            sensitivityText = Label(optionsCard, "SENSIBILIDADE", 23, Paper, new Vector2(55, -337), new Vector2(750, 45));
            sensitivitySlider = Bar(optionsCard, new Vector2(55, -407), new Vector2(750, 30), Gold); sensitivitySlider.minValue = 0.2f; sensitivitySlider.maxValue = 3;
            backButton = ActionButton(optionsCard, "VOLTAR", new Vector2(55, -512), new Vector2(750, 70));
            result = Overlay("Resultado"); var resultCard = Card(result.transform);
            resultTitle = Label(resultCard, "RESULTADO", 39, Gold, new Vector2(55, -60), new Vector2(750, 120));
            resultBody = Label(resultCard, "", 24, Paper, new Vector2(55, -216), new Vector2(750, 115));
            resultRestartButton = ActionButton(resultCard, "TENTAR NOVAMENTE", new Vector2(55, -400), new Vector2(750, 74));
            resultMenuButton = ActionButton(resultCard, "VOLTAR AO MENU", new Vector2(55, -503), new Vector2(750, 74));
            menu.SetActive(false); pause.SetActive(false); options.SetActive(false); result.SetActive(false);
            PrepareFooter();
        }
        void PrepareFooter()
        {
            foreach (var label in hud.GetComponentsInChildren<Text>(true))
            {
                if (!label.text.StartsWith("WASD") || label.transform.parent.name == "Controles") continue;
                var footer = Panel("Controles", hud.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-830, 82), new Vector2(1660, 64), Ink);
                label.transform.SetParent(footer.transform, false);
                Anchor(label.rectTransform, Vector2.up, Vector2.up, Vector2.up, new Vector2(20, -7), new Vector2(1620, 50));
            }
        }
        GameObject Overlay(string name) => Panel(name, transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.015f, 0.03f, 0.04f, 0.8f));
        Transform Card(Transform parent)
        {
            var go = Panel("Painel", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-430, 320), new Vector2(860, 640), Ink);
            return go.transform;
        }
        GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(0, 1);
            if (min != max) { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; } else { rt.anchoredPosition = position; rt.sizeDelta = size; }
            var im = go.GetComponent<Image>(); im.color = color; im.raycastTarget = color.a > 0.3f;
            return go;
        }
        Text Label(Transform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(text.Length > 25 ? text.Substring(0, 25) : text, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            Anchor(go.GetComponent<RectTransform>(), Vector2.up, Vector2.up, Vector2.up, position, dimensions);
            var label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.color = color; label.alignment = TextAnchor.UpperLeft; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        Button ActionButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var go = Panel(label, parent, Vector2.up, Vector2.up, position, size, Gold);
            var image = go.GetComponent<Image>(); image.sprite = buttonSprite; image.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1, 0.88f, 0.64f); colors.pressedColor = new Color(0.7f, 0.8f, 0.72f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
            var text = Label(go.transform, label, 24, Ink, new Vector2(14, -8), size - new Vector2(28, 16)); text.alignment = TextAnchor.MiddleCenter;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            return button;
        }
        Slider Bar(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = Panel("Barra", parent, Vector2.up, Vector2.up, position, size, new Color(0.2f, 0.29f, 0.3f));
            var fill = Panel("Preenchimento", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
            var slider = go.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1; slider.fillRect = fill.GetComponent<RectTransform>(); slider.targetGraphic = go.GetComponent<Image>(); slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }
        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size) { rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size; }
    }
}
